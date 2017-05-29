using System.Collections.Generic;

namespace Nue.Models
{
    public class PackageAtom
    {
        public string Moniker { get; set; }
        public string Name { get; set; }
        public string Version { get; set; }
        public string MonikerBase { get; set; }
        public bool IsMetaPackage { get; set; }
        public Dictionary<string,string> CustomPropertyBag { get; set; }
        public string GetFullName()
        {
            return $"{Name} {Version}";
        }
    }
}
