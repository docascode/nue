[![Build status](https://apidrop.visualstudio.com/_apis/public/build/definitions/97663bb1-33b9-48bf-ab0d-6ab65814469c/68/badge)

# 📦 nue - the NuGet Package Extractor

Tool to extract assemblies shipped in NuGet packages into their correct moniker folders. The primary use for this tool is generating managed reference documentation on [docs.microsoft.com](https://docs.microsoft.com).

![Folder Breakdown](nue.png)

## To Run

`nue.exe`

* `-m` or `--mode` - Operation mode. Should be either `extract` to get data from NuGet packages, `listpac` to list packages from a specific account, or `le` for local extraction from the hard drive.
* `-p` or `--packages` - Path to package listing CSV file. See the structure for the file below.
* `-o` or `--output` - Output path. It's acceptable if the folder does not yet exist, as _nue_ will create one for you.
* `-a` or `--account` - Account for which to list the packages. Account name should match that listed on [NuGet.org](https://nuget.org).
* `-f` or `--frameworks` - Framework for which to extract the packages. Use the [TFMs](https://docs.microsoft.com/en-us/nuget/schema/target-frameworks) reference to target folders in the `lib` folder of the main package.
* `-n` or `--nugetpath` - Path to `nuget.exe` when working in `le` (local extraction) mode. This can be downloaded on the [official NuGet page](https://www.nuget.org/downloads).

## Content

In `extract` mode, you will get the full set of packages in the `_pacman` folder in the directory specified in the output path. Additionally, you will get package output in individual folders specified by the moniker that you listed in the package listing CSV file.

### CSV Structure

When working with a list of packages, generally you need to follow the structure:

```
{package_moniker},{package_ID},{version_1},{version_2},...,{version_N}
{package_moniker},{package_ID},{version_1},{version_2},...,{version_N}
```
New packages should be on a new line.

### Custom Conventions

In some cases, you might need to create custom package onboarding scenarios - as an example, `nue` by default picks up all packages with one single TFM. To override the TFM on a per-package basis, it is possible to use the `[]` prefix, as such:

```
{package_moniker},[tfm=alternate_tfm]{package_ID},{version_1},{version_2},...,{version_N}
```
