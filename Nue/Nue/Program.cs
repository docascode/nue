using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.VisualBasic.FileIO;
using Nue.Models;
using NuGet;
using SearchOption = System.IO.SearchOption;

namespace Nue
{
    class Program
    {
        static void Main(string[] args)
        {
            if (args.Length == 3)
            {
                var csvPath = args[0];
                var outputPath = args[1];
                var folderMapper = args[2];

                List<PackageAtom> packages = new List<PackageAtom>();
                List<TfmAtom> tfms = new List<TfmAtom>();

                using (TextFieldParser parser = new TextFieldParser(folderMapper))
                {
                    parser.TextFieldType = FieldType.Delimited;
                    parser.SetDelimiters(",");

                    while (!parser.EndOfData)
                    {
                        string[] fields = parser.ReadFields();
                        TfmAtom tfm = new TfmAtom();

                        if (fields == null) continue;

                        tfm.Moniker = fields[0];
                        tfm.Target = fields[1];

                        tfms.Add(tfm);

                        Directory.CreateDirectory(outputPath + "\\" + tfm.Target);
                    }
                }

                using (TextFieldParser parser = new TextFieldParser(csvPath))
                {
                    parser.TextFieldType = FieldType.Delimited;
                    parser.SetDelimiters(",");

                    while (!parser.EndOfData)
                    {
                        string[] fields = parser.ReadFields();
                        PackageAtom pAtom = new PackageAtom();

                        if (fields == null) continue;

                        pAtom.Name = fields[0];
                        pAtom.Version = fields[1];

                        packages.Add(pAtom);
                    }
                }

                IPackageRepository repo = PackageRepositoryFactory.Default.CreateRepository("https://packages.nuget.org/api/v2");
                PackageManager pacMan = new PackageManager(repo, outputPath + "\\_pacman");

                foreach (var package in packages)
                {
                    Console.WriteLine($"Getting data for {package.Name}...");

                    List<IPackage> repoPackages = repo.FindPackagesById(package.Name).ToList();
                    var desiredPackage =
                        repoPackages.FirstOrDefault(x => x.Version.ToString() == package.Version);
                    Console.WriteLine($"Found the following package: {desiredPackage.GetFullName()}. Installing...");
                    pacMan.InstallPackage(desiredPackage, true, true);
                    Console.WriteLine("Package installed!");

                    var directories =
                        Directory.GetDirectories(outputPath + "\\_pacman\\" + desiredPackage.Id + "." +
                                                 desiredPackage.Version + "\\lib");
                    Console.WriteLine("Currently available lib sets:");

                    foreach (var folder in directories)
                    {
                        var tfmFolder = Path.GetFileName(folder);
                        Console.WriteLine(tfmFolder);

                        var mappedFolder = tfms.FirstOrDefault(x => x.Moniker.Equals(tfmFolder));

                        if (mappedFolder != null)
                        {
                            var targetFolder = outputPath + "\\" + mappedFolder.Target;
                            if (Directory.Exists(targetFolder))
                            {
                                var binaries = Directory.GetFiles(folder, "*.dll", SearchOption.TopDirectoryOnly);
                                foreach (var binary in binaries)
                                {
                                    File.Copy(binary,targetFolder + "\\" + Path.GetFileName(binary));
                                }
                            }
                        }
                    } 
                }
            }
            else
            {
                Console.WriteLine("You need to specify both the path to the source CSV and output folder.");
            }

            Console.ReadKey();
        }
    }
}
