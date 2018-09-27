using System;
using CommandLine;
using Nue.Core;
using Nue.Models;

namespace Nue
{
    internal class Program
    {
        private const string NewNugetSearchUrl =
            "https://api-v2v3search-0.nuget.org/query?q=owners:{0}&prerelease=false";

        private static void Main(string[] args)
        {
            Console.WriteLine("nue 2.0.0-9212018.1440");

            Parser.Default.ParseArguments<CommandLineOptions>(args).WithParsed(options =>
            {
                // Extracts the content from existing online NuGet packages.
                if (options.Mode == "extract")
                {
                    Console.WriteLine("Working on extracting...");
                    Console.WriteLine("Declared NuGet path: " + options.NuGetPath);

                    var completed = Extractor.DownloadPackages(options.PackagePath, options.OutputPath, options.Framework, new System.Collections.Generic.KeyValuePair<string, string>(options.Username, options.Password), options.Feed, options.NuGetPath);

                    Console.Write("Completed successfully: " + completed);
                }
                // Generate a list of packages.
                else if (options.Mode == "listpac")
                {
                    Lister.CreatePackageListing(options.Account, options.OutputPath, 3, NewNugetSearchUrl);
                }
                // Local extraction of NuGet packages from the disk.
                else if (options.Mode == "le")
                {
                    // PackagePath = Path to package list
                    var completed = Extractor.ExtractLocalPackages(options.OutputPath, options.PackagePath, options.NuGetPath, options.Framework, options.PackageSource);
                    Console.Write("Completed successfully: " + completed);
                }
            });
        }
    }
}
