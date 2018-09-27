using Nue.Core;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;

namespace Nue.StandardResolver
{
    public class Resolver : IPackageResolver
    {
        public const string NUGET_DEFAULT_FEED = "https://api.nuget.org/v3/index.json";
        public IDictionary<string, string> Parameters { get; set; }

        public bool CopyBinarySet(PackageAtom package, string outputPath, KeyValuePair<string, string> credentials = new KeyValuePair<string, string>(), string feed = "", string nugetPath = "")
        {
            string defaultPackageSource = NUGET_DEFAULT_FEED;
            if (!string.IsNullOrWhiteSpace(feed))
            {
                defaultPackageSource = feed;
            }

            // Check if we have a requirement for a custom package source
            if (package.CustomPropertyBag.ContainsKey("customSource"))
            {
                defaultPackageSource = package.CustomPropertyBag["customSource"];
            }

            Parameters = new Dictionary<string, string>(package.CustomPropertyBag);

            var rootPath = outputPath + "\\_pacman";

            ConsoleEx.WriteLine($"Attempting to install: {package.GetFullName()}. Installing...",
                    ConsoleColor.Yellow);

            String command = $"{nugetPath}\\nuget.exe";

            ConsoleEx.WriteLine($"Assumed nuget.exe path: {command}", ConsoleColor.Yellow);

            ProcessStartInfo cmdsi = new ProcessStartInfo(command);
            cmdsi.UseShellExecute = false;

            var configPath = Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), "custom.nuget.config");
            cmdsi.Arguments = $"install {package.Name} -Version {package.Version} -Source {defaultPackageSource} -OutputDirectory {rootPath} -Verbosity Detailed -DisableParallelProcessing -FallbackSource https://api.nuget.org/v3/index.json -ConfigFile {configPath} -PreRelease";

            Process cmd = Process.Start(cmdsi);
            cmd.WaitForExit();

            ConsoleEx.WriteLine($"Getting data for {package.Name}...", ConsoleColor.Yellow);

            var workingFolder = Path.Combine(outputPath, "_pacman");

            var packageFqn = package.Name + "." + package.Version;
            var pacManPackagePath = Path.Combine(workingFolder, packageFqn);

            string pacManPackageLibPath = "";

            pacManPackagePath = (from c in Directory.GetDirectories(workingFolder) where c.StartsWith(pacManPackagePath, StringComparison.InvariantCultureIgnoreCase) select c).FirstOrDefault();

            // In some cases, the lookup might be happening inside a custom path.
            // For PowerShell, this should be done inside the root directory.
            if (package.IsPowerShellPackage)
            {
                pacManPackageLibPath = pacManPackagePath;
            }
            else if (package.CustomPropertyBag.ContainsKey("libpath") && !string.IsNullOrWhiteSpace(package.CustomPropertyBag["libpath"]))
            {
                pacManPackageLibPath = Path.Combine(pacManPackagePath,Convert.ToString(package.CustomPropertyBag["libpath"]));
            }
            else
            {
                pacManPackageLibPath = pacManPackagePath + "\\lib";
            }

            var packageContainerPath = Path.Combine(outputPath, package.Moniker);


