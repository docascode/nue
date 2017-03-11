using System;
using Nue.Models;
using Nue.Core;

namespace Nue
{
    class Program
    {
        private const string NugetFeedUrl = "https://packages.nuget.org/api/v2";
        private const string NewNugetSearchUrl = "https://api-v2v3search-0.nuget.org/query?q=owners:{0}&prerelease=false";

        private static void Main(string[] args)
        {
            var options = new CommandLineOptions();
            if (CommandLine.Parser.Default.ParseArguments(args, options))
            {
                if (options.Mode == "extract")
                {
                    var frameworks = options.Framework.Split(',');

                    Extractor.DownloadPackages(options.PackagePath, options.OutputPath, frameworks, NugetFeedUrl);
                }
                else if (options.Mode == "listpac")
                {
                    Lister.CreatePackageListing(options.Account, options.OutputPath, 3, NewNugetSearchUrl);
                }
            }

            Console.WriteLine("Completed!");

            Console.ReadKey();
        }
    }
}
