using Newtonsoft.Json;
using Nue.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Data;

namespace Nue.Core
{
    public class Lister
    {
        private static readonly object locket = new object();
        public static bool CreatePackageListing(string owner, string outputPath, int lookUpVersions, string feedUrl)
        {
            WebClient client = new WebClient();
            var jsonString = client.DownloadString(string.Format(feedUrl, owner));

            var data = JsonConvert.DeserializeObject<DataContainer>(jsonString);

            jsonString = client.DownloadString(string.Format(feedUrl, owner) + "&take=" + data.TotalHits);
            data = JsonConvert.DeserializeObject<DataContainer>(jsonString);

            var csv = new StringBuilder();
            foreach(var package in data.Data)
            {
                if (!package.Title.ToLower().Contains("deprecated"))
                {
                    string line = package.Id + "," + package.Id;

                    if (package.Versions.Length > lookUpVersions)
                    {
                        for (int i = lookUpVersions; i > 0; i--)
                        {
                            line += "," + package.Versions[package.Versions.Length - i].Version;
                        }
                    }
                    else
                    {
                        foreach (var version in package.Versions)
                        {
                            line += "," + version.Version;
                        }
                    }

                  
                    csv.AppendLine(line);
                }
            }

            File.WriteAllText(Path.Combine(outputPath, "_paclist.csv"), csv.ToString());

            return true;
        }
    }
}
