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

        #region 中心公告
        public ActionResult announcement(int page = 1, int pageSize = 9)
        {
            try
            {
                ViewBag.Title = "中心公告";
                Session["ReturnUrl"] = Request.Url.ToString();

                page = Math.Max(1, page); //確保頁碼至少為 1

                var relatedHashtags = new List<string>
            {
                "新聞發佈",
                "中心訊息",
                "徵才招募",
                "國家運動科學中心",
                "委託研究計畫",
                "運動科學研究處",
                "MOU簽署",
                "行政管理人資組",
                "運動資訊"
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
                    EncryptedUrl = UrlEncoderHelper.EncryptUrl(s.Title),
                    ImageContent = s.ImageContent,
                    Hashtags = s.Hashtags,
                    FormattedCreateDate = (s.CreateDate ?? DateTime.MinValue).ToString("yyyy-MM-dd"),
                    ContentType = _db.ArticleCategory.FirstOrDefault(c => c.Id == s.ContentTypeId)?.CategoryName
                }).ToList();

                ViewBag.CurrentPage = page;
                ViewBag.TotalPages = totalPages;

                return View(articles);
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }
        #endregion

        #region 新聞發布
        public ActionResult press(int page = 1, int pageSize = 9)
        {
            try
            {
                ViewBag.Title = "新聞發佈";
                Session["ReturnUrl"] = Request.Url.ToString();

                page = Math.Max(1, page); //確保頁碼至少為 1

                var list = _db.ArticleContent
                    .Where(a => a.ContentType == "新聞發佈" && a.IsEnabled)
                    .OrderByDescending(a => a.CreateDate)
                    .ToList();

                //計算總數和總頁數
                var totalArticles = list.Count();
                var totalPages = (int)Math.Ceiling(totalArticles / (double)pageSize);

                page = Math.Min(page, totalPages); //確保頁碼不超過最大頁數

                var articles = list.Skip((page - 1) * pageSize).Take(pageSize).Select(s => new ArticleContentModel
                {
                    Title = s.Title,
                    EncryptedUrl = UrlEncoderHelper.EncryptUrl(s.Title),
                    ImageContent = s.ImageContent,
                    Hashtags = s.Hashtags,
                    FormattedCreateDate = (s.CreateDate ?? DateTime.MinValue).ToString("yyyy-MM-dd"),
                    ContentType = _db.ArticleCategory.FirstOrDefault(c => c.Id == s.ContentTypeId)?.CategoryName
                }).ToList();

                ViewBag.CurrentPage = page;
                ViewBag.TotalPages = totalPages;

                return View(articles);
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }
        #endregion

        #region 中心訊息       
        public ActionResult institute(int page = 1, int pageSize = 9)
        {
            try
            {
                ViewBag.Title = "中心訊息";
                Session["ReturnUrl"] = Request.Url.ToString();

                page = Math.Max(1, page); //確保頁碼至少為 1

                var list = _db.ArticleContent
                    .Where(a => a.ContentType == "中心訊息" && a.IsEnabled)
                    .OrderByDescending(a => a.CreateDate)
                    .ToList();

                //計算總數和總頁數
                var totalArticles = list.Count();
                var totalPages = (int)Math.Ceiling(totalArticles / (double)pageSize);

                page = Math.Min(page, totalPages); //確保頁碼不超過最大頁數

                var articles = list.Skip((page - 1) * pageSize).Take(pageSize).Select(s => new ArticleContentModel
                {
                    Title = s.Title,
                    EncryptedUrl = UrlEncoderHelper.EncryptUrl(s.Title),
                    ImageContent = s.ImageContent,
                    Hashtags = s.Hashtags,
                    FormattedCreateDate = (s.CreateDate ?? DateTime.MinValue).ToString("yyyy-MM-dd"),
                    ContentType = _db.ArticleCategory.FirstOrDefault(c => c.Id == s.ContentTypeId)?.CategoryName
                }).ToList();

                ViewBag.CurrentPage = page;
                ViewBag.TotalPages = totalPages;

                return View(articles);
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }
        #endregion

        #region 徵才招募
        public ActionResult recruit(int page = 1, int pageSize = 9)
        {
            try
            {
                ViewBag.Title = "徵才招募";
                Session["ReturnUrl"] = Request.Url.ToString();

                page = Math.Max(1, page); //確保頁碼至少為 1

                var list = _db.ArticleContent
                    .Where(a => a.ContentType == "徵才招募" && a.IsEnabled)
                    .OrderByDescending(a => a.CreateDate)
                    .ToList();

                //計算總數和總頁數
                var totalArticles = list.Count();
                var totalPages = (int)Math.Ceiling(totalArticles / (double)pageSize);

                page = Math.Min(page, totalPages); //確保頁碼不超過最大頁數

                var articles = list.Skip((page - 1) * pageSize).Take(pageSize).Select(s => new ArticleContentModel
                {
                    Title = s.Title,
                    EncryptedUrl = UrlEncoderHelper.EncryptUrl(s.Title),
                    ImageContent = s.ImageContent,
                    Hashtags = s.Hashtags,
                    FormattedCreateDate = (s.CreateDate ?? DateTime.MinValue).ToString("yyyy-MM-dd"),
                    ContentType = _db.ArticleCategory.FirstOrDefault(c => c.Id == s.ContentTypeId)?.CategoryName
                }).ToList();

                ViewBag.CurrentPage = page;
                ViewBag.TotalPages = totalPages;

                return View(articles);
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }
        #endregion
    }
}