using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.VisualBasic.FileIO;
using Nue.Models;
using NuGet.Configuration;
using NuGet.PackageManagement;
using NuGet.Packaging.Core;
using NuGet.ProjectManagement;
using NuGet.Protocol;
using NuGet.Protocol.Core.Types;
using NuGet.Resolver;
using NuGet.Versioning;
using SearchOption = System.IO.SearchOption;
using System.Diagnostics;
using System.Text.RegularExpressions;

namespace Nue.Core
{
    public class Extractor
    {
        // Determines the best folder match for a libary based on the specified target moniker.
        private static string GetBestLibMatch(string tfm, string[] folderPaths)
        {
            var tfmRegex = new Regex(@"(?<Base>[a-zA-Z]*)(?<Version>[0-9\.0-9]*)");
            var match = tfmRegex.Match(tfm);

            var tfmBase = match.Groups["Base"].Value;
            var tfmVersion = match.Groups["Version"].Value;
            string folder = string.Empty;

            // Look for a folder that matches exactly the TFM.
            var exactMatch = (from c in folderPaths
                where Path.GetFileName(c).Equals(tfm, StringComparison.InvariantCultureIgnoreCase)
                select c).FirstOrDefault();

            // If we found one, we should just return it.
            if (exactMatch != null)
                return exactMatch;

            // As an example, if the TFM is net45, this should cover everything like:
            // net45, net451, net452
            var lenientMatch = new Regex($@"^(?<full>(?<base>{tfm})(?<version>[0-9\.0-9]*))$", RegexOptions.IgnoreCase);
            folder = GetWinningFolder(folderPaths, lenientMatch);

            if (!string.IsNullOrWhiteSpace(folder)) return folder;
            // Now we just match the base, e.g. for net we should get:
            // net45, net46, net461
            var baseMatch = new Regex($@"^(?<full>(?<base>{tfmBase})(?<version>[0-9\.0-9]*))$", RegexOptions.IgnoreCase);
            folder = GetWinningFolder(folderPaths, baseMatch);

            if (!string.IsNullOrWhiteSpace(folder)) return folder;
            // Now do an even more lenient match within 
            var preciseTfmRegex = new Regex($@"(?<full>(?<version>{tfmBase})(?<version>[0-9\.0-9]+))",RegexOptions.IgnoreCase);
            folder = GetWinningFolder(folderPaths, preciseTfmRegex);

            return folder;
        }

        private static string GetWinningFolder(string[] folders, Regex regex)
        {
            var folderAssociations = new Dictionary<string, double>();
            foreach (var folder in folders)
            {
                var exactFolderName = Path.GetFileName(folder);
                var token = regex.Match(exactFolderName);
                if (!token.Success) continue;
                var folderVersion = token.Groups["version"].Value;

                folderAssociations.Add(folder, double.Parse(folderVersion));
            }

            if (folderAssociations.Count <= 0) return string.Empty;
            var topItem = (from c in folderAssociations orderby c.Value descending select c).First();
            return topItem.Key;
        }

