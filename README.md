<h1 align="center">
  <br>
  <img src="!Documentation/monocleLogoRoundedSquare.png" alt="monocle" width="200">
  <br>
</h1>

<h3 align="center">A view extension for <a href="http://dynamobim.org/" target="_blank">Dynamo</a>.</h4>

![GitHub release (latest by date)](https://img.shields.io/github/v/release/johnpierson/monoclefordynamo?include_prereleases)
[![Maintenance](https://img.shields.io/badge/Maintained%3F-yes-green.svg)](https://github.com/johnpierson/MonocleForDynamo/graphs/commit-activity)
[![GitHub license](https://img.shields.io/github/license/johnpierson/MonocleForDynamo)](https://github.com/johnpierson/MonocleForDynamo/blob/master/LICENSE)


If you appreciate the work put into this free package, please vote in support of it on Dynamo's package manager. 

## Disclaimer
This tool is not affiliated with Autodesk and was written and provided in a personal capacity by the author. This tool is provided "as is" without warranty of any kind, express or implied. Use at your own risk. By using this open-source tool, you acknowledge that the developers and contributors are not liable for any damages, losses, or issues that may arise from its use. You are responsible for ensuring compliance with all applicable laws and regulations. Remember, open-source software relies on community contributions, so functionality and security might vary. Always review, understand, and, if necessary, modify the code to suit your specific needs.

## LICENSE
This code is licensed primarily under [BSD 3-Clause](https://github.com/johnpierson/MonocleForDynamo/blob/master/LICENSE) with a [Commons Clause License](https://commonsclause.com/) attached to that.

## Current Version
The canonical project contains configurations for Dynamo 2.0 through 2.19 (`net48`), Dynamo 3.0 through 3.6 (`net8.0-windows`), and Dynamo 4.0 through 4.2 (`net10.0-windows`). The Windows CI build matrix covers 2.19, 3.6, 4.1, and 4.2. These are build targets; Dynamo/Revit host smoke tests are still required for each supported host version.

## Build and Validation
Build on Windows with the .NET 8 and .NET 10 SDKs, the .NET Framework 4.8 targeting pack, and MSBuild. Restore and build a configuration with:

```powershell
msbuild src/Monocle/Monocle.sln /p:Configuration="Release 4.1" /p:Platform="Any CPU" /p:EnableDeploymentCopy=false /t:Restore,Build
```

The pure regression checks can run independently of Dynamo:

```powershell
dotnet run --project tests/Monocle.RegressionTests/Monocle.RegressionTests.csproj --configuration Release
```

The canonical project does not stage build output into `deploy` unless explicitly requested. For intentional local packaging, pass `/p:EnableDeploymentCopy=true` to the MSBuild command. The Revit `DynamoPackages.dll` reference is optional and defaults to the Revit 2022 install location; set `/p:DynamoPackagesPath="C:\path\to\DynamoPackages.dll"` when it is installed elsewhere. The separate `Monocle-net8.csproj` is retained as a legacy Dynamo 3.0 package project; its post-build copy is disabled by default and can be enabled with `/p:EnableLegacyPostBuildCopy=true /p:LegacyDynamoPackagePath="C:\path\to\MonocleViewExtension.dll"`.

## Known Issues
- When installing from the package manager for the first time, you may need to restart Dynamo.

## Contributors
This package is primarily managed by the author of http://designtechunraveled.com, and by [People Like You™](https://github.com/johnpierson/MonocleForDynamo/graphs/contributors).

## Updates
Since there is not currently an update notification process on Dynamo's package manager, I post notifications for updates on twitter.
[![](https://img.shields.io/twitter/follow/johntpierson.svg?label=Follow&style=social)](https://twitter.com/johntpierson)

Additionally,  updates come through in the extension menu now!

## Help improve Monocle
If you're interested in contributing to Monocle, just submit a [pull request](https://github.com/johnpierson/MonocleForDynamo/pulls) or a [feature request](https://github.com/johnpierson/MonocleForDynamo/issues) .

If submitting a pull request, keep in mind the monocle style guide below.

## monocle style guide

 <img src="https://github.com/johnpierson/MonocleForDynamo/raw/master/!Documentation/monocleStyleGuide.png" alt="style guide" width="1200">

## Installation
Monocle is available through Dynamo's built-in package manager. This provides the simplest installation for most users. If you are interested in manual install, there are some fabulous resources out there describing how to build your own Dynamo package.

## Thank you!

[![forthebadge](https://github.com/BraveUX/for-the-badge/blob/master/src/images/badges//designed-in-ms-paint.svg)](https://forthebadge.com)

[![forthebadge](https://github.com/BraveUX/for-the-badge/blob/master/src/images/badges//made-with-crayons.svg)](https://forthebadge.com)

