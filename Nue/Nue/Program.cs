using System;
using System.Threading.Tasks;
using CommandLine;
using Nue.Core;
using Nue.Models;

namespace Nue
{
    internal class Program
    {
        private const string NugetFeedUrl = "https://packages.nuget.org/api/v2";

        private const string NewNugetSearchUrl =
            "https://api-v2v3search-0.nuget.org/query?q=owners:{0}&prerelease=false";

        private static void Main(string[] args)
        {
            var options = new CommandLineOptions();
            if (Parser.Default.ParseArguments(args, options))
                if (options.Mode == "extract")
                {
                    var frameworks = options.Framework.Split(',');

                    Task.Run(async () =>
                    {
                        var completed = await Extractor.DownloadPackages(options.PackagePath, options.OutputPath, frameworks,
                            NugetFeedUrl);

                        Console.Write("Completed successfully: " + completed);
                    }).Wait();
                }
                else if (options.Mode == "listpac")
                {
                    Lister.CreatePackageListing(options.Account, options.OutputPath, 3, NewNugetSearchUrl);
                }

            Console.ReadKey();
        }
    }
}