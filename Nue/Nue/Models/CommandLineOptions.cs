using CommandLine;

namespace Nue.Models
{
    internal class CommandLineOptions
    {
        [Option('p', "packages", Required = false, HelpText = "Path to CSV with packages.")]
        public string PackagePath { get; set; }

        [Option('o', "output", Required = false, HelpText = "Determines where to output the binaries and their dependencies.")]
        public string OutputPath { get; set; }

        [Option('f', "framework", Required = false, HelpText = "Determines the framework for which to get the binaries.", Default = "net471")]
        public string Framework { get; set; }

        [Option('n', "nugetpath", Required = false, HelpText = "Path to folder containing NuGet.exe.")]
        public string NuGetPath { get; set; }

        [Option('P', "password", Required = false, HelpText = "Password for the feed to be used.", Default = "")]
        public string Password { get; set; }

        [Option('U', "username", Required = false, HelpText = "Username for the feed to be used.", Default = "")]
        public string Username { get; set; }

        [Option('F', "feed", Required = false, HelpText = "Custom feed to use with to download NuGet packages.", Default = "")]
        public string Feed { get; set; }
    }
}
