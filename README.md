# nue

Tool to sort assemblies shipped in NuGet packages into their correct moniker folders.

## To Run

`nue.exe PATH_TO_CSV_WITH_PACKAGES PATH_TO_OUTPUT_FOLDER PATH_TO_MAPPING_FILE`

* `PATH_TO_CSV_WITH_PACKAGES` - represents the path to the CSV file that contains a list of packages that need to be extracted. CSV should be in the form of _PACKAGE,VERSION_.
* `PATH_TO_OUTPUT_FOLDER` - where to dump the binaries.
* `PATH_TO_MAPPING_FILE` - path to CSV file that contains mapping of [TFMs](https://docs.microsoft.com/en-us/nuget/schema/target-frameworks) to target folder names in the form of _TFM,FOLDER_NAME_.

The output will contain a `_pacman` folder, that contains the full set of packages with the related metadata.

Every other folder in the output directory will contain TFM-based DLLs.

![Folder Breakdown](nue.png)