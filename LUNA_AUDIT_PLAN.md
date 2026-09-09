# Repository audit and Luna implementation plan

Audit date: 2026-09-08. Baseline: `b01037b`.

## Scope and evidence

Read-only source audit of the extension entry point, settings, loader, build projects, and feature implementations. Findings below are established from source/control flow, not reproduced in a running Dynamo host. No production code was changed. The initial worktree was clean. No repository AGENTS.md was found; follow the instructions supplied in the task.

The .NET 10.0.302 SDK is installed. Read-only MSBuild evaluation of `Release 4.2` succeeded and resolved `net10.0-windows`. Full compilation, package restore, dependency vulnerability scanning, and Dynamo/Revit integration tests were not performed. There is no discovered automated test project or CI workflow; `test.ps1` is a machine-specific reflection probe, not a regression suite.

## Findings, ordered by priority

### F1 - P1: Node Swapper modifies the template on the first click

Evidence: `src/Monocle/NodeSwapper/NodeSwapperViewModel.cs:302`, especially lines 310 and 322. The first mouse-up selects `NodeToSwapTo` and advances `_currentStep` to 1. The next independent `if` executes immediately, selects the same node as `NodeToSwap`, and calls `OnSwapNode`, which deletes the original at line 286. Merely choosing the replacement template therefore changes the graph and leaves the template reference pointing at a removed node.

Also, `src/Monocle/NodeSwapper/NodeSwapperModel.cs:30` uses `HasSelection` followed by `Nodes.First(...)`. Note/group-only selections throw; the mouse handler calls selection outside its exception handler.

### F2 - P1: Documentation export can write to the previous destination

Evidence: `src/Monocle/NodeDocumentation/NodeDocumentationViewModel.cs:127`, `:151`, `:180`. The model captures the directory and full name when a node is selected. Changing the directory updates the view model but not that model. Export uses the stale model paths, while the existence check uses the new UI paths. Selecting directory A, selecting a node, then choosing B can still write or overwrite files in A.

### F3 - P1: Quick Save can reuse the original path

Evidence: `src/Monocle/BetterSave/BetterSaveModel.cs:43`. `originalName.Replace(".dyn", timestamp)` alters directory components containing `.dyn` and leaves an uppercase `.DYN` suffix untouched. The latter passes the original path to SaveAs instead of creating a timestamped copy; the former can point at a nonexistent directory. The exact overwrite/prompt behavior depends on the host SaveAs implementation.

### F4 - P2: Failed settings loading destroys the defaults it claims to preserve

Evidence: `src/Monocle/Utilities/Settings.cs:83`, `:111`. Loading clears the live dictionary before validating nullable sections, duplicate group IDs, and colors. A failure leaves empty or partially applied settings and is silently caught. Shutdown then persists that state. Separately, `src/Monocle/MonocleViewExtension.cs:112` skips loading when Shift is held, but `Shutdown` still saves defaults, potentially replacing the default settings file even though the extension was intentionally bypassed.

### F5 - P2: Paste Without Wires permanently changes the shared clipboard

Evidence: `src/Monocle/FancyPaste/FancyPasteModel.cs:41`, `:50`. The local variable aliases Dynamo's clipboard; removing connectors modifies the shared collection. A subsequent ordinary paste from the same copy loses its wires too.

### F6 - P2: Documentation export and reload fail on supported inputs

- `src/Monocle/NodeDocumentation/NodeDocumentationViewModel.cs:158`: `ExtendedDescription.Trim()` dereferences the initially null value, after graph/image output has already begun. Empty description is intended to use sample-only output.
- `src/Monocle/NodeDocumentation/NodeDocumentation.cs:30`: markdown parsing unconditionally indexes the second split segment. Sample-only markdown emitted by this feature has no `## In Depth` section, so it cannot be read back successfully.
- `src/Monocle/NodeDocumentation/NodeDocumentationModel.cs:71` and `:77`: replacing every `img` substring in the complete path changes directories and node names, not just the temporary image suffix. Combined export under a directory named `img` uses the wrong intermediate paths.

