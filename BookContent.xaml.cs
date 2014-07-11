using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using Libber.Models;

namespace Libber
{

    public partial class BookContent
    {

        public Book Book { get; set; }

        public BookContent()
        {
            InitializeComponent();
        }

        private void CloseWindowsBtn_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

        private void BookContentWindow_Loaded(object sender, RoutedEventArgs e)
        {
            BookTitle.Text = String.Format("{0}: {1}", Book.Authors.CommaJoin(), Book.Title);
            ContentText.Text = Book.Contents;
        }
    }
}