        public static async Task<bool> DownloadPackages(string packagePath, string outputPath, string targetFramework)
        {
            if (string.IsNullOrWhiteSpace(packagePath) || string.IsNullOrWhiteSpace(outputPath) ||
                string.IsNullOrWhiteSpace(targetFramework)) return false;

            var packages = GetPackagesFromFile(packagePath);

            var providers = new List<Lazy<INuGetResourceProvider>>();
            providers.AddRange(Repository.Provider.GetCoreV3()); // Add v3 API support

            var rootPath = outputPath + "\\_pacman";
            var settings = Settings.LoadDefaultSettings(rootPath, null, new MachineWideSettings());
            ISourceRepositoryProvider sourceRepositoryProvider = new SourceRepositoryProvider(settings, providers);

            var packageSource = new PackageSource("https://api.nuget.org/v3/index.json");

            NuGetProject project = new TargetedFolderNuGetProject(rootPath, targetFramework);

            var packageManager = new NuGetPackageManager(sourceRepositoryProvider, settings, rootPath)
            {
                PackagesFolderNuGetProject = (FolderNuGetProject)project
            };

            var resolutionContext = new ResolutionContext(DependencyBehavior.Highest, true, true, VersionConstraints.None);

            INuGetProjectContext projectContext = new ProjectContext();
            var sourceRepository = new SourceRepository(packageSource, providers);

            foreach (var package in packages)
            {
                ConsoleEx.WriteLine($"Attempting to install: {package.GetFullName()}. Installing...",
                    ConsoleColor.Yellow);
                var identity = new PackageIdentity(package.Name, NuGetVersion.Parse(package.Version));
                await packageManager.InstallPackageAsync(packageManager.PackagesFolderNuGetProject,
                    identity, resolutionContext, projectContext, sourceRepository,
                    null, // This is a list of secondary source respositories, probably empty
                    CancellationToken.None);
                ConsoleEx.WriteLine($"Getting data for {package.Name}...", ConsoleColor.Yellow);

                ConsoleEx.WriteLine("Downloaded package and dependencies.", ConsoleColor.Green);

                var packageFqn = package.Name + "." + package.Version;
                var pacManPackagePath = outputPath + "\\_pacman\\" + packageFqn;
                var pacManPackageLibPath = pacManPackagePath + "\\lib";
                var packageContainerPath = Path.Combine(outputPath, package.Moniker);

                if (Directory.Exists(pacManPackageLibPath))
                {
                    var directories = Directory.GetDirectories(pacManPackageLibPath);
                    var closestDirectory = GetBestLibMatch(targetFramework, directories);

                    var availableMonikers = new List<string>();
                    var dependencyFolders = from c in Directory.GetDirectories(outputPath + "\\_pacman")
                                            where Path.GetFileName(c).ToLower() != packageFqn.ToLower()
                                            select c;

                    Console.WriteLine("Currently available lib sets:");
                    ConsoleEx.WriteLine("|__", ConsoleColor.Yellow);
                    foreach (var folder in directories)
                    {
                        var tfmFolder = Path.GetFileName(folder);
                        availableMonikers.Add(tfmFolder);
                        ConsoleEx.WriteLine("   |___" + tfmFolder, ConsoleColor.Yellow);
                    }

                    Console.WriteLine("Package dependencies:");
                    ConsoleEx.WriteLine("|__", ConsoleColor.Yellow);
                    foreach (var dependency in dependencyFolders)
                        ConsoleEx.WriteLine("   |___" + Path.GetFileNameWithoutExtension(dependency),
                            ConsoleColor.Yellow);

                    var frameworkIsAvailable = !string.IsNullOrWhiteSpace(closestDirectory);

                    if (frameworkIsAvailable)
                    {
                        var binaries = Directory.GetFiles(closestDirectory,
                            "*.dll",
                            SearchOption.TopDirectoryOnly);
                        var docFiles = Directory.GetFiles(closestDirectory,
                            "*.xml",
                            SearchOption.TopDirectoryOnly);

                        // Make sure to only go through any processing if we found binaries.
                        if (binaries != null && binaries.Any())
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
                                    var dependencyLibFolders = Directory.GetDirectories(Path.Combine(dependency, "lib"));
                                    var closestDepLibFolder = GetBestLibMatch(targetFramework, dependencyLibFolders);

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
                        Console.WriteLine("Skipping the package because framework wasn't found.");
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

                        packages.Add(pAtom);
                    }
                }
            }

            return packages;
        }

        public static bool ExtractLocalPackages(string outputPath, string packageList, string nuGetPath, string targetFramework)
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
                cmdsi.Arguments = $"install {package.Name} -Version {package.Version} -Source {Path.GetDirectoryName(packageList)} -OutputDirectory {outputPath + "\\_pacman"} -Verbosity Detailed -DisableParallelProcessing -FallbackSource https://api.nuget.org/v3/index.json";

                Console.WriteLine($"Package source: {Path.GetDirectoryName(packageList)}");

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

                    var closestFolder = GetBestLibMatch(targetFramework, directories);

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
                        if (binaries != null && binaries.Any())
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
                                    var closestDepLibFolder = GetBestLibMatch(targetFramework, depLibraries);
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