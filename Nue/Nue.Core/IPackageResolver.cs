using System.Collections.Generic;
using System.Threading.Tasks;

namespace Nue.Core
{
    public interface IPackageResolver
    {
        bool CopyBinarySet(PackageAtom package, RunSettings runSettings, string outputPrefix = "");
    }
}
