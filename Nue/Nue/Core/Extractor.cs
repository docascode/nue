using Microsoft.VisualBasic.FileIO;
using Nue.StandardResolver;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

namespace Nue.Core
{
    public class Extractor
    {
        public static void PreparePropertyBag(IEnumerable<PackageAtom> packages, string defaultTargetFramework)
        {
            foreach (var package in packages)
            {

                if (package.CustomPropertyBag ==null)
                {
                    package.CustomPropertyBag = new Dictionary<string, string>();
                }

                // Inject the TFM into the resolver if none was specified for the package.
                if (!package.CustomPropertyBag.ContainsKey("tfm"))
                {
                    package.CustomPropertyBag.Add("tfm", defaultTargetFramework);
                }

                // Determines whether a package is a PowerShell package - there is some custom logic that we need
                // to apply to determine what the assemblies are there.
                if (package.CustomPropertyBag.ContainsKey("ps"))
                {
                    package.IsPowerShellPackage = Convert.ToBoolean(package.CustomPropertyBag["ps"]);
                }
            }
        }

        public static bool DownloadPackages(string packagePath, string outputPath, string targetFramework, KeyValuePair<string,string> credentials = new KeyValuePair<string,string>(), string feed = "", string nugetPath = "")
        {
            if (string.IsNullOrWhiteSpace(packagePath) || string.IsNullOrWhiteSpace(outputPath)) return false;

            var packages = GetPackagesFromFile(packagePath);

            PreparePropertyBag(packages, targetFramework);

            Parallel.ForEach(packages, (package) =>
            {
                // Package resolver that will be used to get the full path to binaries.
                IPackageResolver resolver = new Resolver();

                var currentOutputPrefix = Guid.NewGuid().ToString();
                var isSuccess = resolver.CopyBinarySet(package, outputPath, credentials, feed, nugetPath, currentOutputPrefix);

                try
                {
                    Console.WriteLine($"[info] Deleting {Path.Combine(outputPath, "_pacman" + currentOutputPrefix)}");
                    Helpers.DeleteDirectory(Path.Combine(outputPath, "_pacman" + currentOutputPrefix));
                }
                catch
                {
                    Console.WriteLine("[error] Errored out the first time we tried to delete the folder. Retrying...");

                    Thread.Sleep(2000);
                    Helpers.DeleteDirectory(Path.Combine(outputPath, "_pacman" + currentOutputPrefix));
                }
            });

            return true;
        }

        private static List<PackageAtom> GetPackagesFromFile(string packagePath)
        {
            var packages = new List<PackageAtom>();

            using (var parser = new TextFieldParser(packagePath))
            {
                parser.TextFieldType = FieldType.Delimited;
                parser.SetDelimiters(",");

                while (!parser.EndOfData)
                {
                    var fields = parser.ReadFields();

                    if (fields == null) continue;

                    // Given the conventions, let's find out how many versions are requested to be downloaded.

                    for (var i = 0; i < fields.Length; i++)
                    {
                        var pAtom = new PackageAtom();

                        if (fields.Length == 2)
                        {
                            // There is no version specified.
                            pAtom.Moniker = fields[0];
                            pAtom.MonikerBase = fields[0];
                            pAtom.Name = fields[1];
                            pAtom.Version = "Unknown";
                        }
                        else if (fields.Length > 2)
                        {
                            // There is a version specified.
                            pAtom.Moniker = fields[0] + "-" + fields[2];
                            pAtom.MonikerBase = fields[0];
                            pAtom.Name = fields[1];
                            pAtom.Version = fields[2];
                        }
                        else
                        {
                            Console.WriteLine("[error] Could not read in package information for " + fields.ToString());
                            break;
                        }

                        // Property bag will be formatted like:
                        // [property1=value1;property2=value2]PackageId
                        var propertyBagRegex = @"(\[.+\])";
                        Regex formalizedRegEx = new Regex(propertyBagRegex);
                        var match = formalizedRegEx.Match(pAtom.Name);

                        if (match.Success)
                        {
                            // There seems to be a property bag attached to the name.
                            var rawPropertyBag = match.Value.Replace("[","").Replace("]","").Trim();
                            if (!string.IsNullOrWhiteSpace(rawPropertyBag))
                            {
                                // Normalize the package name without the property bag.
                                pAtom.Name = pAtom.Name.Replace(match.Value, "");
                                pAtom.CustomPropertyBag = new Dictionary<string, string>();

                                // Avoiding the case of empty property bag, looks like in this case we are good.
                                var properties = rawPropertyBag.Split(new char[] { ';' });
                                foreach(var property in properties)
                                {
                                    var splitProperty = property.Split(new char[] { '=' });
                                    pAtom.CustomPropertyBag.Add(splitProperty[0], splitProperty[1]);
                                }
                            }
                        }

                           packages.Add(pAtom);
                    }
                }
            }

            return packages;
        }
    }
}