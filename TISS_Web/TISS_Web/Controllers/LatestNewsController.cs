using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography.Xml;
using System.Web;
using System.Web.Mvc;
using TISS_Web.Models;
using TISS_Web.Utility;

namespace TISS_Web.Controllers
{
    public class LatestNewsController : Controller
    {
        private TISS_WebEntities _db = new TISS_WebEntities(); //資料庫

        #region 方法整合
        private List<ArticleContentModel> GetArticles(Func<ArticleContent, bool> predicate, int page, int pageSize)
        {
            var list = _db.ArticleContent
                .Where(predicate)
                .OrderByDescending(a => a.CreateDate)
                .ToList();

            ViewBag.TotalPages = (int)Math.Ceiling(list.Count / (double)pageSize);
            ViewBag.CurrentPage = page;

            return list.Skip((page - 1) * pageSize).Take(pageSize).Select(s => new ArticleContentModel
            {
                Title = s.Title,
                EncryptedUrl = UrlEncoderHelper.EncryptUrl(s.Title),
                ImageContent = s.ImageContent,
                Hashtags = s.Hashtags,
                FormattedCreateDate = (s.CreateDate ?? DateTime.MinValue).ToString("yyyy-MM-dd"),
                ContentType = _db.ArticleCategory.FirstOrDefault(c => c.Id == s.ContentTypeId)?.CategoryName
            }).ToList();
        }

        private ActionResult HandlePage(string title, Func<ArticleContent, bool> filter, int page, int pageSize, string viewName)
        {
            try
            {
                ViewBag.Title = title;
                Session["ReturnUrl"] = Request.Url?.ToString();
                page = Math.Max(1, page);

                var articles = GetArticles(filter, page, pageSize);
                return View(viewName, articles);
            }
            catch (Exception ex)
            {
                LogHelper.WriteAduioVideoLog(viewName, $"取得 {title} 失敗", ex);
                return RedirectToAction("Error404", "Error");
            }
        }
        #endregion

        #region 中心公告
        public ActionResult announcement(int page = 1, int pageSize = 9)
        {
            var hashtags = new List<string>
            {
                "新聞發佈", "中心訊息", "徵才招募", "國家運動科學中心",
                "委託研究計畫", "運動科學研究處", "MOU簽署", "行政管理人資組", "運動資訊"
            };
            return HandlePage("中心公告", a => hashtags.Contains(a.Hashtags) && a.IsEnabled, page, pageSize, "Announcement");
        }
        #endregion

        #region 新聞發佈
        public ActionResult press(int page = 1, int pageSize = 9)
        {
            return HandlePage("新聞發佈", a => a.ContentType == "新聞發佈" && a.IsEnabled, page, pageSize, "Press");
        }
        #endregion

        #region 中心訊息
        public ActionResult institute(int page = 1, int pageSize = 9)
        {
            return HandlePage("中心訊息", a => a.ContentType == "中心訊息" && a.IsEnabled, page, pageSize, "Institute");
        }
        #endregion

        #region 徵才招募
        public ActionResult recruit(int page = 1, int pageSize = 9)
        {
            return HandlePage("徵才招募", a => a.ContentType == "徵才招募" && a.IsEnabled, page, pageSize, "Recruit");
        }
        #endregion
    }
}