using System;
using System.Collections.Generic;

namespace Libber.Models
{
    public class Book
    {
        public int Id { get; set; }

        public string BookIdentifier { get; set; }

        public string Contents { get; set; }

        public string Title { get; set; }

        public string ShortTitle { get; set; }

        public string Edition { get; set; }

        public int PageCount { get; set; }

        public int Year { get; set; }

        public string Description { get; set; }

        public List<String> Authors { get; set; }

        public string Publisher { get; set; }

        public long FileSize { get; set; }

        public string FilePath { get; set; }

        public DateTime FileLastModified { get; set; }

        public List<String> Tags { get; set; }

        public bool IsFavorite { get; set; }
    }
}
