using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using FB2Library;

namespace BookSorting
{
    internal static class Program
    {
        private static void Main(string[] args)
        {
            Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);

            Console.WriteLine("Начинаем сортировку...");
            Sort().Wait();
            Console.WriteLine("Подчищаем пустые папки...");
            DeleteEmptyFolders(Directory.GetCurrentDirectory());
            Console.WriteLine("Готово!");
        }

        private static async Task Sort()
        {
            var files = GetFiles();

            foreach (var file in files)
                switch (file.Extension)
                {
                    case ".fb2":
                        await ProcessFile(ProcessFb2, file);
                        break;

                    case ".zip":
                        await ProcessFile(ProcessFb2FromArchive, file);
                        break;
                }
        }

        private static async Task LogError(string info)
        {
            await File.AppendAllTextAsync(Path.Combine(Directory.GetCurrentDirectory(), "log.txt"), info);
        }

        private static async Task ProcessFb2(FileInfo file)
        {
            var author = await GetAuthorAsync(File.OpenRead(file.FullName));
            file.MoveTo(Path.Combine(GetAuthorDirectory(author), file.Name));
        }

        private static async Task ProcessFb2FromArchive(FileInfo file)
        {
            var archive = ZipFile.OpenRead(file.FullName);
            var fb2File = archive
                .Entries
                .FirstOrDefault(entry =>
                    new FileInfo(entry.FullName).Extension == ".fb2" ||
                    new FileInfo(entry.FullName).Extension == ".fbd");

            if (fb2File == null) return;
            var author = await GetAuthorAsync(fb2File.Open());
            archive.Dispose();
            file.MoveTo(Path.Combine(GetAuthorDirectory(author), file.Name));
        }

        private static async Task ProcessFile(Func<FileInfo,Task> action, FileInfo file)
        {
            try
            {
                await action.Invoke(file);
            }
            catch (Exception)
            {
                await LogError($"Ошибка с {file} {Environment.NewLine}");
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

        private static async Task<string> GetAuthorAsync(Stream stream)
        {
            var book = await new FB2Reader().ReadAsync(stream, new XmlLoadSettings(new XmlReaderSettings()));
            var author = book.TitleInfo.BookAuthors.ElementAt(0);
            stream.Dispose();

            return $"{author?.FirstName} {author?.MiddleName} {author?.LastName}".Replace("  ", " ").Trim();
        }

        private static IEnumerable<FileInfo> GetFiles()
        {
            return new DirectoryInfo(Directory.GetCurrentDirectory())
                .EnumerateFiles();
        }

        private static string GetAuthorDirectory(string author)
        {
            var authorDirectory = Directory
                .EnumerateDirectories(Directory.GetCurrentDirectory())
                .FirstOrDefault(directory => directory.Contains(author));

            return authorDirectory ?? Directory.CreateDirectory(author).FullName;
        }
    }
}