using NuGet.Frameworks;
using NuGet.ProjectManagement;

namespace Nue.StandardResolver
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
