using System;
using Nue.Models;
using Nue.Core;

namespace Nue
{
    class Program
    {
        const string NUGET_FEED_URL = "https://packages.nuget.org/api/v2";
        static string NEW_NUGET_SEARCH_URL = "https://api-v2v3search-0.nuget.org/query?q=owners:{0}&prerelease=false";

        static void Main(string[] args)
        {
            var options = new CommandLineOptions();
            if (CommandLine.Parser.Default.ParseArguments(args, options))
            {
                switch(options.Mode)
                {
                    case "extract":
                        {
                            string[] frameworks = options.Framework.Split(new char[] { ',' });

                            Extractor.DownloadPackages(options.PackagePath, options.OutputPath, frameworks, NUGET_FEED_URL);
                            break;
                        }
                    case "listpac":
                        {
                            Lister.CreatePackageListing(options.Account, options.OutputPath, 3, NEW_NUGET_SEARCH_URL);
                            break;
                        }
                }
            }

            Console.ReadKey();
        }
    }
}
