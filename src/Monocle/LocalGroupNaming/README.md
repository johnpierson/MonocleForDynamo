# Local group naming experiment

Monocle suggests a short title for groups created from its group flyout by running
`llama-server` (llama.cpp) and a small open-weight model entirely on the local machine.
Dynamo node names, code and notes never leave the computer.

## How a title is produced

1. When a group is created from the flyout, Monocle snapshots its members on the UI
   thread. Plain nodes contribute their name; members that say what the group is *for*
   contribute a detail as well: the first comment or statement of a code block or Python
   node, string input values, file names from path nodes, drop-down selections
   (`Categories: Doors`, `Family Types: ...`), note text and nested group titles.
2. The snapshot is sent to the local server as a chat completion with a fixed system
   prompt (rules plus three examples) and a GBNF grammar that only admits three to seven
   plain words. Grammar-constrained decoding makes preambles, quotes, markdown, JSON and
   reasoning tags impossible, so the model cannot answer anything but a title.
3. Reasoning is switched off at the chat-template level (`--reasoning off` on the server
   and `chat_template_kwargs.enable_thinking=false` per request), so hybrid thinking
   models answer directly instead of spending seconds on a reasoning block.
4. The answer is validated: no copy of a member name (compared without case, spaces or
   punctuation), no API-style identifiers, no generic titles. A second attempt at a
   higher temperature runs if the first proposal is rejected.
5. The rename goes through Dynamo's `UpdateModelValueCommand` (so Ctrl+Z restores the
   previous title) and only if the group still exists with the same title and members.

## First-run setup

Check **local group naming (local AI)** in the Monocle menu. On the first use, Monocle:

1. Shows the third-party licenses and requires separate acceptance of the llama.cpp
   license and the model license.
2. Downloads the pinned Windows CPU runtime and the GGUF model listed in
   `LocalModelManifest.Current`. An interrupted model download is resumed with an HTTP
   range request the next time the feature is enabled instead of starting over.
3. Verifies the expected file sizes and SHA-256 checksums (GitHub's published asset digest
   for the runtime, the Hugging Face LFS hash for the model).
4. Starts the model on localhost, warms the prompt cache with the system prompt, and
   removes installations of earlier model or runtime pins that are no longer in use.

The files and the versioned acceptance marker are stored in:

```text
%LOCALAPPDATA%\Monocle\LocalGroupNaming\<installation version>\
```

Later sessions reuse that installation without showing the agreement or downloading
again. While the menu item remains checked, groups created from the Monocle flyout are
named automatically and the local server remains running. The enabled state is not
retained between Dynamo sessions. Unchecking the item or closing Dynamo stops the server;
the downloaded files remain available for the next time the user enables it. The server
process is tied to the host with a kill-on-close job object, so a crash of Dynamo, Revit
or Civil 3D cannot leave it running.

## Pinned third-party components

The pins live in `LocalModelManifest.cs`. Every URL points at an immutable revision (a
Hugging Face commit, a llama.cpp release tag) and every download is checksum-verified, so
a re-uploaded "latest" cannot change what runs on the user's machine.

| Component | Pin | License | Download |
| --- | --- | --- | --- |
| Model | Qwen3-4B-Instruct-2507, Q4_K_M GGUF from `unsloth/Qwen3-4B-Instruct-2507-GGUF` | Apache-2.0 | 2.3 GiB |
| Runtime | llama.cpp `b12110` (the build behind the `v0.9.0` stable release), `llama-b12110-bin-win-cpu-x64.zip` | MIT | 24 MiB |

`docs/LOCAL_GROUP_NAMING_MODEL_RESEARCH.md` records the comparison that led to this pin
and the alternatives that were considered.

## Server configuration

`llama-server` is started with:

```text
-m <model> --host 127.0.0.1 --port <free port> -c 4096 -np 1 --jinja --reasoning off --no-ui --no-slots
```

- `-c 4096 -np 1`: one slot with a fixed context. Without an explicit size llama.cpp
  reserves the model's full training context (hundreds of thousands of tokens for recent
  models), which costs gigabytes of RAM for a prompt that never exceeds a few hundred tokens.
- `--reasoning off`: hybrid thinking models answer directly.
- No `-t`: the thread count is left to llama.cpp's physical-core default so laptops and
  workstations both get a sensible value. No `-ngl`: the CPU build ignores it.
- Requests use Qwen's published non-thinking sampling (`top_p 0.8`, `top_k 20`, `min_p 0`)
  with `temperature 0.3` (0.8 on the retry), `max_tokens 32`, `cache_prompt`, the grammar
  in `GroupNameGrammar.cs`, and `chat_template_kwargs.enable_thinking=false`. The last two
  are no-ops for Qwen3-4B-Instruct-2507, which has no thinking mode, but they make the same
  request work unchanged with hybrid models such as Granite 4.2 or MiniCPM5.

## Development

Automatic provisioning can be bypassed by setting both:

- `MONOCLE_LOCAL_AI_SERVER_PATH` (a `llama-server.exe`, b10075 or newer)
- `MONOCLE_LOCAL_AI_MODEL_PATH` (any GGUF model)

`MONOCLE_LOCAL_AI_SERVER_ARGS` appends extra server arguments, for example `-t 6`.

`tests/Monocle.LocalGroupNaming.Tests` covers the prompt, grammar, validator, node
descriptions, request shape, server arguments, manifest pinning, the naming queue, and the
provisioner against a simulated download server (interruption, resume, range-ignoring
servers, checksum failure, superseded installation cleanup):

```powershell
dotnet run --project tests/Monocle.LocalGroupNaming.Tests/Monocle.LocalGroupNaming.Tests.csproj --configuration Release
```

`tools/Monocle.LocalGroupNaming.Bench` runs the production naming path against any
runtime and model and prints titles and latencies for a fixed set of Dynamo groups
(`cases.json`). Use it to compare candidates before changing the pin:

```powershell
dotnet run --project tools/Monocle.LocalGroupNaming.Bench --configuration Release -- --server C:\path\llama-server.exe --model C:\path\model.gguf --server-args "-t 6" --out results.json
```

### Changing the pinned model or runtime

1. Pick the file on Hugging Face and read its exact size and SHA-256 without downloading:
   `curl -sI https://huggingface.co/<repo>/resolve/main/<file>` returns `x-linked-size`,
   `x-linked-etag` (the sha256) and `x-repo-commit`; `/api/models/<repo>/tree/main` lists
   the same values for every file.
2. For llama.cpp, read the release through the GitHub API; each asset carries a `digest`.
   Stable releases are the `vX.Y.Z` tags, whose `nightly-tag.txt` names the matching
   `bNNNN` build that actually has the binaries.
3. Update `LocalModelManifest.Current`, bump `installationVersion` (this re-prompts for the
   licenses and triggers cleanup of the previous installation), update this README and the
   research notes, and run the tests and the bench.