### F7 - P2: Enter or Tab with no search results throws

Evidence: `src/Monocle/SimpleSearch/SimpleSearchView.xaml.cs:66`, `:76`. With no selected result and an empty filtered collection, CurrentItem is null. PlaceNode dereferences it. Ordinary no-match searches can therefore raise an unhandled UI event exception.

### F8 - P2: FOCA's Revit element code block is entirely commented out

Evidence: `src/Monocle/Foca/FocaModel.cs:169`. The generated string places `//comment;Revit.Elements.ElementSelector.ByElementId(...)` on one line. The selector expression is part of the comment and does not execute. The categories branch already demonstrates a newline before executable code.

## Implementation sequence

Work in the order below. Each task is small enough for a separate reviewable change. Do not refactor unrelated features, change supported Dynamo versions, or regenerate deploy DLLs as part of bug fixes. Recheck line numbers against the current revision before editing.

### T0 - Establish repeatable validation

1. Inspect current git status and preserve unrelated work. Use a per-command safe.directory option if ownership prevents read-only git operations; do not alter global configuration unnecessarily.
2. Identify the canonical project (`src/Monocle/Monocle.csproj`, referenced by its solution). Treat `Monocle-net8.csproj` as a separate legacy project until its usage is established.
3. Add an explicit opt-in switch around the canonical project's `CopyFiles` target at line 612 before routine builds: builds currently write directly into tracked `deploy` folders. Preserve a documented packaging command for intentional deployment staging.
4. Record baseline builds for `Release 2.19`, `Release 3.6`, and `Release 4.1`; evaluate all advertised configurations and test `Release 4.2` separately. The 4.2 evaluated Dynamo package version is `4.2.0-beta5644.*`; verify that restore selects the intended package before changing it. Do not label it an invalid range without a restore result.
5. Add a small regression test project for pure path/settings/markdown logic as it is extracted by the tasks below. Avoid loading the full Dynamo UI in unit tests. Record host-dependent checks in a manual smoke-test checklist.

Acceptance: repeatable build commands, baseline failures recorded with exact errors, no unintentional changes under `deploy`, and at least one runnable regression test. If a host or dependency is unavailable, mark that check unverified and continue independent fixes.

### T1 - Correct Node Swapper state transitions (F1)

Use mutually exclusive state transitions or an explicit return after template selection. Preserve the special in-canvas activation path, which starts with a different step value. Guard against selecting the template itself as a target. Make note/group-only selection return no node through a deliberate selection guard, with existing UI feedback/cancellation behavior. Keep handler cleanup idempotent on cancellation/close.

Acceptance: first template click makes zero graph changes; the next target click performs exactly one replacement; the template remains usable for repeated swaps; note/group/empty selection does not throw; in-canvas initiation and Alt behavior still work. Verify wiring, grouping, and undo in a Dynamo host. Do not broaden into a new port-matching algorithm.

### T2 - Correct Quick Save path construction (F3)

Extract timestamped path construction using directory, filename-without-extension, and extension components. Keep the file in the original directory. Handle extension casing and ensure the result is different from the original. Validate the configured date format and resulting filename before writing; report invalid input. Preserve existing copy semantics for each supported host API branch.

Acceptance: tests for normal `.dyn`, uppercase `.DYN`, a directory containing `.dyn`, repeated dots, invalid format/filename output, and unchanged source path. Verify a host Quick Save produces a separate file and retains the active graph's intended identity.

### T3 - Apply settings atomically and respect bypass mode (F4)

Deserialize and validate into temporary values before assigning globals. Validate group IDs, required sections, colors, and supported numeric values; preserve defaults/current settings on failure and emit a useful diagnostic. Open settings for reading with explicit read access. Track successful initialization so Shift bypass does not save unloaded defaults. Use a temporary sibling file and safe replacement for settings writes so serialization failure cannot truncate the previous file.

Acceptance: tests for valid round-trip, duplicate IDs, missing sections, invalid colors, malformed XML, and read-only input. Failed loads leave prior settings unchanged. Shift-bypass launch/shutdown leaves the settings file byte-identical. Failed save preserves prior content and reports failure.

