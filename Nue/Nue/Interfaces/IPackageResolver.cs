using Nue.Models;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Nue.Interfaces
{
    interface IPackageResolver
    {
        IDictionary<string, string> Parameters { get; set; }

        Task<bool> CopyBinarySet(PackageAtom package, string outputPath);
    }
}
