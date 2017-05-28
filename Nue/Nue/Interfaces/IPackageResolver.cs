using System.Collections.Generic;

namespace Nue.Interfaces
{
    interface IPackageResolver
    {
        IDictionary<string, string> Parameters { get; set; }

        string GetBinary(string path);
    }
}
