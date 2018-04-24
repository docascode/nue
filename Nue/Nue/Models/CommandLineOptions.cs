using CommandLine;

namespace Nue.Models
{
    internal class CommandLineOptions
    {
        [Option('m', "mode", Required = true, HelpText = "Nue mode of operation. Can be: extract, listpac.")]
        public string Mode { get; set; }

        [Option('p', "packages", Required = false, HelpText = "Path to package list CSV.")]
        public string PackagePath { get; set; }

        [Option('o', "output", Required = false, HelpText = "Determines where to output the files.")]
        public string OutputPath { get; set; }

        [Option('f', "framework", Required = false, HelpText = "In extractor mode, determines the framework for which to get the binaries.")]
        public string Framework { get; set; }

        [Option('a', "account", Required = false, HelpText = "Account from which to pull packages.")]
        public string Account { get; set; }

        [Option('n', "nugetpath", Required = false, HelpText = "Path to NuGet.exe (just the folder).")]
        public string NuGetPath { get; set; }

        [Option('s', "source", Required = false, HelpText = "Folder for local packages, if le mode is used.")]
        public string PackageSource { get; set; }

        [Option('P', "password", Required = false, HelpText = "Password for the feed to be used.", Default = "")]
        public string Password { get; set; }

        [Option('U', "username", Required = false, HelpText = "Username for the feed to be used.", Default = "")]
        public string Username { get; set; }
    }
}
