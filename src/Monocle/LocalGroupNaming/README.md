# Local group naming experiment

This experiment suggests a short title for one selected Dynamo group by running
`llama-server` and a GGUF model entirely on the local machine. No graph data is
sent to an external service.

The experiment does not commit model or runtime binaries. Place these files next
to `MonocleViewExtension.dll` using the following layout:

```text
local-ai/
  llama-server.exe
  llama.dll
  ggml*.dll
  Qwen3-4B-Q4_K_M.gguf
```

Copy the complete contents of the Windows CPU runtime archive into `local-ai`;
the exact supporting DLL list varies by `llama.cpp` release.

For the experiment, use the official Windows `llama.cpp` release and the
Apache-2.0-licensed 2.5 GB Q4_K_M model:

- <https://github.com/ggml-org/llama.cpp/releases>
- <https://huggingface.co/ggml-org/Qwen3-4B-GGUF/blob/main/Qwen3-4B-Q4_K_M.gguf>

For development, alternate paths can be supplied with:

- `MONOCLE_LOCAL_AI_SERVER_PATH`
- `MONOCLE_LOCAL_AI_MODEL_PATH`

Check **local group naming (local AI)** in the Monocle menu to load the model.
While that item remains checked, groups made with Monocle's existing
create-groups flyout are automatically named from every node they contain. The
local server stays running for subsequent groups and stops when the menu item is
unchecked or Dynamo closes.

The initial runtime is intentionally CPU-only with a 2,048-token context and four
threads. Model and runtime redistribution must be reviewed separately before
this moves beyond experimentation.
