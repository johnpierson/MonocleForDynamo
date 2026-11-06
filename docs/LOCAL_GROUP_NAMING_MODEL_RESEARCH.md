# Local group naming: model and runtime research

Research date: 2026-11-06. This note records how the model and llama.cpp build pinned in
`src/Monocle/LocalGroupNaming/LocalModelManifest.cs` were chosen, what else was considered,
and what remains unverified. It is meant to be re-run, not trusted forever: small local
models improve every few months, and `tools/Monocle.LocalGroupNaming.Bench` exists so the
next comparison can be measured instead of argued.

## The task the model has to do

Given one group's contents (node names such as `Element.GetParameterValueByName`, plus
details like `Categories: Doors`, `String: "Mark"`, the first line of a code block, or a
note), produce a three to seven word title that says what the group accomplishes. The
model runs on the CPU of whatever machine runs Revit or Dynamo, it is started on demand,
and the whole exchange is a few hundred prompt tokens and about ten output tokens. Decoding
is grammar-constrained, so output formatting is not something the model has to be good at;
what matters is instruction following, knowledge of Revit and Dynamo vocabulary, CPU speed
and a license that allows Monocle to download the weights for the user after a click-through.

## Summary

**Pinned: Qwen3-4B-Instruct-2507, Q4_K_M, from `unsloth/Qwen3-4B-Instruct-2507-GGUF`**
(2,497,281,120 bytes, Apache-2.0, released 2025-08-06). It is the non-thinking refresh of
the Qwen3-4B that PR #69 originally pinned: the same dense `qwen3` architecture and the same
byte size, so download cost and CPU speed do not change, but it has no thinking mode at all
(the chat template has no `enable_thinking` variable and the model never emits `<think>`
blocks). That removes the `/no_think` soft switch and the reasoning-stripping surface the
original experiment depended on. Its vendor-reported instruction-following and short-form
writing scores are the best in the 2.5 GB class (IFEval 83.4, Arena-Hard v2 43.4,
WritingBench 83.4 against 81.2, 9.5 and 68.5 for the original Qwen3-4B in non-thinking mode).

**Fallback: IBM Granite-4.1-3B** from the official `ibm-granite/granite-4.1-3b-GGUF` repo
(2,099,501,664 bytes, Apache-2.0, released 2026-04-29). The best 2026 release for this job:
no thinking mode exists, the GGUF is built by IBM's own CI, it is 16 percent smaller and a
little faster, and the pinned runtime already carries its chat template. It ranks second
because its general knowledge is weaker (vendor MMLU-Pro about 50 against about 70), which
can matter for recognising Revit and geometry vocabulary.

**Small tier: MiniCPM5-2B** from `openbmb/MiniCPM5-2B-GGUF` (1,561,318,368 bytes,
Apache-2.0). Hybrid thinking that `--reasoning off` disables reliably. No verified
candidate exists under 1.3 GB.

**Not chosen: Qwen3.5-4B.** It is the newest small Qwen (2026-03-02, Apache-2.0) and
scores higher on benchmarks in thinking mode, but it thinks by default, does not support
`/no_think`, is multimodal (dead weight for titling), is 10 percent larger, and its hybrid
Gated DeltaNet layers only have a basic CPU kernel in llama.cpp; users report about half
the CPU speed of Qwen3-4B. Gemma 4 E2B is now Apache-2.0 and thinking-off by default, but
its smallest Q4 GGUF is 2.84 GB and its llama.cpp speed work landed after b12110.

**Runtime: llama.cpp b12110**, the build the stable `v0.9.0` release (2026-11-04) names in
its `nightly-tag.txt`. Both top picks also run on the previous pin b10075.

## Decision matrix

Benchmark figures are vendor-reported numbers relayed by the research sweep; the
verification pass checked licenses, chat templates, file names, sizes, hashes and runtime
behaviour from primary sources but did not re-run benchmarks. CPU speed is third-party or
inferred and marked as such.

