using Microsoft.VisualBasic.FileIO;
using Nue.Interfaces;
using Nue.Models;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using SearchOption = System.IO.SearchOption;

namespace Nue.Core
{
    public class Extractor
    {
        public async static Task<bool> DownloadPackages(string packagePath, string outputPath, string targetFramework)
        {
            if (string.IsNullOrWhiteSpace(packagePath) || string.IsNullOrWhiteSpace(outputPath) ||
                string.IsNullOrWhiteSpace(targetFramework)) return false;

            var packages = GetPackagesFromFile(packagePath);

            foreach (var package in packages)
            {
                // Package resolver that will be used to get the full path to binaries.
                IPackageResolver resolver = null;

                if (package.CustomPropertyBag != null)
                {
                    // Check if we have a custom resolver.
                    if (package.CustomPropertyBag.ContainsKey("resolver"))
                    {
                        // Determine the right resolver.
                        var expectedResolver = package.CustomPropertyBag["resolver"];
                        switch(expectedResolver)
                        {
                            case "xbl":
                                {
                                    // Xbox Live-style resolver.
                                    resolver = new DefaultResolver();
                                    break;
                                }
                            default:
                                {
                                    resolver = new DefaultResolver();
                                    break;
                                }
                        }
                    }
                    else
                    {
                        resolver = new DefaultResolver();
                    }
                }
                else
                {
                    package.CustomPropertyBag = new Dictionary<string, string>();
                    resolver = new DefaultResolver();
                }

                // Inject the TFM into the resolver if none was specified for the package.
                if (!package.CustomPropertyBag.ContainsKey("tfm"))
                {
                    package.CustomPropertyBag.Add("tfm", targetFramework);
                }

                var binaries = await resolver.CopyBinarySet(package,outputPath);

                try
                {
                    ConsoleEx.WriteLine($"Deleting {Path.Combine(outputPath, "_pacman")}", ConsoleColor.Red);
                    Helpers.DeleteDirectory(Path.Combine(outputPath, "_pacman"));
                }
                catch
                {
                    Helpers.DeleteDirectory(Path.Combine(outputPath, "_pacman"));
                }
            }

            return true;
        }

        private static List<PackageAtom> GetPackagesFromFile(string packagePath)
        {
            var packages = new List<PackageAtom>();

            using (var parser = new TextFieldParser(packagePath))
            {
                parser.TextFieldType = FieldType.Delimited;
                parser.SetDelimiters(",");

                while (!parser.EndOfData)
                {
                    var fields = parser.ReadFields();

                    if (fields == null) continue;

                    // Given the conventions, let's find out how many versions are requested to be downloaded.
                    var requestedVersions = fields.Length - 2;

                    if (requestedVersions < 1) continue;
                    for (var i = 2; i < 2 + requestedVersions; i++)
                    {
                        var pAtom = new PackageAtom
                        {
                            Moniker = fields[0] + "-" + fields[i],
                            MonikerBase = fields[0],
                            Name = fields[1],
                            Version = fields[i]
                        };

                        // Property bag will be formatted like:
                        // [property1=value1,property2=value2]PackageId
                        var propertyBagRegex = @"(\[.+\])";
                        Regex formalizedRegEx = new Regex(propertyBagRegex);
                        var match = formalizedRegEx.Match(pAtom.Name);

                        if (match.Success)
                        {
                            // There seems to be a property bag attached to the name.
                            var rawPropertyBag = match.Value.Replace("[","").Replace("]","").Trim();
                            if (!string.IsNullOrWhiteSpace(rawPropertyBag))
                            {
                                // Normalize the package name without the property bag.
                                pAtom.Name = pAtom.Name.Replace(match.Value, "");
                                pAtom.CustomPropertyBag = new Dictionary<string, string>();

                                // Avoiding the case of empty property bag, looks like in this case we are good.
                                var properties = rawPropertyBag.Split(new char[] { ';' });
                                foreach(var property in properties)
                                {
                                    var splitProperty = property.Split(new char[] { '=' });
                                    pAtom.CustomPropertyBag.Add(splitProperty[0], splitProperty[1]);
                                }
                            }
                        }

                           packages.Add(pAtom);
                    }
                }
            }

            return packages;
        }

