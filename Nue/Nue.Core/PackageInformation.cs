using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Nue.Core
{
    public class PackageInfomarion
    {
        public string Name { get; set; }

        public string Version { get; set; }

        public string Feed { get; set; }
    }

    // moniker1
    //   |__ assembly1 => package1
    //   |__ assembly2 => package2
    public class PackageInfomarionMapping : Dictionary<string, Dictionary<string, PackageInfomarion>>
    {
        public PackageInfomarionMapping ToFlatten(string newMoniker)
        {
            var result = new PackageInfomarionMapping();
            result[newMoniker] = new Dictionary<string, PackageInfomarion>();

            foreach (var moniker in this.Keys)
            {
                foreach (var mapping in this[moniker])
                {
                    result[newMoniker][mapping.Key] = mapping.Value;
                }
            }

            return result;
        }
    }

    public class AssemblyMappingPackageInformation : Dictionary<string, List<PackageInfomarion>>
    { 
    
    }
}
