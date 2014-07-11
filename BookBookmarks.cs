using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using DjvuNet.DataChunks.Navigation;
using DjvuNet;
using iTextSharp.text.pdf;

namespace Libber
{
    using PDFNode = IEnumerable<Dictionary<string, object>>;
    public class BookBookmarks
    {
        public string FileName { get; set; }

        public string GetBookmarks()
        {
            switch (new FileInfo(FileName).Extension) {
                case ".pdf":
                    var pdfBm = SimpleBookmark.GetBookmark(new PdfReader(FileName));
                    return GetPdfBookmarkNode(pdfBm, 0);
                case ".djvu":
                    var djvuBm = new DjvuDocument(FileName).Navigation.Bookmarks;
                    return GetDjvuBookmarkNode(djvuBm, 0);
                default:
                    return "Не могу загрузить оглавление";
            }
        }

        private string GetTitle(string title, int level)
        {
            var spaces = new String(' ', level * 4);
            return String.Format("{0}{1}\n", spaces, title);
        }

        private string GetDjvuBookmarkNode(IEnumerable<Bookmark> root, int level)
        {
            var sb = new StringBuilder();
            foreach (var node in root) {
                sb.Append(GetTitle(node.Name, level));
                if (node.Children.Length > 0) {
                    var cn = GetDjvuBookmarkNode(node.Children, level + 1);
                    sb.Append(cn);
                }
            }
            return sb.ToString();
        }

        private string GetPdfBookmarkNode(PDFNode root, int level)
        {
            var sb = new StringBuilder();
            foreach (var node in root) {
                sb.Append(GetTitle((string)node["Title"], level));
                if (node.ContainsKey("Kids")) {
                    var cn = GetPdfBookmarkNode((PDFNode)node["Kids"], level + 1);
                    sb.Append(cn);
                }
            }
            return sb.ToString();
        }

        public BookBookmarks(string fileName)
        {
            var file = new FileInfo(fileName);
            if (file.Extension != ".pdf" && file.Extension != ".djvu") {
                throw new Exception("Only PDF and DjVu files allowed for this time");
            }
            FileName = fileName;
        }
    }
}
