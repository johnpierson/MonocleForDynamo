# Luna host smoke-test checklist

The source and pure regression checks do not exercise Dynamo's graph, clipboard, WPF, or Revit runtime. Run these checks in a configured Dynamo/Revit host for each supported framework family and record the host version beside each result.

- [ ] T1 Node Swapper: first template click leaves the graph and template unchanged; the next target click performs one replacement; repeat swaps reuse the template; note/group/empty selections cancel without an exception; in-canvas and Alt behavior preserve their existing semantics; verify wires, grouping, and undo.
- [ ] T2 Quick Save: run Quick Save from a saved `.dyn` and uppercase `.DYN` workspace; confirm a timestamped sibling is created in the original directory and the active source workspace remains the intended graph.
- [ ] T4 Fancy Paste: copy connected nodes, use Paste Without Wires once, then ordinary paste from the same clipboard; confirm only the first result has wires removed and undo does not change clipboard contents; repeat after a forced paste failure if possible.
- [ ] T5 Documentation: choose directory A, select a node, change to directory B, and export; confirm every output is in B. Edit the full node name and confirm all output paths follow it. Export with an empty description and reload the sample-only markdown; reload a full-description document; test combined images when `img` occurs in a directory and node name; verify missing markdown and failed image export produce visible errors.
- [ ] T6 Simple Search: press Enter and Tab with zero results and confirm no exception or node; repeat with a matching result and confirm the chosen node is placed.
- [ ] T6 FOCA: convert a selected Revit element and confirm the generated code block evaluates to that element. Repeat with single-line and multiline display text and confirm the selector is executable code on its own line.
- [ ] T3 settings: launch normally and confirm valid settings load; launch with Shift held and shut down, then verify the settings file bytes are unchanged. Try malformed, duplicate-ID, missing-section, invalid-color, and read-only settings files and confirm prior live settings remain in effect.
