using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Nue.Models
{

    public class DataContainer
    {
        public Context Context { get; set; }
        public int TotalHits { get; set; }
        public DateTime LastReopen { get; set; }
        public string Index { get; set; }
        public PackageInfo[] Data { get; set; }
    }

    public class Context
    {
        public string Vocab { get; set; }
    }

    public class PackageInfo
    {
        public string Id { get; set; }
        public string Type { get; set; }
        public string Registration { get; set; }
        public string Version { get; set; }
        public string Description { get; set; }
        public string Summary { get; set; }
        public string Title { get; set; }
        public string IconUrl { get; set; }
        public string LicenseUrl { get; set; }
        public string ProjectUrl { get; set; }
        public string[] Tags { get; set; }
        public string[] Authors { get; set; }
        public int TotalDownloads { get; set; }
        public VersionInfo[] Versions { get; set; }
    }

    public class VersionInfo
    {
        public string Version { get; set; }
        public int Downloads { get; set; }
        public string Id { get; set; }
    }

}
