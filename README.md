# 📦 nue - the NuGet Package Extractor

![Build status](https://apidrop.visualstudio.com/_apis/public/build/definitions/97663bb1-33b9-48bf-ab0d-6ab65814469c/68/badge)

`nue` is a tool developed to extract NuGet packages into a folder structure that is compatible with the [docs.microsoft.com](https://docs.microsoft.com) .NET API documentation infrastructure.

It accepts a `*.csv` file as a source, and then relies on `nuget.exe` to install individual packages and collect their dependencies.

![Folder Breakdown](nue.png)

## To run

The core executable is `nue.exe`. It accepts the following parameters:

* `-p` or `--packages` - Path to package listing CSV file. See the structure for the file below.
* `-o` or `--output` - Output path. It's acceptable if the folder does not yet exist, as _nue_ will create one for you.
* `-f` or `--frameworks` - Framework for which to extract the packages. Use the [TFMs](https://docs.microsoft.com/en-us/nuget/schema/target-frameworks) reference to target folders in the `lib` folder of the main package.
* `-n` or `--nugetpath` - Path to `nuget.exe` when working in `le` (local extraction) mode. This can be downloaded on the [official NuGet page](https://www.nuget.org/downloads).

### Input CSV structure

When working with a list of packages, generally you need to follow the structure:

```text
{package_moniker_base},{package_ID},{version}
{package_moniker_base},{package_ID}
{package_moniker_base},{package_ID},{version}
```

The moniker will be assembled by combining the `{package_moniker_base}` and `{version}`, in this form: `{package_moniker}-{version}`, where `{version}` is available. When the `{version}` value is not specified, the `{package_moniker_base}` will become the moniker.

### Behavior

* When in the CSV, a version is specified after a package ID, that specific version will be installed and subsequently - processed.
* If no version is specified after the package ID, the latest available version for the package will be installed (_can be either stable or pre-release, depending on configuration_).

### Custom package configuration

In some cases, you might need to create custom package onboarding scenarios. To handle those, we are enabling custom parameters, that can be included before the package ID, in square brackets, as such:

```text
{package_moniker},[custom_parameter=value]{package_ID}
```

The following custom parameters are supported:

| Parameter | Description |
|:----------|:------------|
| `customSource` | URL to a custom feed for the package. |
| `tfm` | Overrides the global TFM for the specific package. |
| `altDep` | Alternative dependency TFM - helpful when you have a specific TFM for the core library, but a different TFM for dependency libraries. |
| `isPrerelease` | Required to install a pre-release package. |
