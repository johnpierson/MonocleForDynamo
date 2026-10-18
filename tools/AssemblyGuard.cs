// Fails if a built MonocleViewExtension.dll could collide with Dynamo, Revit or other packages:
//  - embeds any assembly (*.dll manifest resource) that would need a custom AssemblyResolve handler, or
//  - references an assembly outside the framework / Dynamo-provided allow-list (Xceed, Helix, SharpDX, Newtonsoft...).
// Usage: dotnet run --file tools/AssemblyGuard.cs -- <dll-or-folder> [...]
using System.Reflection.Metadata;
using System.Reflection.PortableExecutable;

string[] allowedPrefixes =
[
    "System", "Microsoft.CSharp", "Microsoft.Win32", "Microsoft.VisualBasic", "mscorlib", "netstandard",
    "PresentationCore", "PresentationFramework", "WindowsBase", "UIAutomation", "Accessibility", "ReachFramework",
    // provided by Dynamo itself
    "Dynamo", "Proto", "CoreNodeModels", "DSCore", "DesignScript", "LibG", "Greg",
    // Dynamo 2.x view models derive from Prism 4.1's NotificationObject; Dynamo ships it
    "Microsoft.Practices.Prism",
];

var files = args.SelectMany(a => Directory.Exists(a)
        ? Directory.EnumerateFiles(a, "MonocleViewExtension.dll", SearchOption.AllDirectories)
        : [a])
    .ToList();

if (files.Count == 0)
{
    Console.Error.WriteLine("AssemblyGuard: no MonocleViewExtension.dll found.");
    return 2;
}

var failures = 0;
foreach (var file in files)
{
    using var stream = File.OpenRead(file);
    using var pe = new PEReader(stream);
    var md = pe.GetMetadataReader();
    var problems = new List<string>();

    foreach (var handle in md.ManifestResources)
    {
        var name = md.GetString(md.GetManifestResource(handle).Name);
        if (name.EndsWith(".dll", StringComparison.OrdinalIgnoreCase) || name.EndsWith(".exe", StringComparison.OrdinalIgnoreCase))
            problems.Add($"embeds assembly resource '{name}'");
    }

    foreach (var handle in md.AssemblyReferences)
    {
        var reference = md.GetAssemblyReference(handle);
        var name = md.GetString(reference.Name);
        if (!allowedPrefixes.Any(p => name.Equals(p, StringComparison.OrdinalIgnoreCase) || name.StartsWith(p + ".", StringComparison.OrdinalIgnoreCase) || (p.Length > 4 && name.StartsWith(p, StringComparison.OrdinalIgnoreCase))))
            problems.Add($"references third-party assembly '{name} {reference.Version}'");
    }

    var size = new FileInfo(file).Length;
    Console.WriteLine($"{(problems.Count == 0 ? "OK  " : "FAIL")} {file} ({size / 1024} KB)");
    foreach (var problem in problems) Console.WriteLine($"     - {problem}");
    if (problems.Count > 0) failures++;
}

return failures == 0 ? 0 : 1;
