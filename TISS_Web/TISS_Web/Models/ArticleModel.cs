using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace TISS_Web.Models
{
    public class ArticleModel
    {
        public class Article
        {
            public int ArticleID { get; set; }
            public string Title { get; set; }
            public string Content { get; set; }
            public DateTime PublishDate { get; set; }
            public DateTime? UnpublishDate { get; set; }
            public bool IsActive { get; set; }
        }

        public class ArticleClickCount
        {
            public int ArticleClickID { get; set; }
            public string ArticleID { get; set; }
            public int ClickCount { get; set; }
            public DateTime LastResetDate { get; set; }
        }
    }
}