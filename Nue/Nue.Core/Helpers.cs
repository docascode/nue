using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;

namespace Nue.Core
{
    public static class Helpers
    {
        // From: http://stackoverflow.com/a/329502
        public static void DeleteDirectory(string target_dir)
        {
            if (Directory.Exists(target_dir))
            {
                string[] files = Directory.GetFiles(target_dir);
                string[] dirs = Directory.GetDirectories(target_dir);

                foreach (string file in files)
                {
                    File.SetAttributes(file, FileAttributes.Normal);
                    File.Delete(file);
                }

                foreach (string dir in dirs)
                {
                    DeleteDirectory(dir);
                }

                Directory.Delete(target_dir, true);
            }
        }

        // Determines the best folder match for a libary based on the specified target moniker.
        public static string GetBestLibMatch(string tfm, string[] folderPaths)
        {
            var tfmRegex = new Regex(@"(?<Base>[a-zA-Z]*)(?<Version>[0-9\.0-9]*)");
            var match = tfmRegex.Match(tfm);

            var tfmBase = match.Groups["Base"].Value;
            var tfmVersion = match.Groups["Version"].Value;
            string folder = string.Empty;

            // Look for a folder that matches exactly the TFM.
            var exactMatch = (from c in folderPaths
                              where Path.GetFileName(c).Equals(tfm, StringComparison.CurrentCultureIgnoreCase)
                              select c).FirstOrDefault();

            // If we found one, we should just return it.
            if (exactMatch != null)
                return exactMatch;

            // As an example, if the TFM is net45, this should cover everything like:
            // net45, net451, net452
            var lenientMatch = new Regex($@"^(?<full>(?<base>{tfm})(?<version>[0-9\.0-9]*))$", RegexOptions.IgnoreCase);
            folder = GetWinningFolder(folderPaths, lenientMatch);

            if (!string.IsNullOrWhiteSpace(folder)) return folder;
            // Now we just match the base, e.g. for net we should get:
            // net45, net46, net461
            var baseMatch = new Regex($@"^(?<full>(?<base>{tfmBase}[a-z]*)(?<version>[0-9\.0-9]*))$", RegexOptions.IgnoreCase);
            folder = GetWinningFolder(folderPaths, baseMatch);

            if (!string.IsNullOrWhiteSpace(folder)) return folder;
            // Now do an even more lenient match within 
            var preciseTfmRegex = new Regex($@"(?<full>(?<version>{tfmBase})(?<version>[0-9\.0-9]+))", RegexOptions.IgnoreCase);
            folder = GetWinningFolder(folderPaths, preciseTfmRegex);

            if (!string.IsNullOrWhiteSpace(folder)) return folder;
            // Given that we have found nothing, is there anything that matches the first 3 characters?
            var broadAssumptionRegex = new Regex($@"(?<full>(?<version>{tfmBase.Substring(0, 3)})(?<version>[0-9\.0-9]+))", RegexOptions.IgnoreCase);
            folder = GetWinningFolder(folderPaths, broadAssumptionRegex);

            return folder;
        }

        public static bool CopyLibraryContent(string source, string destination, PackageAtom package)
        {
            var binaries = new List<string>();
            var docFiles = new List<string>();

            try
            {
                binaries = Directory.GetFiles(source, "*.*", SearchOption.TopDirectoryOnly)
                                .Where(s => s.EndsWith(".dll") || s.EndsWith(".winmd")).ToList();
            }
            catch
            {
                Console.WriteLine($"[error] Could not get binaries for {package.Name} from {source}.");
                return false;
            }


            foreach (var binary in binaries)
                File.Copy(binary, Path.Combine(destination, Path.GetFileName(binary)), true);

            try
            {
                docFiles = Directory.GetFiles(source, "*.xml", SearchOption.TopDirectoryOnly).ToList();
                foreach (var docFile in docFiles)
                    File.Copy(docFile, Path.Combine(destination, Path.GetFileName(docFile)), true);
            }
            catch
            {
                Console.WriteLine($"[warning] Could not get documentation files for {package.Name} from {source}.");
            }

            return true;
        }

        public static string BuildCommandString(PackageAtom package, string rootPath, string configPath, string defaultPackageSource)
        {
            var baseline = $@"install {package.Name} -Source ""{defaultPackageSource.Trim('"')}"" -OutputDirectory ""{rootPath.Trim('"')}"" -Verbosity Quiet -DisableParallelProcessing -FallbackSource https://api.nuget.org/v3/index.json -ConfigFile ""{configPath.Trim('"')}""";

            if (!string.IsNullOrWhiteSpace(package.CustomPropertyBag["tfm"]))
            {
                baseline += $" -Framework {package.CustomPropertyBag["tfm"]}";
            }

            if (!string.Equals(package.Version, "Unknown", StringComparison.CurrentCultureIgnoreCase))
            {
                baseline += $" -Version {package.Version}";
            }

            if (package.CustomPropertyBag.ContainsKey("isPrerelease"))
            {
                bool shouldIncludePrerelease = Convert.ToBoolean(package.CustomPropertyBag["isPrerelease"]);
                if (shouldIncludePrerelease)
                {
                    baseline += " -PreRelease";
                }
            }

            return baseline;
        }

        private static string GetWinningFolder(string[] folders, Regex regex)
        {
            var folderAssociations = new Dictionary<string, double>();
            foreach (var folder in folders)
            {
                var exactFolderName = Path.GetFileName(folder);
                var token = regex.Match(exactFolderName);
                if (!token.Success) continue;
                var folderVersion = token.Groups["version"].Value;

                if (!string.IsNullOrEmpty(folderVersion))
                {
                    folderAssociations.Add(folder, double.Parse(folderVersion));
                }
                else
                {
                    folderAssociations.Add(folder, 0);
                }
            }

            if (folderAssociations.Count <= 0) return string.Empty;
            var topItem = (from c in folderAssociations orderby c.Value descending select c).First();
            return topItem.Key;
        }
    }
}
