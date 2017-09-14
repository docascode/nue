using Ionic.Zip;
using Nue.Core;
using NuGet.Configuration;
using NuGet.PackageManagement;
using NuGet.Packaging.Core;
using NuGet.ProjectManagement;
using NuGet.Protocol;
using NuGet.Protocol.Core.Types;
using NuGet.Resolver;
using NuGet.Versioning;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace Nue.StandardResolver
{
    public class Resolver : IPackageResolver
    {
        public IDictionary<string, string> Parameters { get; set; }

        public async Task<bool> CopyBinarySet(PackageAtom package, string outputPath)
        {
            Parameters = new Dictionary<string, string>(package.CustomPropertyBag);

            var providers = new List<Lazy<INuGetResourceProvider>>();
            providers.AddRange(Repository.Provider.GetCoreV3()); // Add v3 API support

            var rootPath = outputPath + "\\_pacman";
            var settings = Settings.LoadDefaultSettings(rootPath, null, new MachineWideSettings());
            ISourceRepositoryProvider sourceRepositoryProvider = new SourceRepositoryProvider(settings, providers);

            var packageSource = new PackageSource("https://api.nuget.org/v3/index.json");

            NuGetProject project = new TargetedFolderNuGetProject(rootPath, Parameters["tfm"]);

            var packageManager = new NuGetPackageManager(sourceRepositoryProvider, settings, rootPath)
            {
                PackagesFolderNuGetProject = (FolderNuGetProject)project
            };

            var resolutionContext = new ResolutionContext(DependencyBehavior.Lowest, true, true, VersionConstraints.None);

            INuGetProjectContext projectContext = new ProjectContext();
            var sourceRepository = new SourceRepository(packageSource, providers);

            // Check if we have a metapackage flag
            if (package.CustomPropertyBag.ContainsKey("metapackage"))
            {
                var metaFlag = Convert.ToBoolean(package.CustomPropertyBag["metapackage"]);
                package.IsMetaPackage = metaFlag;
            }

            ConsoleEx.WriteLine($"Attempting to install: {package.GetFullName()}. Installing...",
                    ConsoleColor.Yellow);
            var identity = new PackageIdentity(package.Name, NuGetVersion.Parse(package.Version));
            await packageManager.InstallPackageAsync(packageManager.PackagesFolderNuGetProject,
                identity, resolutionContext, projectContext, sourceRepository,
                null, // This is a list of secondary source respositories, probably empty
                CancellationToken.None);

            ConsoleEx.WriteLine($"Getting data for {package.Name}...", ConsoleColor.Yellow);

            var packageFqn = package.Name + "." + package.Version;
            var pacManPackagePath = outputPath + "\\_pacman\\" + packageFqn;
            var pacManPackageLibPath = pacManPackagePath + "\\lib";
            var packageContainerPath = Path.Combine(outputPath, package.Moniker);

            // Among other things, we need to make sure that the package was not already extracted for 
            // another team.
            if (Directory.Exists(pacManPackageLibPath) && !Directory.Exists(packageContainerPath))
            {

                // Directory exists, so we should proceed to package extraction.
                var directories = Directory.GetDirectories(pacManPackageLibPath);
                var closestDirectory = Helpers.GetBestLibMatch(Parameters["tfm"], directories);

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


                // We could not find a closest folder, so let's just check in the root.

                var binaries = Directory.EnumerateFiles(pacManPackageLibPath, "*.*", SearchOption.TopDirectoryOnly)
                                .Where(s => s.EndsWith(".dll") || s.EndsWith(".winmd"));
                var docFiles = Directory.GetFiles(pacManPackageLibPath,
                    "*.xml",
                    SearchOption.TopDirectoryOnly);

                Directory.CreateDirectory(packageContainerPath);

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
                }
            }
            return true;
        }
    }
}
