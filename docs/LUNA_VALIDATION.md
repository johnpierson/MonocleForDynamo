# Luna audit validation

Validation was run on Windows on 2026-09-08 with the .NET 10.0.302 SDK. The full Dynamo/Revit UI host was not available, so host-only behavior remains in the smoke-test checklist.

## Repeatable checks

Run the pure checks with:

```powershell
dotnet run --project tests/Monocle.RegressionTests/Monocle.RegressionTests.csproj --configuration Release
```

Build a canonical configuration without deployment staging with:

```powershell
dotnet msbuild src/Monocle/Monocle.csproj /p:Configuration="Release 4.1" /p:Platform="Any CPU" /p:EnableDeploymentCopy=false /t:Restore,Build
```

The `CopyFiles` target is opt-in. Intentional local package staging uses `/p:EnableDeploymentCopy=true`; CI leaves it disabled.

## Results

- Pure regression project: passed, including Quick Save paths, documentation markdown/path helpers, FOCA code generation, settings validation/atomic write behavior, and read-only settings input.
- Canonical `Release 2.19`: restore and build passed; existing compatibility/obsolete warnings remain.
- Canonical `Release 3.6`: restore and build passed; existing Prism and framework-reference warnings remain.
- Canonical `Release 4.1`: restore and build passed; existing package and framework-reference warnings remain.
- Canonical `Release 4.2`: restore and build passed with the beta package range resolving to `4.2.0-beta5644`; existing package/framework warnings remain.
- Retained `Monocle-net8.csproj` (`Release net8`): restore and build passed with its legacy package copy disabled; existing framework, Prism, and obsolete API warnings remain.
- `MonocleLoader/MonocleExtension.csproj`: restore and build passed; existing `System.Windows.Interactivity` reference conflict warnings remain.
- Deployment safety: routine builds were run with deployment staging disabled; no tracked files under `deploy` were changed.

The no-restore check is target-specific because the shared assets file is regenerated for the selected configuration. Running no-restore after the `Release 4.2` restore produced `NETSDK1005` for `Release 2.19` and `Release 3.6`; configuration-specific restore followed by build passed for every target listed above. The warning counts are from the final restored builds and represent existing compatibility, framework-reference, package, and obsolete-API diagnostics.
