using System;
using System.Diagnostics;
using CommandLine;
using Nue.Core;
using Nue.Models;

namespace Nue
{
    internal class Program
    {
        private static void Main(string[] args)
        {
            Stopwatch stopwatch = new Stopwatch();
            stopwatch.Start();

            DualOutput.Initialize();

            Console.WriteLine("[info] nue 2.0.0-9272018.1817");

            Parser.Default.ParseArguments<CommandLineOptions>(args).WithParsed(options =>
            {
                Console.WriteLine("[info] Declared NuGet path: " + options.NuGetPath);

                var completed = Extractor.DownloadPackages(options.PackagePath, options.OutputPath, options.Framework, new System.Collections.Generic.KeyValuePair<string, string>(options.Username, options.Password), options.Feed, options.NuGetPath);

                Console.WriteLine("[info] Completed successfully: " + completed);
            });

            stopwatch.Stop();
            Console.WriteLine($"[info] Completed extraction in {stopwatch.Elapsed.TotalMinutes} minutes");
            Debug.WriteLine($"[info] Completed extraction in {stopwatch.Elapsed.TotalMinutes} minutes");
        }
    }
}
