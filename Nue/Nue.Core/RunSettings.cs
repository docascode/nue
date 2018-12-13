using System;
using System.Collections.Generic;
using System.Text;

namespace Nue.Core
{
    public class RunSettings
    {
        public const string NUGET_DEFAULT_FEED = "https://api.nuget.org/v3/index.json";

        public readonly string TFM;

        public readonly string Feed;

        public readonly string NugetPath;

        public readonly string OutputPath;

        public RunSettings(string tfm, string feed, string nugetPath, string outputPath)
        {
            feed = feed?.Trim('\"');
            Feed = string.IsNullOrEmpty(feed) ? NUGET_DEFAULT_FEED : feed;

            TFM = tfm;
            NugetPath = nugetPath;
            OutputPath = outputPath;
        }
    }
}
