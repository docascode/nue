using Newtonsoft.Json;
using Nue.Models;
using System.IO;
using System.Net;
using System.Text;

namespace Nue.Core
{
    public class Lister
    {
        public static bool CreatePackageListing(string owner, string outputPath, int lookUpVersions, string feedUrl)
        {
            var client = new WebClient();
            var jsonString = client.DownloadString(string.Format(feedUrl, owner));

            var data = JsonConvert.DeserializeObject<DataContainer>(jsonString);

            jsonString = client.DownloadString(string.Format(feedUrl, owner) + "&take=" + data.TotalHits);
            data = JsonConvert.DeserializeObject<DataContainer>(jsonString);

            var csv = new StringBuilder();
            foreach(var package in data.Data)
            {
                if (package.Title.ToLower().Contains("deprecated")) continue;
                var line = package.Id + "," + package.Id;

                if (package.Versions.Length > lookUpVersions)
                {
                    for (var i = lookUpVersions; i > 0; i--)
                    {
                        line += "," + package.Versions[package.Versions.Length - i].Version;
                    }
                }
                else
                {
                    for (var index = 0; index < package.Versions.Length; index++)
                    {
                        var version = package.Versions[index];
                        line += "," + version.Version;
                    }
                }

                  
                csv.AppendLine(line);
            }

            File.WriteAllText(Path.Combine(outputPath, "_paclist.csv"), csv.ToString());

            return true;
        }
    }
}
