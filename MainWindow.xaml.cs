using System;
using System.Diagnostics;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using Libber.Models;
using Raven.Client;
using Raven.Client.Linq;

namespace Libber
{

    public partial class MainWindow
    {
        public SortableBindingList<Book> Books { get; set; }
        public string SearchRequestText { get; set; }
        public string FilterQuery { get; set; }

        public MainWindow()
        {
            InitializeComponent();
            DataContext = this;
            Books = new SortableBindingList<Book>();
        }

        private void OpenCurrentBook()
        {
            try {
                var book = (Book)BooksList.SelectedItem;
                Process.Start(book.FilePath);
            } catch(NullReferenceException) {
                MessageBox.Show("Can't open the book");
            }
        }

        private void ReloadBooksCollection()
        {
            using (var session = Db.DocumentStore.OpenSession()) {
                var records = session.Query<Book>();
                if (!string.IsNullOrEmpty(FilterQuery)) {
                    var tokensRegEx = new Regex(@"(?<field>tag|year|y|p):(?<value>\S+)", RegexOptions.Compiled);
                    var parseTokens = tokensRegEx.Matches(FilterQuery);
                    var searchParts = (parseTokens.Cast<Match>()).Select(m => new { field = m.Groups["field"].Value, value = m.Groups["value"].Value });
                    var _iQuery = tokensRegEx.Replace(FilterQuery, "");
                    var searchReq = String.Format("*{0}*", _iQuery);
                    records = records.Search(b => b.Title, searchReq, escapeQueryOptions: EscapeQueryOptions.AllowAllWildcards);
                    foreach (var sp in searchParts) {
                        switch (sp.field) {
                            case "tag": case "t":
                                records = records.Where(b => b.Tags.Any(t => t == sp.value));
                                break;
                            case "year": case "y":
                                int year;
                                if (Int32.TryParse(sp.value, out year)) {
                                    records = records.Where(b => b.Year == year);
                                }
                                break;
                        }
                    }
                }
                Books.Load(records.OrderBy(b => b.Title));
            }
        }

        private async void Grid_Loaded(object sender, RoutedEventArgs e)
        {
            MainMenu.IsEnabled = false;
            await Task.Run(() => Db.DocumentStore.Initialize());
            ReloadBooksCollection();
            MainMenu.IsEnabled = true;
        }

        private void OpenEditBookInformationForm(object sender, RoutedEventArgs e)
        {
            new UnprocessedBooks {
                BookInformation = (Book)BooksList.SelectedItem
            }.ShowDialog();
            ReloadBooksCollection();
            Books.ResetBindings();
        }

        private void OpenUnprocessedBooksForm(object sender, RoutedEventArgs e)
        {
            new UnprocessedBooks().ShowDialog();
            ReloadBooksCollection();
        }

        private void UpdateBookList(object sender, RoutedEventArgs e)
        {
            ReloadBooksCollection();
        }

        private void RemoveBookFromDatabase(object sender, RoutedEventArgs e)
        {
            var book = (Book)BooksList.SelectedItem;
            using (var session = Db.DocumentStore.OpenSession()) {
                var entity = session.Load<Book>(book.Id);
                session.Delete(entity);
                session.SaveChanges();
            }
            ReloadBooksCollection();
        }

        private void OpenBookFileWithViewer(object sender, MouseButtonEventArgs e)
        {
            OpenCurrentBook();
        }

        private void OpenBookFileWithViewer(object sender, RoutedEventArgs e)
        {
            OpenCurrentBook();
        }


        private void BooksList_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            var item = (Book)BooksList.SelectedItem;
            if (item != null) {
                SelectedBookDescription.Blocks.Clear();
                var description = item.Description ?? "Описание отсутствует";
                SelectedBookDescription.Blocks.Add(LibberHelpers.CompileBookDescription(description, item));
            }
        }

        private void ExitApplication(object sender, RoutedEventArgs e)
        {
            Environment.Exit(0);
        }

        private void ShowBookContents(object sender, RoutedEventArgs e)
        {
            new BookContent { Book = (Book)BooksList.SelectedItem }.ShowDialog();
        }

        private void SearchRequest_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key != Key.Return) return;
            FilterQuery = SearchRequest.Text;
            ReloadBooksCollection();
        }

        private void AddToFavorites(object sender, RoutedEventArgs e)
        {
            var item = (Book)BooksList.SelectedItem;
            item.IsFavorite = ! item.IsFavorite;
            using (var session = Db.DocumentStore.OpenSession()) {
                session.Store(item);
                session.SaveChanges();
            }
            Books.ResetBindings();
        }

    }
}
