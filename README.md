[![Build status](https://ci.appveyor.com/api/projects/status/xk6smvvpr3wk9pru?svg=true)](https://ci.appveyor.com/project/dend/nue)

# nue

Tool to sort assemblies shipped in NuGet packages into their correct moniker folders.

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

![Folder Breakdown](nue.png)
