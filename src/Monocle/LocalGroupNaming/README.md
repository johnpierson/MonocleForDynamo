# Local group naming experiment

Monocle suggests a short title for groups created from its existing group
flyout by running `llama-server` and Qwen3 entirely on the local machine. Dynamo
node names are never sent to an external AI service.

## First-run setup

Check **local group naming (local AI)** in the Monocle menu. On the first use,
Monocle:

1. Shows the third-party licenses and requires separate acceptance of the
   llama.cpp MIT License and Qwen3 Apache License 2.0.
2. Downloads the pinned Windows CPU runtime and 2.5 GB Qwen3 4B Q4_K_M model.
3. Verifies the expected file size and SHA-256 checksums.
4. Starts the model on localhost after verification succeeds.

The files and the versioned acceptance marker are stored in:

```text
%LOCALAPPDATA%\Monocle\LocalGroupNaming\qwen3-4b-q4km-llama-b10075-v1\
```

Later sessions reuse that installation without showing the agreement or
downloading again. While the menu item remains checked, groups created from the
Monocle flyout are named automatically and the local server remains running.
The enabled state is not retained between Dynamo sessions. Unchecking the item
or closing Dynamo stops the server; the downloaded files remain available for
the next time the user enables it.

## Pinned third-party components

- llama.cpp b10075 Windows x64 CPU runtime (MIT):
  <https://github.com/ggml-org/llama.cpp/releases/tag/b10075>
- Qwen3-4B-Q4_K_M GGUF model (Apache-2.0):
  <https://huggingface.co/ggml-org/Qwen3-4B-GGUF/blob/main/Qwen3-4B-Q4_K_M.gguf>

The runtime is CPU-only with a 2,048-token context and four threads.

For development, automatic provisioning can be bypassed by setting both:

- `MONOCLE_LOCAL_AI_SERVER_PATH`
- `MONOCLE_LOCAL_AI_MODEL_PATH`
