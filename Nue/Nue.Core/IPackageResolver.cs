using System.Collections.Generic;
using System.Threading.Tasks;

namespace Nue.Core
{
    public interface IPackageResolver
    {
        IDictionary<string, string> Parameters { get; set; }

        Task<bool> CopyBinarySet(PackageAtom package, string outputPath, KeyValuePair<string, string> credentials = new KeyValuePair<string, string>(), string feed = "");
    }
}
