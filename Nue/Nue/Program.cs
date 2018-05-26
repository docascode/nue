using System;
using System.Threading.Tasks;
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
            Parser.Default.ParseArguments<CommandLineOptions>(args).WithParsed(options =>
            {
                // Extracts the content from existing online NuGet packages.
                if (options.Mode == "extract")
                {
                    Task.Run(async () =>
                    {
                        var completed = await Extractor.DownloadPackages(options.PackagePath, options.OutputPath, options.Framework, new System.Collections.Generic.KeyValuePair<string, string>(options.Username,options.Password), options.Feed);

                        Console.Write("Completed successfully: " + completed);
                    }).Wait();
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
