using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows.Documents;
using System.Windows.Markup;
using Libber.Models;

namespace Libber
{
    public static class LibberHelpers
    {
        public static string RemoveHtmlTags(string input)
        {
            return Regex.Replace(input, @"<[^>]+>|&nbsp;", "").Trim();
        }

        public static string PrepareAmazonSearchRequest(string input)
        {
            const string amazonUrlReq = "http://www.amazon.com/s/ref=nb_sb_noss?url=search-alias%3Dstripbooks&field-keywords={0}";
            return String.Format(amazonUrlReq, Uri.EscapeDataString(input));
        }

        private static string GetValidXamlSection(string preparedCode)
        {
            var res = String.Format("<Section xmlns=\"http://schemas.microsoft.com/winfx/2006/xaml/presentation\" " +
                "xmlns:local=\"clr-namespace:MARS\"><Paragraph>{0}</Paragraph></Section>", preparedCode);
            return res;
        }

        public static Block CompileBookDescription(string input, Book bookInfo)
        {

            string header = "";
            header += String.Format("<Span FontSize=\"18\"><Bold>{0}</Bold></Span><LineBreak/><Span Foreground=\"DarkGreen\" FontSize=\"12\"><Bold>Publisher: {1} | Year: {2}</Bold></Span><LineBreak/><LineBreak/>", bookInfo.Title, bookInfo.Publisher, bookInfo.Year);
            // Заголовки
            input = Regex.Replace(input, @"^==\s*(.*)$", "<Span FontSize=\"14\"><Bold>$1</Bold></Span>", RegexOptions.Multiline);
            // Поддержка жирного текста
            input = Regex.Replace(input, @"\[b\](.*?)\[/b\]", "<Bold>$1</Bold>", RegexOptions.Multiline);
            // Поддержка курсивного текста
            input = Regex.Replace(input, @"\[i\](.*?)\[/i\]", "<Italic>$1</Italic>", RegexOptions.Multiline);
            input = Regex.Replace(input, @"&", "&amp;", RegexOptions.Multiline);
            // Поддержка списков, костыль на костыле
            input = Regex.Replace(input, @"^-- (.*)$", "&#x25CF;&#x202F;&#x202F;$1", RegexOptions.Multiline);
            // Пустая строка конвертируется в новый абзац
            input = Regex.Replace(input, @"(\r\n?|\n|\r)", "<LineBreak/>", RegexOptions.Multiline);
            Block result;
            try {
                result = (Block)XamlReader.Parse(GetValidXamlSection(header + input));
            } catch (XamlParseException ex) {
                result = (Block)XamlReader.Parse(GetValidXamlSection(String.Format("{0}: {1}",
                    "Ошибка в форматировании описания к документу", ex.Message)));
            }
            return result;
        }
    }
}
