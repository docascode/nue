using System;
using CommandLine;
using Nue.Core;
using Nue.Models;

namespace Nue
{
    internal class Program
    {
        private static void Main(string[] args)
        {
            Console.WriteLine("nue 2.0.0-9272018.1318");

            Parser.Default.ParseArguments<CommandLineOptions>(args).WithParsed(options =>
            {
                Console.WriteLine("Working on extracting...");
                Console.WriteLine("Declared NuGet path: " + options.NuGetPath);

                var completed = Extractor.DownloadPackages(options.PackagePath, options.OutputPath, options.Framework, new System.Collections.Generic.KeyValuePair<string, string>(options.Username, options.Password), options.Feed, options.NuGetPath);

                Console.Write("Completed successfully: " + completed);
            });
        }
    }
}
