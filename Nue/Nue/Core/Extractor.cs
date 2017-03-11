using Microsoft.VisualBasic.FileIO;
using Nue.Models;
using NuGet;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Nue.Core
{
    public class Extractor
    {
        public static bool DownloadPackages(string packagePath, string outputPath, string[] frameworks, string feedUrl)
        {
            if (!string.IsNullOrWhiteSpace(packagePath) && !string.IsNullOrWhiteSpace(outputPath) &&
                frameworks.Length > 0 && !string.IsNullOrWhiteSpace(feedUrl))
            {
                List<PackageAtom> packages = new List<PackageAtom>();

                using (TextFieldParser parser = new TextFieldParser(packagePath))
                {
                    parser.TextFieldType = FieldType.Delimited;
                    parser.SetDelimiters(",");

                    while (!parser.EndOfData)
                    {
                        string[] fields = parser.ReadFields();

                        if (fields == null) continue;

                        // Given the conventions, let's find out how many versions are requested to be downloaded.
                        int requestedVersions = fields.Length - 2;

                        if (requestedVersions > 2)
                        {
                            for (int i = 2; i < 2 + requestedVersions; i++)
                            {
                                PackageAtom pAtom = new PackageAtom();
                                pAtom.Moniker = fields[0] + "-" + fields[i];
                                pAtom.Name = fields[1];
                                pAtom.Version = fields[i];

                                packages.Add(pAtom);
                            }
                        }
                    }
                }

                IPackageRepository repo = PackageRepositoryFactory.Default.CreateRepository(feedUrl);
                PackageManager pacMan = new PackageManager(repo, outputPath + "\\_pacman");

                foreach (var package in packages)
                {
                    Console.WriteLine($"Getting data for {package.Name}...");

                    List<IPackage> repoPackages = repo.FindPackagesById(package.Name).ToList();
                    var desiredPackage =
                        repoPackages.FirstOrDefault(x => x.Version.ToFullString() == package.Version);
                    Console.WriteLine($"Found the following package: {desiredPackage.GetFullName()}. Installing...");
                    pacMan.InstallPackage(desiredPackage, true, true);
                    Console.WriteLine("Package installed!");

                    var pacManPackagePath = outputPath + "\\_pacman\\" + desiredPackage.Id + "." + desiredPackage.Version;
                    var pacManPackageLibPath = pacManPackagePath + "\\lib";

                    if (Directory.Exists(pacManPackageLibPath))
                    {
                        var directories = Directory.GetDirectories(pacManPackageLibPath);

                        Console.WriteLine("Currently available lib sets:");

                        List<string> availableMonikers = new List<string>();

                        // Print available monikers from the downloaded package.
                        foreach (var folder in directories)
                        {
                            var tfmFolder = Path.GetFileName(folder);
                            availableMonikers.Add(tfmFolder);
                            Console.WriteLine(tfmFolder);
                        }

                        foreach (var framework in frameworks)
                        {
                            bool frameworkIsAvailable = availableMonikers.Contains(framework);
                            Console.WriteLine($"Found target in package: {frameworkIsAvailable}");

                            if (frameworkIsAvailable)
                            {
                                var finalPath = Path.Combine(outputPath, package.Moniker);

                                Directory.CreateDirectory(finalPath);

                                var binaries = Directory.GetFiles(Path.Combine(pacManPackageLibPath, framework), "*.dll", System.IO.SearchOption.TopDirectoryOnly);
                                foreach (var binary in binaries)
                                {
                                    File.Copy(binary, Path.Combine(finalPath, Path.GetFileName(binary)));
                                }
                            }
                            else
                            {
                                Console.WriteLine("Skipping the package because framework wasn't found.");
                            }
                        }
                    }
                }

                return true;
            }
            else
            {
                return false;
            }
        }
    }
}