        public static bool ExtractLocalPackages(string outputPath, string packageList, string nuGetPath, string targetFramework, string packageSource)
        {
            var packages = GetPackagesFromFile(packageList);

            foreach (var package in packages)
            {
                Directory.CreateDirectory(outputPath + "\\_pacman");

                ConsoleEx.WriteLine($"Attempting to install: {package.GetFullName()}. Installing...",
                    ConsoleColor.Yellow);

                // Execute a local NuGet install
                String command = $"{nuGetPath}\\nuget.exe";
                ProcessStartInfo cmdsi = new ProcessStartInfo(command);
                cmdsi.UseShellExecute = false;
                cmdsi.Arguments = $"install {package.Name} -Version {package.Version} -Source {packageSource} -OutputDirectory {outputPath + "\\_pacman"} -Verbosity Detailed -DisableParallelProcessing -FallbackSource https://api.nuget.org/v3/index.json";

                Console.WriteLine($"Package source: {packageSource}");

                Process cmd = Process.Start(cmdsi);
                cmd.WaitForExit();

                ConsoleEx.WriteLine($"Getting data for {package.Name}...", ConsoleColor.Yellow);

                ConsoleEx.WriteLine("Downloaded package and dependencies.", ConsoleColor.Green);

                var packageFqn = package.Name + "." + package.Version;
                var pacManPackagePath = outputPath + "\\_pacman\\" + packageFqn;
                var pacManPackageLibPath = pacManPackagePath + "\\lib";
                var packageContainerPath = Path.Combine(outputPath, package.Moniker);

                if (Directory.Exists(pacManPackageLibPath))
                {
                    var directories = Directory.GetDirectories(pacManPackageLibPath);
                    var dependencyFolders = from c in Directory.GetDirectories(outputPath + "\\_pacman")
                                            where Path.GetFileName(c).ToLower() != packageFqn.ToLower()
                                            select c;

                    var closestFolder = Helpers.GetBestLibMatch(targetFramework, directories);

                    var frameworkIsAvailable = !string.IsNullOrWhiteSpace(closestFolder);

                    if (frameworkIsAvailable)
                    {
                        var binaries = Directory.GetFiles(closestFolder,
                            "*.dll",
                            SearchOption.TopDirectoryOnly);
                        var docFiles = Directory.GetFiles(closestFolder,
                            "*.xml",
                            SearchOption.TopDirectoryOnly);

                        // Make sure to only go through any processing if we found binaries.
                        if (binaries.Any())
                        {
                            Directory.CreateDirectory(packageContainerPath);


                            foreach (var binary in binaries)
                                File.Copy(binary, Path.Combine(packageContainerPath, Path.GetFileName(binary)), true);

                            foreach (var docFile in docFiles)
                                File.Copy(docFile, Path.Combine(packageContainerPath, Path.GetFileName(docFile)), true);

                            foreach (var dependency in dependencyFolders)
                            {
                                var availableDependencyMonikers = new List<string>();

                                if (Directory.Exists(Path.Combine(dependency, "lib")))
                                {
                                    var depLibraries = Directory.GetDirectories(Path.Combine(dependency, "lib"));
                                    var closestDepLibFolder = Helpers.GetBestLibMatch(targetFramework, depLibraries);
                                    var dFrameworkIsAvailable = !string.IsNullOrWhiteSpace(closestDepLibFolder);

                                    if (dFrameworkIsAvailable)
                                    {

                                        Directory.CreateDirectory(Path.Combine(outputPath, "dependencies",
                                            package.Moniker));

                                        var dependencyBinaries = Directory.GetFiles(closestDepLibFolder, "*.dll",
                                            SearchOption.TopDirectoryOnly);

                                        foreach (var binary in dependencyBinaries)
                                            File.Copy(binary,
                                                Path.Combine(outputPath, "dependencies", package.Moniker,
                                                    Path.GetFileName(binary)), true);

                                    }

                                }
                            }
                        }

                    }
                    else
                    {
                        // We could not find a closest folder, so let's just check in the root.
                        var binaries = Directory.GetFiles(pacManPackageLibPath,
                            "*.dll",
                            SearchOption.TopDirectoryOnly);
                        var docFiles = Directory.GetFiles(pacManPackageLibPath,
                            "*.xml",
                            SearchOption.TopDirectoryOnly);

                        // Make sure to only go through any processing if we found binaries.
                        if (binaries.Any())
                        {
                            Directory.CreateDirectory(packageContainerPath);


                            foreach (var binary in binaries)
                                File.Copy(binary, Path.Combine(packageContainerPath, Path.GetFileName(binary)), true);

                            foreach (var docFile in docFiles)
                                File.Copy(docFile, Path.Combine(packageContainerPath, Path.GetFileName(docFile)), true);

                            foreach (var dependency in dependencyFolders)
                            {
                                var availableDependencyMonikers = new List<string>();

                                if (Directory.Exists(Path.Combine(dependency, "lib")))
                                {
                                    var depLibraries = Directory.GetDirectories(Path.Combine(dependency, "lib"));
                                    var closestDepLibFolder = Helpers.GetBestLibMatch(targetFramework, depLibraries);
                                    var dFrameworkIsAvailable = !string.IsNullOrWhiteSpace(closestDepLibFolder);

                                    if (dFrameworkIsAvailable)
                                    {

                                        Directory.CreateDirectory(Path.Combine(outputPath, "dependencies",
                                            package.Moniker));

                                        var dependencyBinaries = Directory.GetFiles(closestDepLibFolder, "*.dll",
                                            SearchOption.TopDirectoryOnly);

                                        foreach (var binary in dependencyBinaries)
                                            File.Copy(binary,
                                                Path.Combine(outputPath, "dependencies", package.Moniker,
                                                    Path.GetFileName(binary)), true);

                                    }

                                }
                            }
                        }

                    }

                }

                try
                {
                    ConsoleEx.WriteLine($"Deleting {Path.Combine(outputPath, "_pacman")}", ConsoleColor.Red);
                    Helpers.DeleteDirectory(Path.Combine(outputPath, "_pacman"));
                }
                catch
                {
                    Helpers.DeleteDirectory(Path.Combine(outputPath, "_pacman"));
                }
            }

            return true;
        }
    }
}