| Model | Params | License | GGUF (verified size) | Thinking control | Quality evidence (vendor) | CPU speed | Verdict |
| --- | --- | --- | --- | --- | --- | --- | --- |
| Qwen3-4B-Instruct-2507 (unsloth Q4_K_M) | 4.02B dense | Apache-2.0, verified, ungated | 2,497,281,120 B | None needed: non-thinking only | IFEval 83.4, Arena-Hard v2 43.4, WritingBench 83.4, MMLU-Pro 69.6 | Same as the incumbent; third-party 7 to 10 tok/s for 3 to 4B Q4 on older i5 laptops | **Pinned** |
| Granite-4.1-3B (IBM official Q4_K_M) | 3.40B dense | Apache-2.0, verified, ungated; no LICENSE file in the HF repos | 2,099,501,664 B | None needed: no thinking mode | IFEval 82.30, ArenaHard 37.8, MMLU-Pro 49.83 | About 15 to 20 percent fewer weights than Qwen; third-party 8.5 to 9 tok/s on an old i5 | Fallback |
| MiniCPM5-2B (OpenBMB official Q4_K_M) | 2.6B dense llama | Apache-2.0, verified, ungated | 1,561,318,368 B | Hybrid, on by default; `--reasoning off` or the template kwarg disables it | IFEval 86.7 (mode unspecified), MMLU-Pro 70.8 | Community: 9.1 tok/s on a 2-thread i3-6006U | Small tier |
| Granite-4.2-3B (IBM official Q4_K_M) | 3.66B dense | Apache-2.0, verified, ungated | 2,244,011,552 B | Hybrid, on by default; `--reasoning off` prefills an empty think block | Thinking-mode numbers only (IFBench 74.33); no-think quality unpublished | Similar to 4.1 | Watch list |
| Gemma 4 E2B-it (ggml-org QAT Q4_0) | 2.3B effective, 5.1B total | Apache-2.0, verified (Gemma Terms dropped), ungated | 2,841,481,184 B | Off by default | IFBench 38, MMLU-Pro 60.0; conflicting third-party IFEval | 2B-class per-token cost; large speed PRs landed after b12110 | Over the size target |
| Qwen3-4B hybrid (PR #69 incumbent) | 4.02B dense | Apache-2.0, verified | 2,497,280,640 B (ggml-org) | Hybrid; `/no_think` leaves an empty think block, the hard switch works | IFEval 81.2, Arena-Hard v2 9.5, WritingBench 68.5 (non-thinking) | Baseline | Superseded |
| Qwen3.5-4B (unsloth Q4_K_M) | 4.66B hybrid GDN plus vision | Apache-2.0 (metadata; not re-verified) | 2,740,937,888 B | On by default; only the template kwarg works | IFEval 89.8 thinking; non-thinking unpublished | Users report about half the speed of Qwen3-4B on CPU | Not recommended |
| Qwen3-4B-Thinking-2507 (unsloth Q4_K_M) | 4.02B dense | Apache-2.0, verified | 2,497,281,152 B | Thinking only | IFEval 87.4 with long traces | Same weights as Instruct-2507 | Fails the no-thinking constraint |

Excluded on license or quality grounds (sweep level, not individually verified): Gemma 3
4B and 1B (Gemma Terms with use-policy flow-down), Llama 3.2 3B (Llama Community License),
LFM3-1.7B and LFM2.5 (LFM Open License with a revenue cap), Hunyuan 4B and Youtu 2B
(territorial exclusions), Qwen2.5-3B (Qwen Research License), EXAONE 4.0 (non-commercial),
Phi-4-mini-instruct (MIT, but February 2025 and IFEval 70.1), Qwen3.5-2B and 0.8B
(non-thinking IFEval 61.2 and 52.1), Nemotron 3 Nano 4B (custom NVIDIA license, 2.84 GB),
Ministral 3 3B (Apache-2.0 but no quality edge over the picks at 2.15 GB), SmolLM3-3B
(2025, hybrid thinking, weaker numbers).

## Candidate notes

### Qwen3-4B-Instruct-2507 (pinned)

- License: Apache-2.0. The official `Qwen/Qwen3-4B-Instruct-2507` repository is tagged
  `apache-2.0` and ships the unmodified Apache License 2.0 text; the QwenLM README states
  that all open-weight Qwen models are Apache 2.0. The unsloth GGUF repo is tagged
  `apache-2.0` and the GGUF metadata embeds `general.license=apache-2.0`. The bartowski and
  lmstudio-community mirrors carry no license field in their metadata, which is why the
  click-through links the official Qwen LICENSE rather than a mirror page.
- Thinking: none. The model card states it supports only non-thinking mode and does not
  generate `<think>` blocks; no revision of its chat template references `enable_thinking`,
  and the generation prompt is a bare assistant turn. The GGUF embeds Qwen's 2025-08-05
  template revision, which still contains a history-only block for re-wrapping prior
  turns' reasoning; Qwen removed that block on 2025-09-17 without the GGUFs being
  re-converted. It never fires for single-turn prompting.
- File: `Qwen3-4B-Instruct-2507-Q4_K_M.gguf` at commit
  `a06e946bb6b655725eafa393f4a9745d460374c9`, 2,497,281,120 bytes, SHA-256
  `3605803b982cb64aead44f6c1b2ae36e3acdb41d8e46c8a94c6533bc4c67e597` (Hugging Face LFS
  metadata, confirmed by a HEAD request on the pinned URL). Equal-standing alternative:
  `bartowski/Qwen_Qwen3-4B-Instruct-2507-GGUF`, `Qwen_Qwen3-4B-Instruct-2507-Q4_K_M.gguf`,
  2,497,280,736 bytes, SHA-256 `2fde00ce69dd4899c70d020845e2638353015bba0fdf161b3eb965f2bca4464e`.
  ggml-org publishes only a 4.28 GB Q8_0 of this model.
- Sampling (model card and `generation_config.json`): temperature 0.7, top_p 0.8, top_k 20,
  min_p 0; presence_penalty only to curb repetition. Monocle sends those nucleus settings
  with temperature 0.3 for steadier titles and 0.8 on the retry; `min_p` is sent explicitly
  because llama.cpp's default is non-zero.
- Sources: https://huggingface.co/Qwen/Qwen3-4B-Instruct-2507 ,
  https://huggingface.co/Qwen/Qwen3-4B-Instruct-2507/blob/main/LICENSE ,
  https://github.com/QwenLM/Qwen3/blob/main/README.md ,
  https://huggingface.co/unsloth/Qwen3-4B-Instruct-2507-GGUF ,
  https://huggingface.co/bartowski/Qwen_Qwen3-4B-Instruct-2507-GGUF

### Granite-4.1-3B (fallback)

- License: Apache-2.0, stated in the model card metadata and README, the IBM Research blog
  and the HF technical blog of 2026-04-29, and the GitHub `granite-4.1-language-models`
  LICENSE file. Neither HF repository ships a LICENSE or NOTICE file, so a Monocle pin would
  have to carry its own copy of the Apache-2.0 text plus an attribution line.
- Thinking: none. The chat template contains no think or reasoning logic; the generation
  prompt is `<|start_of_role|>assistant<|end_of_role|>`. Do not confuse it with Granite
  4.2-3B, which thinks by default.
- File: `ibm-granite/granite-4.1-3b-GGUF`, `granite-4.1-3b-Q4_K_M.gguf`, commit
  `ab4701481089b58a082ef63cc1cee738887293ff`, 2,099,501,664 bytes, SHA-256
  `662b0626cd58f443baea23559b469df6576a81d349649c59413b36a9fb32eb29` (the Ollama registry
  blob for `granite4.1:3b` has the same hash). Built by IBM's GGUF workflow on 2026-04-16
  without imatrix; 3,402,836,480 parameters; `bos` and `eos` are `<|end_of_text|>`.
- Runtime: llama.cpp b10075 already contains the Granite 4.1 chat template support
  (PR #23518, 2026-05-28) and uses this file as a worked example in its function-calling docs.
- Sampling: IBM publishes none; greedy or temperature at or below 0.3 with top_p 0.9.
- Sources: https://huggingface.co/ibm-granite/granite-4.1-3b ,
  https://huggingface.co/ibm-granite/granite-4.1-3b-GGUF ,
  https://research.ibm.com/blog/granite-4-1-ai-foundation-models ,
  https://huggingface.co/blog/ibm-granite/granite-4-1 ,
  https://github.com/ibm-granite/granite-4.1-language-models/blob/main/LICENSE ,
  https://github.com/ggml-org/llama.cpp/pull/23518

### MiniCPM5-2B (small tier)

- License: Apache-2.0 (card metadata, README statement, GitHub LICENSE); ungated; no
  LICENSE file in the HF repos.
- Thinking: hybrid. The template emits an empty think block when `enable_thinking` is
  false and thinks when it is undefined, so llama-server must run with `--reasoning off`
  (which Monocle already passes) or receive the template kwarg per request (which Monocle
  already sends). Needs llama.cpp with the `minicpm5` pre-tokenizer (PR #23384, 2026-05-27);
  b10075 and b12110 both qualify.
- File: `openbmb/MiniCPM5-2B-GGUF`, `MiniCPM5-2B-Q4_K_M.gguf`, commit
  `2079a22f3beaa4e306449978533478fe0522f4b3`, 1,561,318,368 bytes, SHA-256
  `ec2d5801640099e97d8d7e8003ad4d81f336e757811f03a26173dddf386602fd`.
- Sampling: temperature 1.0, top_p 0.95, min_p 0.0 explicitly (OpenBMB staff attribute
  repetition loops to llama.cpp's non-zero default min_p).
- Sources: https://huggingface.co/openbmb/MiniCPM5-2B ,
  https://huggingface.co/openbmb/MiniCPM5-2B-GGUF ,
  https://github.com/ggml-org/llama.cpp/pull/23384

### Granite-4.2-3B, Gemma 4 E2B, Qwen3.5-4B

- Granite-4.2-3B: Apache-2.0, official GGUF (2,244,011,552 bytes, commit `c40945d7...`,
  SHA-256 `e0406663...`). Thinking is on by default and switched off cleanly by
  `--reasoning off`, but IBM publishes only thinking-mode benchmarks, so title quality
  without thinking is unknown. IBM re-uploaded all GGUFs on 2026-09-02 with new hashes, a
  reminder to pin commits rather than `main`.
- Gemma 4 E2B-it: the first Gemma under plain Apache-2.0 (the license link resolves to the
  verbatim Apache text and the repos are ungated). Thinking is off by default. The
  ggml-org `gemma-4-E2B-it-Q4_0.gguf` (2,841,481,184 bytes, SHA-256 `8e30dff3...`) is
  converted from Google's QAT checkpoint, which argues for its quality, but it is 14 percent
  over the 2.5 GB target, carries multimodal weights that titling never uses, and its large
  prompt-processing optimisations landed in llama.cpp after b12110.
- Qwen3.5-4B: Apache-2.0 per metadata, 2,740,937,888 bytes at Q4_K_M. Thinking on by
  default with no soft switch; hybrid Gated DeltaNet architecture whose chunked CPU kernel
  did not exist as of the research date (basic vector implementation only), with users
  measuring about half the CPU speed of Qwen3-4B; natively multimodal. It was the
  provisional pin on this branch before the research completed and was replaced.

## Runtime: llama.cpp

- llama.cpp changed its release scheme on 2026-08-21. Every `bNNNN` build is now published
  as a GitHub prerelease, and stable versions are tagged `vX.Y.Z`; the GitHub release for a
  stable tag carries a single `nightly-tag.txt` asset naming the `bNNNN` build it
  corresponds to. `v0.9.0` (2026-11-04) maps to **b12110**, which is the build Monocle pins.
  `GET /releases/latest` therefore returns a release with no binaries; an updater must
  resolve the stable tag first, and build numbers have gaps (b10565, b12010 and b12011 do
  not exist).
- The Windows CPU x64 asset is `llama-b12110-bin-win-cpu-x64.zip` (24,708,445 bytes,
  SHA-256 `98901e327c252875accaf2d1bf364e10e868843b0b63468baad1aa945cdc5da2` from the
  GitHub releases API `digest` field; no checksum file is published). The zip is flat:
  `llama-server.exe` is a thin launcher that needs `llama-server-impl.dll` and the
  `ggml-cpu-*.dll` backends beside it, so the installer copies the whole directory. The
  previous pin b10075 (2026-07-20) shipped the same layout and also runs both top picks, so
  the model change alone would not have forced a runtime bump.
- Server facts that shaped the implementation (verified against the server README at
  b12174, the b10075 `--help` output on this machine, and llama.cpp source): `--jinja` is on
  by default; `--reasoning off` is the recommended way to disable hybrid thinking
  (`--reasoning-budget 0` now means "force the end-of-thinking tag", not "disable"); the
  per-request `chat_template_kwargs: {"enable_thinking": false}` is accepted and must be a
  JSON boolean; `grammar` and `json_schema` are accepted on `/v1/chat/completions` but never
  together; `-np` defaults to auto (4 slots sharing a unified KV cache), so `-np 1` makes
  `-c` unambiguous; `-c 0` means the model's training context (40,960 tokens for Qwen3-4B),
  so an explicit small context is essential on CPU; the default port moved from 8080 to
  9931; `/health` returns 503 while loading and 200 when ready; `--no-webui` is a deprecated
  alias of `--no-ui`; the thread default is the physical core count; `-ngl` is ignored by
  CPU builds; `--mlock` and `--no-mmap` were removed in b10875; Windows executables since
  b12010 embed a UTF-8 manifest, with a further non-ASCII path cleanup in b12131 and b12132.

## Open questions

1. No CPU tokens-per-second measurement exists for any candidate on representative AEC
   hardware. The bench tool should be run on at least one 4-core laptop before this pin is
   considered final; it reports startup time and per-title latency.
2. Title quality on real Dynamo node lists has not been compared blind. Vendor IFEval is a
   proxy; a run of the bench cases against Qwen3-4B-Instruct-2507 and Granite-4.1-3B would
   settle the primary versus fallback order.
3. Confirm on b12110 that the 2025-08-05 Qwen template renders plain content under the
   server's reasoning auto-detection and that the grammar path behaves after the b11531
   chat refactor (expected from source reading, not executed).
4. Non-ASCII `%LOCALAPPDATA%` paths on b12110 should be tested with a non-ASCII user profile.
5. Granite-4.2-3B could displace 4.1 as the fallback if IBM publishes non-thinking numbers.
6. Download behaviour behind corporate proxies and Hugging Face CDN redirects for a 2.5 GB
   file (including whether the proxy honours range requests) was not evaluated.

## Methodology

1. Sweep: five independent web research passes (frontier small models, the Qwen family,
   non-Qwen vendors and their license terms, CPU inference and quantization evidence, the
   llama.cpp runtime), followed by a completeness critic that searched for 2026 releases the
   sweep had missed (it confirmed that Qwen3.6 and 3.8 have no small dense sizes, that no
   Llama, Phi or SmolLM small release appeared in 2026, and added Gemma 4 QAT and MiniCPM5).
2. Verification: one skeptical verifier per shortlisted model reading the Hugging Face
   model card, license file, raw chat template and GGUF repository (sizes and hashes via the
   API and HEAD requests on commit-pinned URLs), plus one for the runtime facts against the
   GitHub releases API and llama.cpp source at b10075, b12110 and master. Corrections the
   verifiers made to the sweep are folded into the notes above.
3. Ground truth gathered directly (Appendix A): exact GGUF sizes and LFS SHA-256 values from
   `https://huggingface.co/api/models/<repo>/tree/main`, repository commits for immutable
   `/resolve/<commit>/` URLs, and release asset digests from the GitHub API.
4. Not done: a local benchmark. Downloading candidate models (1.5 to 3 GB each) and running
   them needs the user's go-ahead; the bench tool and its case set are ready for that step.

## Appendix A: exact file facts (collected 2026-11-06)

Sizes and hashes come from the Hugging Face tree API (`lfs.oid` is the SHA-256) and the
GitHub releases API. Commits are the repository revisions at the time of collection; a
download URL of the form `https://huggingface.co/<repo>/resolve/<commit>/<file>` is immutable.

| Repository (commit) | File | Bytes | SHA-256 | License tag |
| --- | --- | ---: | --- | --- |
| `unsloth/Qwen3-4B-Instruct-2507-GGUF` (`a06e946bb6b655725eafa393f4a9745d460374c9`) | `Qwen3-4B-Instruct-2507-Q4_K_M.gguf` (pinned) | 2,497,281,120 | `3605803b982cb64aead44f6c1b2ae36e3acdb41d8e46c8a94c6533bc4c67e597` | apache-2.0 |
| `unsloth/Qwen3-4B-Instruct-2507-GGUF` | `Qwen3-4B-Instruct-2507-Q6_K.gguf` | 3,306,261,600 | `cd7b21b38b3e71400587c184b6a9b04d3beb4d13fdae6464d4075dee4f1bc5ad` | apache-2.0 |
| `bartowski/Qwen_Qwen3-4B-Instruct-2507-GGUF` (`ae44f08e1392f39c0e474af10c3ff8355c8b6688`) | `Qwen_Qwen3-4B-Instruct-2507-Q4_K_M.gguf` | 2,497,280,736 | `2fde00ce69dd4899c70d020845e2638353015bba0fdf161b3eb965f2bca4464e` | (none) |
| `lmstudio-community/Qwen3-4B-Instruct-2507-GGUF` (`4edb920b6f14e3b9284d4502a6485103d72cde05`) | `Qwen3-4B-Instruct-2507-Q4_K_M.gguf` | 2,497,280,448 | `8cdb57cbb880d313736a9bc4e3d3d2485f145b5e19cf33783746e753e82641fc` | (none) |
| `ggml-org/Qwen3-4B-GGUF` (`2f3b082b1356a6123f7ed71e65aea340da25d53c`) | `Qwen3-4B-Q4_K_M.gguf` (PR #69 incumbent) | 2,497,280,640 | `ab27b9bfa375a178d6cba48f3ad892b94b7739659dcc7aae8058ce0ffed6b328` | apache-2.0 |
| `ibm-granite/granite-4.1-3b-GGUF` (`ab4701481089b58a082ef63cc1cee738887293ff`) | `granite-4.1-3b-Q4_K_M.gguf` (fallback) | 2,099,501,664 | `662b0626cd58f443baea23559b469df6576a81d349649c59413b36a9fb32eb29` | apache-2.0 |
| `ibm-granite/granite-4.1-3b-GGUF` | `granite-4.1-3b-Q5_K_M.gguf` | 2,437,012,064 | `f7724d259f29b0edf147144ac530ca26f91c97af8274249f933073c461678a3c` | apache-2.0 |
| `openbmb/MiniCPM5-2B-GGUF` (`2079a22f3beaa4e306449978533478fe0522f4b3`) | `MiniCPM5-2B-Q4_K_M.gguf` (small tier) | 1,561,318,368 | `ec2d5801640099e97d8d7e8003ad4d81f336e757811f03a26173dddf386602fd` | apache-2.0 |
| `ibm-granite/granite-4.2-3b-GGUF` (`c40945d71cd90f249a56985e8155551a9188dc30`) | `granite-4.2-3b-Q4_K_M.gguf` | 2,244,011,552 | `e0406663965846ae22a403456eb826ccce5f450840491f71952f18a7cb78e7d5` | apache-2.0 |
| `ggml-org/gemma-4-E2B-it-GGUF` (`6938c58e698502037401a5f19f6905f2fb2f7dee`) | `gemma-4-E2B-it-Q4_0.gguf` | 2,841,481,184 | `8e30dff3ac4c8434c49a7036fa15564bdbb6044e42bf04550bf1a096ad7e6a52` | apache-2.0 |
| `unsloth/gemma-4-E2B-it-GGUF` (`0314792d7f1f7e229411f620751375812bb9faf2`) | `gemma-4-E2B-it-Q4_K_M.gguf` | 3,106,738,272 | `740185b21d22ceb83a11c3aa62ad5842ef32c70f6096d756bbee85a1e4ec34b8` | apache-2.0 |
| `ggml-org/gemma-4-E4B-it-GGUF` (`a69f018d7678a2b9f5e705f8903d6fb29b9b25dc`) | `gemma-4-E4B-it-Q4_0.gguf` | 4,590,807,392 | `a555b900214b477d8880e7832e0b8925e139b0159640036b09fe472b6f2097f2` | apache-2.0 |
| `unsloth/Qwen3.5-4B-GGUF` (`e87f176479d0855a907a41277aca2f8ee7a09523`) | `Qwen3.5-4B-Q4_K_M.gguf` | 2,740,937,888 | `00fe7986ff5f6b463e62455821146049db6f9313603938a70800d1fb69ef11a4` | apache-2.0 |
| `unsloth/Qwen3.5-4B-GGUF` | `Qwen3.5-4B-IQ4_XS.gguf` | 2,477,053,088 | `658a9e7e406deb06d0179755e3c14f6a82915a4be4962a2f92a64d948d2e572f` | apache-2.0 |
| `unsloth/Qwen3.5-2B-GGUF` (`f6d5376be1edb4d416d56da11e5397a961aca8ae`) | `Qwen3.5-2B-Q6_K.gguf` | 1,574,961,408 | `fc90339420b4298887aafb307a4291c55440b730133bbffe6ba9630503dcb548` | apache-2.0 |
| `unsloth/Qwen3.5-2B-GGUF` | `Qwen3.5-2B-Q8_0.gguf` | 2,012,012,800 | `1b04acba824817554f4ce23639bc8495ff70453b8fcb047900c731521021f2c1` | apache-2.0 |
| `ggml-org/Qwen3.5-0.8B-GGUF` (`807dfd9a6fadbf703207aac3bfb0b2b3040d077c`) | `Qwen3.5-0.8B-Q8_0.gguf` | 833,592,096 | `37ae482d336108d23516fa35e8e0c4126688d81018b87178a18d752a1357814f` | apache-2.0 |
| `unsloth/Qwen3-4B-Thinking-2507-GGUF` (`f40adb104d4d44aee52f398b60597c5866a973a3`) | `Qwen3-4B-Thinking-2507-Q4_K_M.gguf` | 2,497,281,152 | `ddd52e18200baab281c5c46f70d544ce4d4fe4846eab1608f2fff48a64554212` | apache-2.0 |
| `mistralai/Ministral-3-3B-Instruct-2512-GGUF` (`eb599d408350ea2bb60452cb86be7c7b2fc28227`) | `Ministral-3-3B-Instruct-2512-Q4_K_M.gguf` | 2,147,023,008 | `9ed150d4367e68df0ac8e1540f6ddc65b42d0ee26378329d1ecbca60f93fc5f8` | apache-2.0 |
| `unsloth/NVIDIA-Nemotron-3-Nano-4B-GGUF` (`8e81be55c5aa3d63bb82b6ceec62d50805d9e1bb`) | `NVIDIA-Nemotron-3-Nano-4B-Q4_K_M.gguf` | 2,900,295,712 | `e515c9ceb10ae503db22a201fade92167f757510d1247a65987f8b6ae9e296e7` | nvidia-nemotron-open-model-license |
| `LiquidAI/LFM3-1.7B-GGUF` (`cd0c74c575b5c416f3011b5b4c298077f26ca8c2`) | `LFM3-1.7B-Q8_0.gguf` | 1,846,238,496 | `8f6b24182258205e9e01e21f1f24a1f208c1e8115578825d202ce39976af20b6` | lfm1.0 |
| `LiquidAI/LFM2.5-1.2B-Instruct-GGUF` (`8ed288026e23958ad9dfa92d53ed773a8eee7125`) | `LFM2.5-1.2B-Instruct-Q8_0.gguf` | 1,246,253,888 | `f6b981dcb86917fa463f78a362320bd5e2dc45445df147287eedb85e5a30d26a` | lfm1.0 |
| `bartowski/HuggingFaceTB_SmolLM3-3B-GGUF` (`86b3536ed1ca0dcb4716745642db4e6804bf3d32`) | `HuggingFaceTB_SmolLM3-3B-Q4_K_M.gguf` | 1,915,305,792 | `519732558d5fa7420ab058e1b776dcfe73da78013c2fe59c7ca43c325ef89132` | apache-2.0 |
| `unsloth/Phi-4-mini-instruct-GGUF` (`78eb92a46fc37e6b524df991ed9aca9bc6aa7b80`) | `Phi-4-mini-instruct-Q4_K_M.gguf` | 2,491,874,272 | `88c00229914083cd112853aab84ed51b87bdf6b9ce42f532d8c85c7c63b1730a` | mit |

llama.cpp release assets (GitHub releases API `digest`):

| Release | Asset | Bytes | SHA-256 |
| --- | --- | ---: | --- |
| b12110 (= v0.9.0, 2026-11-04, pinned) | `llama-b12110-bin-win-cpu-x64.zip` | 24,708,445 | `98901e327c252875accaf2d1bf364e10e868843b0b63468baad1aa945cdc5da2` |
| b12110 | `llama-b12110-bin-win-vulkan-x64.zip` | 37,661,146 | `9732a5e48887ca54d0bfe4af4324dc85fd56cd8cda19b52df3542fa6ebe6874f` |
| b12174 (2026-11-06) | `llama-b12174-bin-win-cpu-x64.zip` | 24,746,357 | `b2ec458994bacf7e837dfc54e98152ea4bea97779014c858d98840d050f349d6` |
| b10075 (2026-07-20, previous pin) | `llama-b10075-bin-win-cpu-x64.zip` | 18,013,666 | `67ccd320365193e5fa5e2778773a30ee3fc19802b2a9f324023641d160a1e802` |

Model repositories that do not exist under the names one might guess (the Hugging Face API
returned 401 for them on 2026-11-06): `ggml-org/Qwen3.5-4B-GGUF`, `ggml-org/Qwen3.5-2B-GGUF`,
`ggml-org/Qwen3-4B-Instruct-2507-GGUF` (only a Q8_0-only repo exists), `Qwen/Qwen3.5-4B-GGUF`.
Qwen stopped publishing its own GGUF repositories after the Qwen3 and Qwen3-VL series.