### T4 - Preserve the clipboard (F5)

Use a supported isolated paste payload if the host API permits it. Otherwise snapshot the shared collection, temporarily filter it, and restore its full contents/order in `finally`, including when Paste throws. Confirm collection identity requirements against the actual supported Dynamo API.

Acceptance: copy connected nodes, paste without wires, then ordinary paste; only the first result lacks wires. Test repeated paste and failure restoration. Verify undo affects pasted nodes without changing clipboard contents.

### T5 - Make documentation export internally consistent (F2, F6)

Build or synchronize the export model from current UI values immediately before validation/export. Use the same resolved paths for existence checks and writes. Remove null-unsafe trimming. Parse markdown with and without an in-depth section; handle a missing markdown sibling explicitly. Generate temporary image names with path APIs, touching only the filename suffix, and clean up only files this operation created. Validate all inputs before starting file output and surface actionable errors.

Acceptance: choose A/select node/change to B/export writes only to B; editing full node name updates all output paths. Null/empty description generates reloadable sample-only docs. Existing full-description docs still round-trip. Combined export works under an `img` directory and with `img` inside a node name. Missing markdown and failed image export produce clear feedback without falsely reporting success.

### T6 - Handle empty search and repair FOCA code generation (F7, F8)

For search, place a node only when a valid result exists; leave the popup usable when there are no matches. For FOCA, put the selector expression on its own line and normalize multiline comment text so it cannot turn into unintended executable source. Use the category branch as local precedent.

Acceptance: Enter/Tab with zero results causes no exception or node creation; populated search still places the chosen node. A generated Revit element code block evaluates to the selected element. Test code generation for single-line and multiline display text, then verify in a Revit-hosted Dynamo instance.

### T7 - Close the build and documentation gaps

After functional fixes, add Windows CI for the validated framework families and pure regression tests. Resolve the machine-specific Revit 2022 `DynamoPackages.dll` HintPath in the canonical and loader projects through a documented configurable dependency path or suitable package reference. Do not blindly upgrade packages. Decide whether the separate legacy net8 project is retained; it has a hard-coded user-profile post-build copy. Document actual tested compatibility and build prerequisites in README, whose current-version paragraph still describes Dynamo 2.17/2.19 despite project configurations through 4.2.

Acceptance: clean-checkout build instructions work on a configured Windows environment; CI does not publish or modify deployment artifacts; supported and tested versions are distinguished; all F1-F8 acceptance checks have results or explicit environment limitations.

## Deferred investigations, not confirmed active regressions

- `BetterSaveModel.MakeWorkspaceUnique` globally rewrites GUID-looking text throughout JSON, including user data and external identifiers. No menu/shortcut call site for `SaveWithNewGuids` was found. Do not expose this dormant command without a schema-aware ID mapping, backup/atomic output, and graph round-trip tests.
- The loader downloads a nonempty DLL from mutable `master/deploy` and checks only for file existence on later starts. Investigate pinned release/integrity metadata, unsupported-version reporting, offline behavior, and package reuse across host versions. A release design decision is needed before altering distribution behavior.
- Review extension-wide event subscription ownership and disposal with repeated workspace/window lifecycle tests. Existing StandardViews disposal is present; do not report it as missing.
- Floating Dynamo package references make restores depend on feed state. Consider tested exact versions/lock files after the supported version matrix is established. No vulnerability claim was made by this audit.

## Prompt to give Luna

Implement `LUNA_AUDIT_PLAN.md` in task order, starting with T0. Keep each fix focused and preserve existing Dynamo compatibility branches. Reproduce each finding with a regression test or documented host scenario, implement the fix, and record the validation result. Do not publish, commit regenerated binaries, expose dormant commands, or upgrade unrelated dependencies. Continue independent work when a host check is unavailable and report the precise limitation. Finish with changed files, completed task IDs, test/build results, and remaining checks. The audit is source-based; verify host-specific assumptions before treating them as proven runtime behavior.
