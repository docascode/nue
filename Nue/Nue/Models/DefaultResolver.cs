using Nue.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Nue.Models
{
    public class DefaultResolver : IPackageResolver
    {
        public IDictionary<string, string> Parameters { get; set; }

        public string GetBinary(string path)
        {
            throw new NotImplementedException();
        }
    }
}
