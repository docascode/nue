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
        public bool CopyBinarySet(
            PackageAtom package,
            RunSettings runSettings,
            PackageInfomarionMapping pkgInfoMap,
            string outputPrefix = "")
        {
            var tfm = package.CustomProperties.TFM ?? runSettings.TFM;
            var rootPath = runSettings.OutputPath + "\\_pacman" + outputPrefix;

            Console.WriteLine($"[info] Attempting to install: {package.GetFullName()}...");

            string command = $"{runSettings.NugetPath}\\nuget.exe";

            ProcessStartInfo cmdsi = new ProcessStartInfo(command)
            {
                UseShellExecute = false
            };

            var configPath = Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), "custom.nuget.config");

            string commandString = Helpers.BuildCommandString(package, rootPath, configPath, runSettings);
            cmdsi.Arguments = commandString;
            Console.WriteLine($"[info] {command} {commandString}");

            Process cmd = Process.Start(cmdsi);
            cmd.WaitForExit();

            if (cmd.ExitCode != 0)
            {
                Console.WriteLine("[error] There was an error in NuGet installation. Package attempted: " + package.Name);
                return false;
            }
            else
            {
                var packageFqn = package.Name;
                if (package.CustomVersionDefined)
                {
                    packageFqn += "." + package.CustomVersion;
                }

                var pacManPackagePath = Path.Combine(rootPath, packageFqn);

                string pacManPackageLibPath = "";

                pacManPackagePath = (from c in Directory.GetDirectories(rootPath) where c.StartsWith(pacManPackagePath, StringComparison.InvariantCultureIgnoreCase) select c).FirstOrDefault();

                var packageVersion = pacManPackagePath.Replace(Path.Combine(rootPath, package.Name + "."), "");

                // In some cases, the lookup might be happening inside a custom path.
                // For PowerShell, this should be done inside the root directory.
                if (package.IsPowerShellPackage)
                {
                    pacManPackageLibPath = pacManPackagePath;
                }
                else if (!string.IsNullOrWhiteSpace(package.CustomProperties?.CustomLibraryFolder))
                {
                    pacManPackageLibPath = Path.Combine(pacManPackagePath, package.CustomProperties.CustomLibraryFolder);
                }
                else
                {
                    pacManPackageLibPath = pacManPackagePath + "\\lib";
                }

                var packageFolderId = string.IsNullOrEmpty(package.Moniker) ? package.Name : package.Moniker;
                var packageContainerPath = Path.Combine(runSettings.OutputPath, packageFolderId);
                var packageDependencyContainerPath = Path.Combine(runSettings.OutputPath, "dependencies", packageFolderId);

                // Among other things, we need to make sure that the package was not already extracted for 
                // another team.
                if (Directory.Exists(pacManPackageLibPath) && !Directory.Exists(packageContainerPath))
                {
                    Directory.CreateDirectory(packageContainerPath);

                    // If we are dealing with a different PowerShell package, we might need to operate slightly
                    // differently givent that the structure is not at all reflective of what other NuGet packages encompass.
                    if (package.IsPowerShellPackage)
                    {
                        Console.WriteLine($"[info] Treating {package.Name} as a PowerShell package.");

                        var helpXmlFiles = from c in Directory.GetFiles(pacManPackageLibPath)
                                           where Path.GetFileName(c).ToLower().EndsWith("-help.xml")
                                           select c;

                        var dllFiles = new List<string>();

                        foreach (var helpXmlFile in helpXmlFiles)
                        {
                            var workingDll = Path.GetFileName(helpXmlFile).ToLower().Replace("-help.xml", "");
                            if (File.Exists(Path.Combine(pacManPackageLibPath, workingDll)))
                            {
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
                                Directory.CreateDirectory(packageDependencyContainerPath);

                                foreach (var dependency in dependencies)
                                {

                                    File.Copy(dependency, Path.Combine(packageDependencyContainerPath, Path.GetFileName(dependency)), true);
                                }
                            }
                        }
                    }
                    else
                    {
                        var dependencyFolders = new List<string>();

                        // Directory exists, so we should proceed to package extraction.
                        var directories = Directory.GetDirectories(pacManPackageLibPath);
                        var closestDirectory = Helpers.GetBestLibMatch(tfm, directories);

                        try
                        {
                            dependencyFolders = (from c in Directory.GetDirectories(rootPath)
                                                 where Path.GetFileName(c).ToLower() != packageFqn.ToLower()
                                                 select c).ToList();
                        }
                        catch
                        {
                            Console.WriteLine($"[warning] Could not create list of dependencies for {package.Name}");
                        }

                        // It might be possible that the author specified an additional dependency folder.
                        // If that is the case, we are just going to add it to the existing set of folders.
                        if (!string.IsNullOrEmpty(package.CustomProperties.CustomDependencyFolder))
                        {
                            dependencyFolders.Add(Path.Combine(pacManPackagePath, package.CustomProperties.CustomDependencyFolder));
                        }

                        string informationalPackageString = $"[info] Currently available library sets for {package.Name}\n";

                        foreach (var folder in directories)
                        {
                            var tfmFolder = Path.GetFileName(folder);
                            informationalPackageString += "   |___" + tfmFolder + "\n";
                        }

                        Console.WriteLine(informationalPackageString);

                        if (dependencyFolders.Any())
                        {
                            informationalPackageString = $"[info] Package dependencies for {package.Name}\n";

                            foreach (var dependency in dependencyFolders)
                            {
                                informationalPackageString += "   |___" + Path.GetFileNameWithoutExtension(dependency) + "\n";
                            }

                            Console.WriteLine(informationalPackageString);
                        }

                        var frameworkIsAvailable = !string.IsNullOrWhiteSpace(closestDirectory);

                        bool capturedContent = false;
                        List<string> binaries = null;
                        if (frameworkIsAvailable)
                        {
                            capturedContent = Helpers.CopyLibraryContent(closestDirectory, packageContainerPath, package, out binaries);
                        }
                        else
                        {
                            capturedContent = Helpers.CopyLibraryContent(pacManPackageLibPath, packageContainerPath, package, out binaries);
                        }

                        // record the assembly => package mapping
                        var packageInfo = new PackageInfomarion()
                        {
                            Name = package.Name,
                            Version = packageVersion,
                            Feed = runSettings.Feed
                        };
                        if (!pkgInfoMap.ContainsKey(packageFolderId))
                        {
                            pkgInfoMap[packageFolderId] = new Dictionary<string, PackageInfomarion>();
                        }
                        foreach (var binary in binaries)
                        {
                            var assemblyName = Path.GetFileNameWithoutExtension(binary);

                            foreach (var item in pkgInfoMap)
                            {
                                if (item.Value.ContainsKey(assemblyName))
                                {
                                    var package_temp = pkgInfoMap[packageFolderId][assemblyName];
                                    Console.WriteLine($"[warning] {assemblyName} already exists in the {package_temp.Version} version of named {package_temp.Name} package");
                                }
                            }

                            pkgInfoMap[packageFolderId][assemblyName] = packageInfo;

                        }

                        // Only process dependencies if we actually captured binary content.
                        if (capturedContent)
                        {
                            if (dependencyFolders.Any())
                            {
                                foreach (var dependency in dependencyFolders)
                                {
                                    var availableDependencyMonikers = new List<string>();

                                    var targetPath = Path.Combine(dependency, "lib");
                                    if (Directory.Exists(targetPath) && Directory.GetFiles(targetPath, "*.*", SearchOption.AllDirectories)
                                            .Where(s => s.EndsWith(".dll") || s.EndsWith(".winmd")).Count() > 0)
                                    {
                                        List<string> alternateDependencies = new List<string>();

                                        // In some cases, we might want to have alterhative dependency monikers.
                                        if (package.CustomPropertyBag.ContainsKey("altDep"))
                                        {
                                            alternateDependencies = new List<string>(package.CustomPropertyBag["altDep"].Split('|'));
                                        }

                                        var dependencyLibFolders = Directory.GetDirectories(Path.Combine(dependency, "lib"));
                                        var closestDepLibFolder = Helpers.GetBestLibMatch(tfm, dependencyLibFolders);

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
                                            Directory.CreateDirectory(packageDependencyContainerPath);

                                            var dependencyBinaries = Directory.EnumerateFiles(closestDepLibFolder, "*.*", SearchOption.TopDirectoryOnly)
                                            .Where(s => s.EndsWith(".dll") || s.EndsWith(".winmd"));

                                            foreach (var binary in dependencyBinaries)
                                                File.Copy(binary,
                                                    Path.Combine(packageDependencyContainerPath, Path.GetFileName(binary)),
                                                    true);
                                        }
                                    }
                                    else
                                    {
                                        // The "lib" folder does not exist, so let's just look in the root.
                                        var dependencyBinaries = Directory.EnumerateFiles(dependency, "*.*", SearchOption.TopDirectoryOnly)
                                            .Where(s => s.EndsWith(".dll") || s.EndsWith(".winmd"));

                                        foreach (var binary in dependencyBinaries)
                                            File.Copy(binary,
                                                Path.Combine(packageDependencyContainerPath, Path.GetFileName(binary)),
                                                true);
                                    }
                                }
                            }
                            else
                            {
                                Console.WriteLine($"[warning] No dependencies captured for {package.Name}");
                            }
                        }
                        else
                        {
                            Console.WriteLine($"[error] No binaries captured for {package.Name}");
                            return false;
                        }
                    }
                }
                return true;
            }
        }
    }
}
