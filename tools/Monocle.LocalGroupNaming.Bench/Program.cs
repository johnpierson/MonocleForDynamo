using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MonocleViewExtension.LocalGroupNaming;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Monocle.LocalGroupNaming.Bench
{
    /// <summary>
    /// Runs the production group-naming path (prompt, grammar, sampling, validation and
    /// retry) against any llama-server build and GGUF model, and reports the titles and
    /// latencies. Use it to compare candidate models before changing the manifest pin:
    ///
    ///   dotnet run --project tools/Monocle.LocalGroupNaming.Bench --configuration Release --
    ///       --server C:\path\llama-server.exe --model C:\path\model.gguf [--server-args "-t 6"]
    ///       [--cases cases.json] [--repeat 1] [--out results.json]
    ///
    /// The server and model paths fall back to MONOCLE_LOCAL_AI_SERVER_PATH and
    /// MONOCLE_LOCAL_AI_MODEL_PATH, extra flags to MONOCLE_LOCAL_AI_SERVER_ARGS.
    /// </summary>
    internal static class Program
    {
        private sealed class BenchCase
        {
            [JsonProperty("name")] public string Name { get; set; }
            [JsonProperty("nodes")] public List<string> Nodes { get; set; }
            [JsonProperty("expected")] public string Expected { get; set; }
        }

        private sealed class BenchResult
        {
            public string Case { get; set; }
            public string Title { get; set; }
            public long Milliseconds { get; set; }
            public int Attempts { get; set; }
            public IReadOnlyList<string> Rejected { get; set; }
            public string Error { get; set; }
            public string Expected { get; set; }
        }

        private static async Task<int> Main(string[] args)
        {
            var arguments = ParseArguments(args);
            var serverPath = Get(arguments, "server") ?? Environment.GetEnvironmentVariable("MONOCLE_LOCAL_AI_SERVER_PATH");
            var modelPath = Get(arguments, "model") ?? Environment.GetEnvironmentVariable("MONOCLE_LOCAL_AI_MODEL_PATH");
            var serverArguments = Get(arguments, "server-args") ?? Environment.GetEnvironmentVariable("MONOCLE_LOCAL_AI_SERVER_ARGS") ?? string.Empty;
            var casesPath = Get(arguments, "cases") ?? Path.Combine(AppContext.BaseDirectory, "cases.json");
            var outputPath = Get(arguments, "out");
            var repeat = int.Parse(Get(arguments, "repeat") ?? "1", CultureInfo.InvariantCulture);

            if (string.IsNullOrWhiteSpace(serverPath) || string.IsNullOrWhiteSpace(modelPath))
            {
                Console.Error.WriteLine("usage: --server <llama-server.exe> --model <model.gguf> [--server-args \"-t 6\"] [--cases cases.json] [--repeat N] [--out results.json]");
                return 2;
            }

            var cases = JsonConvert.DeserializeObject<List<BenchCase>>(File.ReadAllText(casesPath)) ?? new List<BenchCase>();
            var options = LocalLlamaServerOptions
                .CreateExternal(serverPath, modelPath, LocalModelManifest.Current)
                .WithAdditionalServerArguments(serverArguments);

            Console.WriteLine($"Model:      {Path.GetFileName(modelPath)} ({LocalModelManifest.FormatSize(new FileInfo(modelPath).Length)})");
            Console.WriteLine($"Server:     {serverPath}");
            Console.WriteLine($"Arguments:  {LocalLlamaServerClient.BuildServerArguments(options, 0)}");
            Console.WriteLine($"Cases:      {cases.Count} x {repeat}");
            Console.WriteLine();

            var results = new List<BenchResult>();
            long startupMilliseconds;
            using (var client = new LocalLlamaServerClient(options))
            {
                var startup = Stopwatch.StartNew();
                await client.EnableAsync(CancellationToken.None).ConfigureAwait(false);
                startup.Stop();
                startupMilliseconds = startup.ElapsedMilliseconds;
                Console.WriteLine($"Startup including warm-up: {startupMilliseconds} ms");
                Console.WriteLine();

                for (var round = 0; round < repeat; round++)
                {
                    foreach (var benchCase in cases)
                    {
                        var result = new BenchResult { Case = benchCase.Name, Expected = benchCase.Expected };
                        var stopwatch = Stopwatch.StartNew();
                        try
                        {
                            var suggestion = await GroupNameSuggester
                                .SuggestAsync(client, benchCase.Nodes, CancellationToken.None)
                                .ConfigureAwait(false);
                            result.Title = suggestion.Title;
                            result.Attempts = suggestion.Attempts;
                            result.Rejected = suggestion.RejectedResponses;
                        }
                        catch (Exception exception)
                        {
                            result.Error = exception.Message;
                            result.Attempts = 2;
                        }

                        stopwatch.Stop();
                        result.Milliseconds = stopwatch.ElapsedMilliseconds;
                        results.Add(result);
                        Console.WriteLine($"{result.Milliseconds,6} ms  {result.Attempts}x  {benchCase.Name,-28} {result.Title ?? "ERROR: " + result.Error}");
                    }
                }
            }

            Console.WriteLine();
            PrintSummary(results);

            if (!string.IsNullOrWhiteSpace(outputPath))
            {
                var report = new JObject
                {
                    ["model"] = Path.GetFileName(modelPath),
                    ["server"] = serverPath,
                    ["arguments"] = LocalLlamaServerClient.BuildServerArguments(options, 0),
                    ["startupMilliseconds"] = startupMilliseconds,
                    ["results"] = JArray.FromObject(results)
                };
                File.WriteAllText(outputPath, report.ToString(Formatting.Indented));
                Console.WriteLine($"Wrote {outputPath}");
            }

            return results.Any(result => result.Error != null) ? 1 : 0;
        }

        private static void PrintSummary(List<BenchResult> results)
        {
            if (results.Count == 0) return;

            var times = results.Select(result => result.Milliseconds).OrderBy(value => value).ToList();
            var median = times[times.Count / 2];
            var p90 = times[Math.Min(times.Count - 1, (int)Math.Ceiling(times.Count * 0.9) - 1)];
            var mean = times.Average();
            var retries = results.Count(result => result.Attempts > 1);
            var failures = results.Count(result => result.Error != null);

            Console.WriteLine("| Case | Title | ms | Attempts |");
            Console.WriteLine("| --- | --- | ---: | ---: |");
            foreach (var result in results)
            {
                Console.WriteLine($"| {result.Case} | {result.Title ?? "ERROR: " + result.Error} | {result.Milliseconds} | {result.Attempts} |");
            }

            Console.WriteLine();
            Console.WriteLine($"Latency: median {median} ms, mean {mean:0} ms, p90 {p90} ms over {results.Count} requests");
            Console.WriteLine($"Retries: {retries}, failures: {failures}");
        }

        private static Dictionary<string, string> ParseArguments(string[] args)
        {
            var parsed = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            for (var index = 0; index < args.Length; index++)
            {
                if (!args[index].StartsWith("--", StringComparison.Ordinal)) continue;

                var key = args[index].Substring(2);
                var value = index + 1 < args.Length && !args[index + 1].StartsWith("--", StringComparison.Ordinal)
                    ? args[++index]
                    : "true";
                parsed[key] = value;
            }

            return parsed;
        }

        private static string Get(Dictionary<string, string> arguments, string key)
        {
            return arguments.TryGetValue(key, out var value) ? value : null;
        }
    }
}
