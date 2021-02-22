using Newtonsoft.Json;
using System;
using System.Collections.Generic;

namespace Nue.Core
{
    public class PackageAtom
    {
        [JsonProperty("moniker")]
        public string Moniker { get; set; }

        [JsonProperty("id")]
        public string Name { get; set; }

        [JsonProperty("versionOption")]
        public VersionOption VersionOption { get; set; }

        [JsonProperty("customVersion")]
        public string CustomVersion { get; set; }

        [JsonIgnore]
        public bool CustomVersionDefined { get => VersionOption == VersionOption.Custom && !string.IsNullOrEmpty(CustomVersion); }

        [JsonProperty("isPrerelease")]
        public bool IsPrerelease { get; set; }

        public bool IsPowerShellPackage { get; set; }

        public bool IsDotnetPlatform { get; set; }

        [JsonProperty("customProperties")]
        public PackageAdditionalProperties CustomProperties { get; set; }

        public Dictionary<string, string> CustomPropertyBag { get; set; }

        public string GetFullName()
        {
            var versionStr = VersionOption == VersionOption.Custom ? CustomVersion : VersionOption.ToString();
            if (IsPrerelease)
            {
                versionStr += " -Prerelease";
            }
            return $"{Name} [Version {versionStr}]";
        }
    }

    public class PackageAdditionalProperties
    {
        [JsonProperty("feed")]
        public string CustomFeed { get; set; }

        [JsonProperty("libFolder")]
        public string CustomLibraryFolder { get; set; }

        [JsonProperty("depFolder")]
        public string CustomDependencyFolder { get; set; }

        [JsonProperty("tfm")]
        public string TFM { get; set; }

        [JsonProperty("excludedDlls")]
        public string[] ExcludedDlls { get; set; }
    }

    public enum VersionOption
    {
        Latest,
        Custom
    }
}
