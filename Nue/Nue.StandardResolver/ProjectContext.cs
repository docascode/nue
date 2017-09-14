using NuGet.Packaging;
using NuGet.ProjectManagement;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace Nue.StandardResolver
{
    public class ProjectContext : INuGetProjectContext
    {
        TelemetryServiceHelper telemetryHelper;

        public ProjectContext()
        {
            telemetryHelper = new TelemetryServiceHelper();
        }

        public void Log(MessageLevel level, string message, params object[] args)
        {
            // Do your logging here...
        }

        public FileConflictAction ResolveFileConflict(string message) => FileConflictAction.Ignore;

        public PackageExtractionContext PackageExtractionContext { get; set; }

        public XDocument OriginalPackagesConfig { get; set; }

        public ISourceControlManagerProvider SourceControlManagerProvider => null;

        public ExecutionContext ExecutionContext => null;

        public void ReportError(string message)
        {
        }

        public NuGetActionType ActionType { get; set; }

        public TelemetryServiceHelper TelemetryService
        {
            get
            {
                return telemetryHelper;
            }
            set
            {
                telemetryHelper = value;
            }
        }
    }
}
