using NuGet.Frameworks;
using NuGet.ProjectManagement;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Nue.Models
{
    public class TargetedFolderNuGetProject : FolderNuGetProject
    {
        public TargetedFolderNuGetProject(string root, string targetFramework) : base(root)
        {
            InternalMetadata.Remove(NuGetProjectMetadataKeys.TargetFramework);
            InternalMetadata.Add(NuGetProjectMetadataKeys.TargetFramework, NuGetFramework.Parse(targetFramework));
        }
    }
}
