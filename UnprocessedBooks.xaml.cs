using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using DjvuNet;
using iTextSharp.text.pdf;
using JsonConfig;
using Libber.Models;
using NKCraddock.AmazonItemLookup;
using NKCraddock.AmazonItemLookup.Client;
using Raven.Abstractions.Extensions;

namespace Libber
{
    public partial class UnprocessedBooks
    {
        public BindingList<FileInfo> UnprocessedBooksCollection { get; set; }
        public Book FocusedBook { get; set; }

        public Book BookInformation { get; set; }

        public UnprocessedBooks()
        {
            InitializeComponent();
            DataContext = this;
            UnprocessedBooksCollection = new BindingList<FileInfo>();
        }
        private Task<List<FileInfo>> LoadFiles()
        {
            var dir = (string)Config.Default.BooksDirectory;
            return Task.Run(() => Directory
                .EnumerateFiles(dir, "*.*", SearchOption.AllDirectories)
                .Select(fn => new FileInfo(fn))
                .OrderBy(fi => fi.Name).ToList());
        }

        private Task<string> GetFileContents(string fileName)
        {
            return Task.Run(() => {
                var bookmarks = new BookBookmarks(fileName);
                return bookmarks.GetBookmarks();
            });
        }

        private Task<int> GetPageCount(string fileName)
        {
            return Task.Run(() => {
                int pageCount = 0;
                var fileInfo = new FileInfo(fileName);
                switch (fileInfo.Extension) {
                    case ".pdf":
                        pageCount = new PdfReader(fileInfo.FullName).NumberOfPages;
                        break;
                    case ".djvu":
                        pageCount = new DjvuDocument(fileInfo.FullName).Pages.Length;
                        break;
                }
                return pageCount;
            });
        }

        public Task<AwsItem> GetByAsin(string asin)
        {
            var i = new ProductApiConnectionInfo {
                AWSAccessKey = Config.Default.AmazonAccessKey,
                AWSSecretKey = Config.Default.AmazonSecretKey,
                AWSAssociateTag = Config.Default.AmazonAwsTag
            };
            var client = new AwsProductApiClient(i);
            return Task.Run(() => client.ItemLookupByAsin(asin));
        }

        private async void UngroupedBooksForm_Loaded(object sender, RoutedEventArgs e)
        {
            var files = await LoadFiles();
            HashSet<string> dbBooks;
            using (var session = Db.DocumentStore.OpenSession()) {
                dbBooks = session.Query<Book>().ToList().Select(b => b.FilePath).ToHashSet();
            }
            foreach (var f in files.Where(f => ! dbBooks.Contains(f.FullName))) {
                UnprocessedBooksCollection.Add(f);
            }
            if (BookInformation != null) {
                ReloadInformation();
            }
        }

        private void SearchBookAtAmazon(object sender, RoutedEventArgs e)
        {
            var item = (FileInfo)UnprocessedBookList.SelectedItem;
            var searchReq = Path.GetFileNameWithoutExtension(item.Name);
            Process.Start(LibberHelpers.PrepareAmazonSearchRequest(searchReq));

        }

        // TODO: сделать все по человечески
        public void UpdateFields()
        {
            BookInformation.Title = BookTitle.Text;
            BookInformation.Year = Int32.Parse(BookYear.Text);
            BookInformation.Authors = BookAuthors.Text.Split(';').ToList();
            BookInformation.Publisher = BookPublisher.Text;
            BookInformation.Description = BookDescription.Text;
            BookInformation.Tags = BookTags.Text.Split(';').ToList();
            BookInformation.BookIdentifier = BookIdentifier.Text;
            BookInformation.Edition = BookEdition.Text;
        }

        public void ReloadInformation()
        {
            BookTitle.Text = BookInformation.Title;
            BookYear.Text = BookInformation.Year.ToString(CultureInfo.InvariantCulture);
            BookAuthors.Text = String.Join(";", BookInformation.Authors ?? new List<string>());
            BookPublisher.Text = BookInformation.Publisher;
            BookDescription.Text = BookInformation.Description;
            PageCount.Text = BookInformation.PageCount.ToString(CultureInfo.InvariantCulture);
            BookContents.Text = BookInformation.Contents;
            BookTags.Text = String.Join(";", BookInformation.Tags ?? new List<string>());
            BookIdentifier.Text = BookInformation.BookIdentifier;
            BookEdition.Text = BookInformation.Edition;
        }

        private async void UnprocessedBookList_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            var item = (FileInfo)UnprocessedBookList.SelectedItem;
            Book dbDocument;
            using (var session = Db.DocumentStore.OpenSession()) {
                dbDocument = session.Query<Book>().FirstOrDefault(b => b.FilePath == item.FullName);
            }
            BookInformation = dbDocument ?? new Book();
            if (dbDocument == null) {
                BookContents.Text = PageCount.Text = "Загрузка...";
                SaveItemBtn.IsEnabled = false;
                UnprocessedBookList.IsEnabled = false;
                BookInformation.FilePath = item.FullName;
                BookInformation.FileSize = item.Length;
                BookInformation.FileLastModified = item.LastWriteTimeUtc;
                BookInformation.Contents = await GetFileContents(item.FullName);
                BookInformation.PageCount = await GetPageCount(item.FullName);
                SaveItemBtn.IsEnabled = true;
                UnprocessedBookList.IsEnabled = true;
            }
            ReloadInformation();
        }

        private void CloseFormBtn_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        private void SaveItemBtn_Click(object sender, RoutedEventArgs e)
        {
            UpdateFields();
            using (var session = Db.DocumentStore.OpenSession()) {
                session.Store(BookInformation);
                session.SaveChanges();
            }
        }

        private async void FillBookInformation_Click(object sender, RoutedEventArgs e)
        {
            try {
                var info = await GetByAsin(BookIdentifier.Text);
                var bookTitle = info.ItemAttributes["Title"];
                BookInformation.Title = bookTitle;
                BookInformation.Publisher = info.ItemAttributes["Publisher"];
                BookInformation.Description = LibberHelpers.RemoveHtmlTags(info.Description);
                var unprocessedDate = Regex.Matches(info.ItemAttributes["PublicationDate"], @"^(\d+)-")[0];
                BookInformation.Year = Int32.Parse(unprocessedDate.Groups[1].Value);
                BookInformation.Authors = new List<string> { info.ItemAttributes["Author"] };
                BookInformation.BookIdentifier = BookIdentifier.Text;
                BookInformation.Edition = info.ItemAttributes["Edition"] ?? "1";
                ReloadInformation();
            } catch (Exception) {
                MessageBox.Show("Не удалось загрузить необходимую информацию");
            }
        }

        private void OpenSelectedBook(object sender, MouseButtonEventArgs e)
        {
            var item = (ListBoxItem)e.Source;
            var file = (FileInfo)item.Content;
            var fileName = file.FullName;
            Process.Start(fileName);
        }
    }
}
