using System;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;
using System.Xml;

namespace Sorter
{
    internal static class Program
    {
        private static readonly XmlDocument XmlDocument = new XmlDocument();

        private static void Main()
        {
            Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);

            Console.WriteLine("Sorting started...");
            Sort();
            Console.WriteLine("Sorting done!");

            Console.WriteLine("Cleaning empty folders...");
            DeleteEmptyFolders(Directory.GetCurrentDirectory());
            Console.WriteLine("Cleaning done!");
        }

        private static void Sort()
        {
            var files = new DirectoryInfo(Directory.GetCurrentDirectory())
                .EnumerateFiles("*.zip");

            foreach (var file in files)
                try
                {
                    string authorDirectory;

                    using (var archive = ZipFile.OpenRead(file.FullName))
                    {
                        var fb2File = archive
                            .Entries
                            .FirstOrDefault(entry => entry.Name.Contains(".fb2") || entry.Name.Contains(".fbd"));

                        if (fb2File == null) continue;

                        authorDirectory = GetAuthorDirectory(fb2File.Open());
                    }

                    file.MoveTo(Path.Combine(authorDirectory, file.Name));
                }
                catch (Exception e)
                {
                    Console.WriteLine($"Error with {file.Name}:");
                    Console.WriteLine(e.Message);
                }
        }

        private static string GetAuthorDirectory(Stream fileStream)
        {
            XmlDocument.Load(fileStream);

            var authorTag = XmlDocument.GetElementsByTagName("author")[0];

            var lastName = authorTag["last-name"]?.InnerText;
            var firstName = authorTag["first-name"]?.InnerText;
            var middleName = authorTag["middle-name"]?.InnerText;

            return Directory.CreateDirectory($"{lastName} {firstName} {middleName}".Trim()).FullName;
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