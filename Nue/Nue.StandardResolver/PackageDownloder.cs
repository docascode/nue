using System;
using System.IO;
using System.IO.Compression;
using System.Net;

namespace Nue.StandardResolver
{
    public class PackageDownloder
    {
        private string rootUrl = "http://nuget.org/api/v2/package/";
        private string outPutPath;
        private string fileName;
        private string packageId;
        private string version;
        private bool overwrite;
        public string Url
        {
            get
            {
                var url = CombineUriToString(rootUrl, packageId);
                if (!string.IsNullOrEmpty(version))
                {
                    url = $"{url}/{version}";
                }

                return url;
            }
        }

        public PackageDownloder(string outPutPath, string packageId, string version, bool overwrite = false)
        {
            this.outPutPath = outPutPath;
            this.packageId = packageId;
            this.overwrite = overwrite;
            this.version = version;
        }

        public void DownloadPackage()
        {
            if (string.IsNullOrEmpty(outPutPath))
            {
                Console.WriteLine($"[error] outPutPath cannot be empty"); return;
            }

            if (string.IsNullOrEmpty(packageId))
            {
                Console.WriteLine($"[error] packageId cannot be empty"); return;
            }

            if (!Directory.Exists(outPutPath))
            {
                Directory.CreateDirectory(outPutPath);
            }

            var request = WebRequest.Create(Url) as HttpWebRequest;
            var response = request.GetResponse() as HttpWebResponse;
            fileName = response.Headers["Content-Disposition"];
            if (string.IsNullOrEmpty(fileName))
            {
                fileName = response.ResponseUri.Segments[response.ResponseUri.Segments.Length - 1];
            }
            else
            {
                fileName = fileName.Remove(0, fileName.IndexOf("filename=") + 9);
            }

            using (var responseStream = response.GetResponseStream())
            {
                long totalLength = response.ContentLength;
                using (var stream = new FileStream(Path.Combine(outPutPath, fileName), overwrite ? FileMode.Create : FileMode.CreateNew))
                {
                    byte[] bArr = new byte[1024];
                    int size;
                    while ((size = responseStream.Read(bArr, 0, bArr.Length)) > 0)
                    {
                        stream.Write(bArr, 0, size);
                    }
                }
            }
        }

        public void Unzip()
        {
            ModifyFileNameExtension();
            var sourceFileName = Path.Combine(outPutPath, fileName);
            var zipPath = Path.Combine(outPutPath, $"{Path.GetFileNameWithoutExtension(sourceFileName)}");
            if (!Directory.Exists(zipPath))
            {
                Directory.CreateDirectory(zipPath);
            }

            ZipFile.ExtractToDirectory(fileName, zipPath);
        }

        private void ModifyFileNameExtension()
        {
            var sourceFileName = Path.Combine(outPutPath, fileName);
            var destFileName = Path.Combine(outPutPath, $"{Path.GetFileNameWithoutExtension(sourceFileName)}.zip");
            File.Move(sourceFileName, destFileName);
            fileName = destFileName;
        }

        private string CombineUriToString(string baseUri, string relativeOrAbsoluteUri)
        {
            return new Uri(new Uri(baseUri), relativeOrAbsoluteUri).ToString();
        }
    }
}
