using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Web;
using System.Web.Mvc;
using System.Web.Script.Serialization;
using System.Web.WebPages;
using TISS_Web.Models;

namespace TISS_Web.Controllers
{
    public class HomeController : Controller
    {
        private TISS_WebEntities _db = new TISS_WebEntities();

        public ActionResult Index()
        {
            return View();
        }

        public ActionResult About()
        {
            ViewBag.Message = "Your application description page.";

            return View();
        }

        public ActionResult Contact()
        {
            ViewBag.Message = "Your contact page.";

            return View();
        }

        public ActionResult OnInit(int? page, int? pageSize)
        {
            int pageNumber = page ?? 1;
            int size = pageSize ?? 6; // 每頁顯示的資料筆數

            var dto = _db.AnnouncementPageContent
                   .OrderByDescending(x => x.FileUploadTime)
                   .Skip((pageNumber - 1) * size)
                   .Take(size)
                   .Select(x => new AnnouncementPageContentModel
                   {
                       ID = x.ID,
                       TextContent = x.TextContent,
                       ImageContent = x.ImageContent,
                       //FileNo = x.FileNo.GetValueOrDefault(),
                   })
                   .ToList();

            // 計算總頁數，設置 ViewBag 用於視圖中顯示分頁
            int totalItemCount = _db.AnnouncementPageContent.Count();
            int totalPages = (int)Math.Ceiling((double)totalItemCount / size);

            ViewBag.CurrentPage = pageNumber;
            ViewBag.TotalPages = totalPages;
            ViewBag.PageSize = size;

            return View(dto);
        }

        public ActionResult Navbar() 
        {
            return View();
        }
    }
}