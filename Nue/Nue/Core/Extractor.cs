﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Microsoft.VisualBasic.FileIO;
using Nue.Models;
using NuGet;
using SearchOption = System.IO.SearchOption;
using NuGet.Packaging.Core;
using NuGet.Protocol.Core.Types;
using NuGet.Configuration;
using NuGet.Versioning;
using NuGet.Protocol;
using NuGet.ProjectManagement;
using NuGet.PackageManagement;
using NuGet.Resolver;
using System.Threading;
using System.Threading.Tasks;

namespace Nue.Core
{
    public class Extractor
    {
        public async static Task<bool> DownloadPackages(string packagePath, string outputPath, string[] frameworks, string feedUrl)
        {
            if (!string.IsNullOrWhiteSpace(packagePath) && !string.IsNullOrWhiteSpace(outputPath) &&
                frameworks.Length > 0 && !string.IsNullOrWhiteSpace(feedUrl))
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

                        if (requestedVersions > 2)
                            for (var i = 2; i < 2 + requestedVersions; i++)
                            {
                                var pAtom = new PackageAtom();
                                pAtom.Moniker = fields[0] + "-" + fields[i];
                                pAtom.Name = fields[1];
                                pAtom.Version = fields[i];

                                packages.Add(pAtom);
                            }
                    }
                }

                List<Lazy<INuGetResourceProvider>> providers = new List<Lazy<INuGetResourceProvider>>();
                providers.AddRange(Repository.Provider.GetCoreV3());  // Add v3 API support

                string rootPath = outputPath + "\\_pacman";
                ISettings settings = Settings.LoadDefaultSettings(rootPath, null, new MachineWideSettings());
                ISourceRepositoryProvider sourceRepositoryProvider = new SourceRepositoryProvider(settings, providers);

                PackageSource packageSource = new PackageSource("https://api.nuget.org/v3/index.json");

                NuGetProject project = new FolderNuGetProject(rootPath);
                NuGetPackageManager packageManager = new NuGetPackageManager(sourceRepositoryProvider, settings, rootPath)
                {
                    PackagesFolderNuGetProject = (FolderNuGetProject)project
                };

                bool allowPrereleaseVersions = false;
                bool allowUnlisted = false;
                ResolutionContext resolutionContext = new ResolutionContext(DependencyBehavior.Lowest, allowPrereleaseVersions, allowUnlisted, VersionConstraints.None);
                INuGetProjectContext projectContext = new ProjectContext();
                SourceRepository sourceRepository = new SourceRepository(packageSource, providers);

                foreach (var package in packages)
                {
                    ConsoleEx.WriteLine($"Attempting to install: {package.GetFullName()}. Installing...", ConsoleColor.Yellow);
                    PackageIdentity identity = new PackageIdentity(package.Name,NuGetVersion.Parse(package.Version));
                    await packageManager.InstallPackageAsync(packageManager.PackagesFolderNuGetProject,
                        identity, resolutionContext, projectContext, sourceRepository,
                        null,  // This is a list of secondary source respositories, probably empty
                        CancellationToken.None);
                    ConsoleEx.WriteLine($"Getting data for {package.Name}...", ConsoleColor.Yellow);

                    //if (package.GetFullName().Equals("Microsoft.Azure.Mobile.Server 1.0.119", StringComparison.CurrentCultureIgnoreCase))
                    //    throw new Exception();

                    ConsoleEx.WriteLine("Downloaded package and dependencies.",ConsoleColor.Green);

                    var packageFqn = package.Name + "." + package.Version;
                    var pacManPackagePath = outputPath + "\\_pacman_" + packageFqn;
                    var pacManPackageLibPath = pacManPackagePath + "\\lib";
                    var finalPath = Path.Combine(outputPath, package.Moniker);

                    if (Directory.Exists(pacManPackageLibPath))
                    {
                        var directories = Directory.GetDirectories(pacManPackageLibPath);
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
                        {
                            ConsoleEx.WriteLine("   |___" + Path.GetFileNameWithoutExtension(dependency), ConsoleColor.Yellow);
                        }

                        foreach (var framework in frameworks)
                        {
                            var libMoniker = (from c in availableMonikers where c.ToLowerInvariant().Contains(framework.ToLowerInvariant()) select c).FirstOrDefault();
                            bool frameworkIsAvailable = (libMoniker != null);

                            ConsoleEx.WriteLine($"Target framework found in package: {frameworkIsAvailable}", ConsoleColor.Yellow);

                            if (frameworkIsAvailable)
                            {
                                var binaries = Directory.GetFiles(Path.Combine(pacManPackageLibPath, libMoniker), "*.dll",
                                    SearchOption.TopDirectoryOnly);
                                var docFiles = Directory.GetFiles(Path.Combine(pacManPackageLibPath, libMoniker), "*.xml",
                                    SearchOption.TopDirectoryOnly);

                                // Make sure to only go through any processing if we found binaries.
                                if (binaries != null && binaries.Count() > 0)
                                {
                                    Directory.CreateDirectory(finalPath);



                                    foreach (var binary in binaries)
                                        File.Copy(binary, Path.Combine(finalPath, Path.GetFileName(binary)), true);

                                    foreach (var docFile in docFiles)
                                        File.Copy(docFile, Path.Combine(finalPath, Path.GetFileName(docFile)), true);

                                    foreach (var dependency in dependencyFolders)
                                    {
                                        Directory.CreateDirectory(Path.Combine(outputPath, "dependencies", package.Moniker));

                                        string libFolder = string.Empty;
                                        string[] dependencyBinaries;
                                        if (Directory.Exists(libFolder))
                                        {
                                            libFolder = Path.Combine(dependency, "lib", framework);
                                        }
                                        else
                                        {
                                            var dependencyLib = Path.Combine(dependency, "lib");
                                            if (Directory.Exists(dependencyLib))
                                            {
                                                var frameworkDirectory = from c in Directory.GetDirectories(Path.Combine(dependency, "lib")) where c.Contains(dependency) select c;
                                                libFolder = frameworkDirectory.FirstOrDefault();
                                            }
                                        }

                                        if (!string.IsNullOrWhiteSpace(libFolder))
                                        {
                                            dependencyBinaries = Directory.GetFiles(libFolder, "*.dll",
                                                    SearchOption.TopDirectoryOnly);

                                            foreach (var binary in dependencyBinaries)
                                                File.Copy(binary, Path.Combine(outputPath, "dependencies", package.Moniker, Path.GetFileName(binary)), true);
                                        }
                                    }
                                }
                            }
                            else
                            {
                                Console.WriteLine("Skipping the package because framework wasn't found.");
                            }
                        }
                    }

                    try
                    {
                        ConsoleEx.WriteLine($"Deleting {Path.Combine(outputPath, "_pacman")}", ConsoleColor.Red);
                        Helpers.DeleteDirectory(pacManPackagePath);
                    }
                    catch
                    {
                        Helpers.DeleteDirectory(Path.Combine(outputPath, "_pacman"));
                    }
                }

                return true;
            }
            return false;
        }
    }
}