using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Web;
using System.Web.Mvc;
using TISS_Web.Models;
using TISS_Web.Utility;

namespace TISS_Web.Controllers
{
    public class AduioVideoAreaController : Controller
    {
        private TISS_WebEntities _db = new TISS_WebEntities(); //資料庫

        #region 共用方法：文章查詢
        private List<ArticleContentModel> GetArticlesByHashtags(List<string> hashtags, int page, int pageSize, bool extractIframe = false)
        {
            var list = _db.ArticleContent
                .Where(a => hashtags.Contains(a.Hashtags) && a.IsEnabled)
                .OrderByDescending(a => a.CreateDate)
                .ToList();

            return list.Skip((page - 1) * pageSize).Take(pageSize).Select(s => new ArticleContentModel
            {
                Title = s.Title,
                EncryptedUrl = UrlEncoderHelper.EncryptUrl(s.Title),
                ImageContent = s.ImageContent,
                Hashtags = s.Hashtags,
                FormattedCreateDate = (s.CreateDate ?? DateTime.MinValue).ToString("yyyy-MM-dd"),
                ContentType = _db.ArticleCategory.FirstOrDefault(c => c.Id == s.ContentTypeId)?.CategoryName,
                VideoIframe = extractIframe ? ExtractIframe(s.ContentBody) : null
            }).ToList();
        }
        #endregion

        #region 共用方法：iframe 調整
        private string ExtractIframe(string content)
        {
            if (string.IsNullOrWhiteSpace(content)) return string.Empty;

            // 移除空白段落
            content = Regex.Replace(content, @"<p>(&nbsp;|\s*)<\/p>", string.Empty, RegexOptions.IgnoreCase);

            // 擷取 <iframe> 內容
            var match = Regex.Match(content, @"<iframe[^>]*src=\""([^\""]*)\""[^>]*><\/iframe>", RegexOptions.IgnoreCase);
            if (!match.Success) return string.Empty;

            var iframeHtml = match.Value;

            // 調整寬高為 100%
            iframeHtml = Regex.Replace(iframeHtml, @"width=\""(\d+)%?\""", "width=\"100%\"");
            iframeHtml = Regex.Replace(iframeHtml, @"height=\""(\d+)%?\""", "height=\"100%\"");

            return iframeHtml;
        }
        #endregion

        #region 共用方法：分頁資料設定
        private void SetPaging(int page, int totalItems, int pageSize)
        {
            int totalPages = (int)Math.Ceiling(totalItems / (double)pageSize);
            ViewBag.CurrentPage = page;
            ViewBag.TotalPages = totalPages;
        }
        #endregion

        #region 影音專區
        public ActionResult videoArea(int page = 1, int pageSize = 9)
        {
            try
            {
                ViewBag.Title = "影音專區";
                page = Math.Max(1, page);

                var hashtags = new List<string> { "人物專訪", "中心成果", "運動科技論壇", "影音專區", "運動科學研究處" };
                var list = _db.ArticleContent.Where(a => hashtags.Contains(a.Hashtags) && a.IsEnabled).ToList();

                SetPaging(page, list.Count, pageSize);
                var articles = GetArticlesByHashtags(hashtags, page, pageSize, extractIframe: true);

                ViewBag.Videos = articles;
                return View("videoArea", articles);
            }
            catch (Exception ex)
            {
                LogHelper.WriteAduioVideoLog("videoArea", "影音專區錯誤", ex);
                return RedirectToAction("Error404", "Error");
            }
        }
        #endregion

        #region 中心成果
        public ActionResult achievement(int page = 1, int pageSize = 9)
        {
            try
            {
                ViewBag.Title = "中心成果";
                Session["ReturnUrl"] = Request.Url.ToString();
                page = Math.Max(1, page);

                var hashtags = new List<string> { "中心成果", "人物專訪" };
                var list = _db.ArticleContent.Where(a => hashtags.Contains(a.Hashtags) && a.IsEnabled).ToList();

                SetPaging(page, list.Count, pageSize);
                var articles = GetArticlesByHashtags(hashtags, page, pageSize);

                return View("achievement", articles);
            }
            catch (Exception ex)
            {
                LogHelper.WriteAduioVideoLog("achievement", "中心成果錯誤", ex);
                return RedirectToAction("Error404", "Error");
            }
        }
        #endregion

        #region 新聞影音
        public ActionResult news(int page = 1, int pageSize = 9)
        {
            try
            {
                ViewBag.Title = "新聞影音";
                Session["ReturnUrl"] = Request.Url.ToString();
                page = Math.Max(1, page);

                var hashtags = new List<string> { "影音專區" };
                var list = _db.ArticleContent.Where(a => hashtags.Contains(a.Hashtags) && a.IsEnabled).ToList();

                SetPaging(page, list.Count, pageSize);
                var articles = GetArticlesByHashtags(hashtags, page, pageSize);

                return View("news", articles);
            }
            catch (Exception ex)
            {
                LogHelper.WriteAduioVideoLog("news", "新聞影音錯誤", ex);
                return RedirectToAction("Error404", "Error");
            }
        }
        #endregion

        #region 活動紀錄
        public ActionResult activityRecord(int page = 1, int pageSize = 9)
        {
            try
            {
                ViewBag.Title = "活動紀錄";
                Session["ReturnUrl"] = Request.Url.ToString();
                page = Math.Max(1, page);

                var hashtags = new List<string> { "運動科技論壇" };
                var list = _db.ArticleContent.Where(a => hashtags.Contains(a.Hashtags) && a.IsEnabled).ToList();

                SetPaging(page, list.Count, pageSize);
                var articles = GetArticlesByHashtags(hashtags, page, pageSize);

                return View("activityRecord", articles);
            }
            catch (Exception ex)
            {
                LogHelper.WriteAduioVideoLog("activityRecord", "活動紀錄錯誤", ex);
                return RedirectToAction("Error404", "Error");
            }
        }
        #endregion
    }
}