            // Among other things, we need to make sure that the package was not already extracted for 
            // another team.
            if (Directory.Exists(pacManPackageLibPath) && !Directory.Exists(packageContainerPath))
            {
                Directory.CreateDirectory(packageContainerPath);

                // If we are dealing with a different PowerShell package, we might need to operate slightly
                // differently givent that the structure is not at all reflective of what other NuGet packages encompass.
                if (package.IsPowerShellPackage)
                {
                    ConsoleEx.WriteLine("Operating on a PowerShell package...", ConsoleColor.Blue);

                    var helpXmlFiles = from c in Directory.GetFiles(pacManPackageLibPath)
                                       where Path.GetFileName(c).ToLower().EndsWith("-help.xml")
                                       select c;

                    var dllFiles = new List<string>();

                    Console.WriteLine("Here are the XML files that we will work with:");
                    foreach (var helpXmlFile in helpXmlFiles)
                    {
                        ConsoleEx.WriteLine(helpXmlFile, ConsoleColor.Blue);
                        var workingDll = Path.GetFileName(helpXmlFile).ToLower().Replace("-help.xml", "");
                        if (File.Exists(Path.Combine(pacManPackageLibPath, workingDll)))
                        {
                            ConsoleEx.Write("Found matching DLL: ");
                            ConsoleEx.Write(workingDll + "\n", ConsoleColor.Blue);

                            dllFiles.Add(workingDll);
                        }
                    }

                    if (dllFiles.Any())
                    {
                        foreach (var dll in dllFiles)
                        {
                            File.Copy(Path.Combine(pacManPackageLibPath, dll), Path.Combine(packageContainerPath, dll), true);
                            //File.Copy(Path.Combine(pacManPackageLibPath, dll + "-help.xml"), Path.Combine(packageContainerPath, Path.GetFileNameWithoutExtension(dll) + ".xml"), true);
                        }

                        var dependencies = from c in Directory.GetFiles(pacManPackageLibPath)
                                           where !dllFiles.Contains(Path.GetFileName(c).ToLower()) && Path.GetFileName(c).EndsWith(".dll")
                                           select c;

                        if (dependencies.Any())
                        {
                            Directory.CreateDirectory(Path.Combine(outputPath, "dependencies", package.Moniker));

                            foreach (var dependency in dependencies)
                            {

                                File.Copy(dependency, Path.Combine(outputPath, "dependencies", package.Moniker, Path.GetFileName(dependency)), true);
                            }
                        }
                    }
                }
                else
                {
                    // Directory exists, so we should proceed to package extraction.
                    var directories = Directory.GetDirectories(pacManPackageLibPath);
                    var closestDirectory = Helpers.GetBestLibMatch(Parameters["tfm"], directories);

                    var availableMonikers = new List<string>();
                    var dependencyFolders = new List<string>();

                    try
                    {
                        dependencyFolders = (from c in Directory.GetDirectories(outputPath + "\\_pacman")
                                             where Path.GetFileName(c).ToLower() != packageFqn.ToLower()
                                             select c).ToList();
                    }
                    catch
                    {
                        Console.WriteLine("Could not create list of dependencies.");
                    }

                    // It might be possible that the author specified an additional dependency folder.
                    // If that is the case, we are just going to add it to the existing set of folders.
                    if (package.CustomPropertyBag.ContainsKey("customDependencyFolder"))
                    {
                        dependencyFolders.Add(Path.Combine(pacManPackagePath, package.CustomPropertyBag["customDependencyFolder"]));
                    }

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


                    // We could not find a closest folder, so let's just check in the root.

                    var binaries = Directory.EnumerateFiles(pacManPackageLibPath, "*.*", SearchOption.TopDirectoryOnly)
                                    .Where(s => s.EndsWith(".dll") || s.EndsWith(".winmd"));
                    var docFiles = Directory.GetFiles(pacManPackageLibPath,
                        "*.xml",
                        SearchOption.TopDirectoryOnly);

                    // Make sure to only go through any processing if we found binaries.
                    if (binaries.Any())
                    {
                        foreach (var binary in binaries)
                            File.Copy(binary, Path.Combine(packageContainerPath, Path.GetFileName(binary)), true);

                        foreach (var docFile in docFiles)
                            File.Copy(docFile, Path.Combine(packageContainerPath, Path.GetFileName(docFile)), true);
                    }

                    if (frameworkIsAvailable)
                    {
                        binaries = Directory.EnumerateFiles(closestDirectory, "*.*", SearchOption.TopDirectoryOnly)
                                        .Where(s => s.EndsWith(".dll") || s.EndsWith(".winmd"));
                        docFiles = Directory.GetFiles(closestDirectory,
                            "*.xml",
                            SearchOption.TopDirectoryOnly);

                        // Make sure to only go through any processing if we found binaries.
                        if (binaries.Any())
                        {
                            foreach (var binary in binaries)
                                File.Copy(binary, Path.Combine(packageContainerPath, Path.GetFileName(binary)), true);

                            foreach (var docFile in docFiles)
                                File.Copy(docFile, Path.Combine(packageContainerPath, Path.GetFileName(docFile)), true);
                        }
                    }

                    foreach (var dependency in dependencyFolders)
                    {
                        var availableDependencyMonikers = new List<string>();

                        var targetPath = Path.Combine(dependency, "lib");
                        if (Directory.Exists(targetPath) && Directory.EnumerateFiles(targetPath, "*.*", SearchOption.AllDirectories)
                                .Where(s => s.EndsWith(".dll") || s.EndsWith(".winmd")).Count() > 0)
                        {
                            List<string> alternateDependencies = new List<string>();
                            // In some cases, we might want to have alterhative dependency monikers.
                            if (package.CustomPropertyBag.ContainsKey("altDep"))
                            {
                                alternateDependencies = new List<string>(package.CustomPropertyBag["altDep"].Split('|'));
                            }

                            var dependencyLibFolders = Directory.GetDirectories(Path.Combine(dependency, "lib"));
                            var closestDepLibFolder = Helpers.GetBestLibMatch(Parameters["tfm"], dependencyLibFolders);

                            if (string.IsNullOrWhiteSpace(closestDepLibFolder))
                            {
                                // We could not find a regular TFM dependency, let's try again for alternates.
                                if (alternateDependencies.Count > 0)
                                {
                                    foreach (var altDependency in alternateDependencies)
                                    {
                                        closestDepLibFolder = Helpers.GetBestLibMatch(altDependency, dependencyLibFolders);
                                        if (!string.IsNullOrWhiteSpace(closestDepLibFolder))
                                            break;
                                    }
                                }
                            }

                            var dFrameworkIsAvailable = !string.IsNullOrWhiteSpace(closestDepLibFolder);

                            if (dFrameworkIsAvailable)
                            {
                                Directory.CreateDirectory(Path.Combine(outputPath, "dependencies",
                                    package.Moniker));

                                var dependencyBinaries = Directory.EnumerateFiles(closestDepLibFolder, "*.*", SearchOption.TopDirectoryOnly)
                                .Where(s => s.EndsWith(".dll") || s.EndsWith(".winmd"));

                                foreach (var binary in dependencyBinaries)
                                    File.Copy(binary,
                                        Path.Combine(outputPath, "dependencies", package.Moniker,
                                            Path.GetFileName(binary)), true);

                            }
                        }
                        else
                        {
                            // The "lib" folder does not exist, so let's just look in the root.
                            var dependencyBinaries = Directory.EnumerateFiles(dependency, "*.*", SearchOption.TopDirectoryOnly)
                                .Where(s => s.EndsWith(".dll") || s.EndsWith(".winmd"));

                            foreach (var binary in dependencyBinaries)
                                File.Copy(binary,
                                    Path.Combine(outputPath, "dependencies", package.Moniker,
                                        Path.GetFileName(binary)), true);
                        }
                    }
                }
            }
            return true;
        }
    }
}
