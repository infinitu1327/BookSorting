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
    public class Book
    {
        public FileInfo File { get; set; }
        public string Author { get; set; }
    }

    internal class Program
    {
        private static void Main(string[] args)
        {
            Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);

            var files = GetFiles(Directory.GetCurrentDirectory());
            var directories = GetDirectories().ToList();

            Console.Write("Получаем список книг...");

            var books = GetBooks(files);

            Console.WriteLine($"\r Список книг получен. Всего книг:{books.Count}");
            Console.WriteLine("Начинаем сортировку...");

            foreach (var book in books)
            {
                var directory = GetDirectory(directories, book.Author);
                var filename = book.File.Name;

                try
                {
                    if (directory == null)
                    {
                        var newDirectory =
                            Directory.CreateDirectory(Path.Combine(Directory.GetCurrentDirectory(), book.Author));
                        directories.Add(newDirectory.FullName);
                        File.Move(book.File.FullName, Path.Combine(newDirectory.FullName, filename));
                    }
                    else
                    {
                        if (directory != book.File.DirectoryName && !File.Exists(Path.Combine(directory, filename)))
                            File.Move(book.File.FullName, Path.Combine(directory, filename));
                    }
                }
                catch (Exception)
                {
                    Console.WriteLine($"{book.File.Name} ignored");
                }
            }

            Console.WriteLine("Сортировка выполнена!");

            Console.WriteLine("Подчищаем пустые папки...");
            Clear(Directory.GetCurrentDirectory());
            Console.WriteLine("Готово!");
        }

        private static void Clear(string path)
        {
            var directories = Directory.GetDirectories(path);
            var files = Directory.GetFiles(path);

            if (directories.Length == 0 && files.Length == 0)
                Directory.Delete(path);
            else
                foreach (var directory in directories)
                    Clear(directory);
        }

        private static List<Book> GetBooks(IEnumerable<string> files)
        {
            var books = new List<Book>();

            foreach (var file in files)
                try
                {
                    switch (new FileInfo(file).Extension)
                    {
                        case ".zip":
                            using (var archive = ZipFile.OpenRead(file))
                            {
                                books.AddRange(archive.Entries
                                    .Where(entry => new FileInfo(entry.FullName).Extension == ".fb2")
                                    .Select(zipArchiveEntry =>
                                        new Book
                                        {
                                            Author = GetAuthorAsync(zipArchiveEntry.Open()).Result,
                                            File = new FileInfo(file)
                                        }
                                    ));
                            }
                            break;
                        case ".fb2":
                            books.Add(new Book
                            {
                                Author = GetAuthorAsync(File.OpenRead(file)).Result,
                                File = new FileInfo(file)
                            });
                            break;
                    }
                }
                catch (Exception)
                {
                    books.Add(new Book {Author = "Error Error", File = new FileInfo(file)});
                }

            return books;
        }

        private static async Task<string> GetAuthorAsync(Stream stream)
        {
            var book = await new FB2Reader().ReadAsync(stream, new XmlLoadSettings(new XmlReaderSettings()));
            var author = book.TitleInfo.BookAuthors.ToList()[0];

            return $"{author?.FirstName} {author?.MiddleName} {author?.LastName}".Replace("  ", " ");
        }

        private static IEnumerable<string> GetFiles(string path)
        {
            var files = new List<string>();
            var directories = Directory.GetDirectories(path);

            foreach (var directory in directories)
            {
                var directoryInfo = new DirectoryInfo(directory);

                if (directoryInfo.Attributes != FileAttributes.Hidden)
                    files.AddRange(GetFiles(directory));
            }

            files.AddRange(Directory.GetFiles(path));
            return files;
        }

        private static IEnumerable<string> GetDirectories()
        {
            return Directory.GetDirectories(Directory.GetCurrentDirectory());
        }

        private static string GetDirectory(IEnumerable<string> directories, string name)
        {
            return directories.FirstOrDefault(directory => directory.Contains(name));
        }
    }
}