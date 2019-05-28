using System;
using System.IO;

namespace Extractor
{
    internal static class Program
    {
        private static void Main()
        {
            ExtractFilesFromDirectory(Directory.GetCurrentDirectory());
            DeleteEmptyFolders(Directory.GetCurrentDirectory());
        }

        private static void ExtractFilesFromDirectory(string path)
        {
            var directory = new DirectoryInfo(path);

            foreach (var file in directory.EnumerateFiles())
            {
                if (file.Extension != ".fb2" && file.Extension != ".zip") continue;
                
                try
                {
                    file.MoveTo(Path.Combine(Directory.GetCurrentDirectory(), file.Name));
                }
                catch (Exception e)
                {
                    Console.WriteLine(e);
                    Console.WriteLine(file.Name);
                }
            }

            foreach (var dir in directory.EnumerateDirectories())
            {
                ExtractFilesFromDirectory(dir.FullName);
            }
        }
        
        private static void DeleteEmptyFolders(string path)
        {
            var directories = Directory.GetDirectories(path);
            var files = Directory.GetFiles(path);

            if (directories.Length == 0 && files.Length == 0)
                Directory.Delete(path);
            else
                foreach (var directory in directories)
                    DeleteEmptyFolders(directory);
        }
    }
}