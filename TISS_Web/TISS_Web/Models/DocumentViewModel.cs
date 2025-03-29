using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace TISS_Web.Models
{
    public class DocumentViewModel
    {
        public int DocumentID { get; set; }
        public string DocumentName { get; set; }
        public string DocumentType { get; set; }
        public DateTime UploadTime { get; set; }
        public int FileSize { get; set; }
        public int? ArticleId { get; set; }
        public string Category { get; set; }
    }

    public class ArticleViewModel
    {
        public ArticleContent Article { get; set; }
        public List<DocumentViewModel> AssociatedDocuments { get; set; }
    }
}