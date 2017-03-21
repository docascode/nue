using System.IO;

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
    }
}
