using System;
using System.IO;
using System.IO.Compression;

namespace Archivator
{
    internal static class Program
    {
        private static void Main()
        {
            var directory = new DirectoryInfo(Directory.GetCurrentDirectory());

            foreach (var file in directory.EnumerateFiles())
            {
                if (file.Extension != ".fb2") continue;
                
                try
                {
                    using var fileStream = File.Create(Path.Combine(directory.FullName, $"{file.Name}.zip"));
                    using var archive = new ZipArchive(fileStream, ZipArchiveMode.Create, true);
                    archive.CreateEntryFromFile(file.FullName, file.Name, CompressionLevel.Optimal);
                    file.Delete();
                }
                catch (Exception e)
                {
                    Console.WriteLine(e);
                    Console.WriteLine(file.Name);
                }
            }
        }
    }
}