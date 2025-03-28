using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Web;
using System.Web.Mvc;
using TISS_Web.Models;

namespace TISS_Web.Controllers
{
    public class AduioVideoAreaController : Controller
    {
        private TISS_WebEntities _db = new TISS_WebEntities(); //資料庫

        #region 處理DB影片的尺寸
        private string ExtractIframe(string content)
        {
            try
            {
                // 過濾空白段落，防止它們影響顯示
                content = Regex.Replace(content, @"<p>(&nbsp;|\s*)<\/p>", string.Empty, RegexOptions.IgnoreCase);

                var regex = new Regex(@"<iframe[^>]*src=""([^""]*)""[^>]*><\/iframe>"); //正則表達式提取 iframe
                var match = regex.Match(content);

                if (match.Success)
                {
                    var iframeHtml = match.Value;

                    // 替換原始的 width 和 height 屬性
                    iframeHtml = Regex.Replace(iframeHtml, @"width=""\d+""", "width=\"100%\"");
                    iframeHtml = Regex.Replace(iframeHtml, @"height=""\d+""", "height=\"100%\"");
                    return iframeHtml;
                }

                return string.Empty;
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }
        #endregion

        private string EncryptUrl(string title)
        {
            try
            {
                var bytes = System.Text.Encoding.UTF8.GetBytes(title);
                var base64string = Convert.ToBase64String(bytes);
                return base64string.Replace("/", "_").Replace("+", "-"); // 保持標準 Base64 URL Safe 編碼
            }
            catch (Exception)
            {
                return string.Empty;
            }
        }

        #region 影音專區
        public ActionResult videoArea(int page = 1, int pageSize = 9)
        {
            try
            {
                ViewBag.Title = "影音專區";
                page = Math.Max(1, page); //確保頁碼至少為 1

                var relatedHashtags = new List<string>
            {
                "人物專訪",
                "中心成果",
                "運動科技論壇",
                "影音專區",
                "運動科學研究處",
                //"運動科技",
            };
                // 查詢相關 hashtags 的文章
                var list = _db.ArticleContent
                    .Where(a => relatedHashtags.Contains(a.Hashtags) && a.IsEnabled)
                    .OrderByDescending(a => a.CreateDate)
                    .ToList();

                //計算總數和總頁數
                var totalArticles = list.Count();
                var totalPages = (int)Math.Ceiling(totalArticles / (double)pageSize);

                page = Math.Min(page, totalPages); //確保頁碼不超過最大頁數

                var articles = list.Skip((page - 1) * pageSize).Take(pageSize).Select(s => new ArticleContentModel
                {
                    Title = s.Title,
                    EncryptedUrl = EncryptUrl(s.Title),
                    ImageContent = s.ImageContent,
                    Hashtags = s.Hashtags,
                    FormattedCreateDate = (s.CreateDate ?? DateTime.MinValue).ToString("yyyy-MM-dd"),
                    ContentType = _db.ArticleCategory.FirstOrDefault(c => c.Id == s.ContentTypeId)?.CategoryName,
                    VideoIframe = ExtractIframe(s.ContentBody),
                }).ToList();

                ViewBag.CurrentPage = page;
                ViewBag.TotalPages = totalPages;
                ViewBag.Videos = articles;

                return View(articles);
            }
            catch (Exception)
            {
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

                page = Math.Max(1, page); //確保頁碼至少為 1

                var list = _db.ArticleContent
                    .Where(a => a.Hashtags == "中心成果" || a.Hashtags == "人物專訪" && a.IsEnabled)
                    .OrderByDescending(a => a.CreateDate)
                    .ToList();

                //計算總數和總頁數
                var totalArticles = list.Count();
                var totalPages = (int)Math.Ceiling(totalArticles / (double)pageSize);

                page = Math.Min(page, totalPages); //確保頁碼不超過最大頁數

                var articles = list.Skip((page - 1) * pageSize).Take(pageSize).Select(s => new ArticleContentModel
                {
                    Title = s.Title,
                    EncryptedUrl = EncryptUrl(s.Title),
                    ImageContent = s.ImageContent,
                    Hashtags = s.Hashtags,
                    FormattedCreateDate = (s.CreateDate ?? DateTime.MinValue).ToString("yyyy-MM-dd"),
                    ContentType = _db.ArticleCategory.FirstOrDefault(c => c.Id == s.ContentTypeId)?.CategoryName
                }).ToList();

                ViewBag.CurrentPage = page;
                ViewBag.TotalPages = totalPages;

                return View(articles);
            }
            catch (Exception)
            {
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

                page = Math.Max(1, page); //確保頁碼至少為 1

                var list = _db.ArticleContent
                    .Where(a => a.Hashtags == "影音專區" && a.IsEnabled)
                    .OrderByDescending(a => a.CreateDate)
                    .ToList();

                //計算總數和總頁數
                var totalArticles = list.Count();
                var totalPages = (int)Math.Ceiling(totalArticles / (double)pageSize);

                page = Math.Min(page, totalPages); //確保頁碼不超過最大頁數

                var articles = list.Skip((page - 1) * pageSize).Take(pageSize).Select(s => new ArticleContentModel
                {
                    Title = s.Title,
                    EncryptedUrl = EncryptUrl(s.Title),
                    ImageContent = s.ImageContent,
                    Hashtags = s.Hashtags,
                    FormattedCreateDate = (s.CreateDate ?? DateTime.MinValue).ToString("yyyy-MM-dd"),
                    ContentType = _db.ArticleCategory.FirstOrDefault(c => c.Id == s.ContentTypeId)?.CategoryName
                }).ToList();

                ViewBag.CurrentPage = page;
                ViewBag.TotalPages = totalPages;

                return View(articles);
            }
            catch (Exception)
            {
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

                page = Math.Max(1, page); //確保頁碼至少為 1

                var list = _db.ArticleContent
                    .Where(a => a.Hashtags == "運動科技論壇" && a.IsEnabled)
                    .OrderByDescending(a => a.CreateDate)
                    .ToList();

                //計算總數和總頁數
                var totalArticles = list.Count();
                var totalPages = (int)Math.Ceiling(totalArticles / (double)pageSize);

                page = Math.Min(page, totalPages); //確保頁碼不超過最大頁數

                var articles = list.Skip((page - 1) * pageSize).Take(pageSize).Select(s => new ArticleContentModel
                {
                    Title = s.Title,
                    EncryptedUrl = EncryptUrl(s.Title),
                    ImageContent = s.ImageContent,
                    Hashtags = s.Hashtags,
                    FormattedCreateDate = (s.CreateDate ?? DateTime.MinValue).ToString("yyyy-MM-dd"),
                    ContentType = _db.ArticleCategory.FirstOrDefault(c => c.Id == s.ContentTypeId)?.CategoryName
                }).ToList();

                ViewBag.CurrentPage = page;
                ViewBag.TotalPages = totalPages;

                return View(articles);
            }
            catch (Exception)
            {
                return RedirectToAction("Error404", "Error");
            }
        }
        #endregion
    }
}