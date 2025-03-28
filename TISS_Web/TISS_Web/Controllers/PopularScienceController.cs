using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using TISS_Web.Models;
using TISS_Web.Utility;

namespace TISS_Web.Controllers
{
    public class PopularScienceController : Controller
    {
        private TISS_WebEntities _db = new TISS_WebEntities(); //資料庫

        #region 科普專欄
        public ActionResult research(int page = 1, int pageSize = 9)
        {
            try
            {
                ViewBag.Title = "科普專欄";
                Session["ReturnUrl"] = Request.Url.ToString();

                page = Math.Max(1, page); // 確保頁碼至少為 1

                var relatedHashtags = new List<string>
            {
                "運動醫學",
                "運動科技",
                "運動科學",
                "運動生理",
                "運動心理",
                "體能訓練",
                "運動營養",
                "兒少科普",
                "科普海報下載專區"
            };

                // 查詢相關 hashtags 的文章
                var list = _db.ArticleContent
                    .Where(a => a.IsEnabled)
                    .ToList()
                    .Where(a => relatedHashtags.Any(tag => a.Hashtags.Split(',').Contains(tag)))
                    .OrderByDescending(a => a.CreateDate)
                    .ToList();

                // 計算總數和總頁數
                var totalArticles = list.Count();
                var totalPages = (int)Math.Ceiling(totalArticles / (double)pageSize);

                page = Math.Min(page, totalPages); // 確保頁碼不超過最大頁數

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

        #region 運動科學
        public ActionResult sportScience(int page = 1, int pageSize = 9)
        {
            try
            {
                ViewBag.Title = "運動科學";
                Session["ReturnUrl"] = Request.Url.ToString();

                page = Math.Max(1, page); //確保頁碼至少為 1

                // 修改篩選邏輯以處理多個 Hashtags
                var targetHashtags = new List<string> { "運動科學", "運動管理" };

                var list = _db.ArticleContent
                    .Where(a => a.IsEnabled && a.Hashtags != null)
                    .AsEnumerable() // 將資料拉至記憶體中處理
                    .Where(a => a.Hashtags.Split(',').Select(tag => tag.Trim()).Any(tag => targetHashtags.Contains(tag)))
                    .OrderByDescending(a => a.CreateDate)
                    .ToList();


                //計算總數和總頁數
                var totalArticles = list.Count();
                var totalPages = (int)Math.Ceiling(totalArticles / (double)pageSize);

                page = Math.Min(page, totalPages); //確保頁碼不超過最大頁數

                // 分頁處理
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

        #region 運動科技
        public ActionResult sportTech(int page = 1, int pageSize = 9)
        {
            try
            {
                ViewBag.Title = "運動科技";
                Session["ReturnUrl"] = Request.Url.ToString();

                page = Math.Max(1, page); //確保頁碼至少為 1

                var list = _db.ArticleContent
                    .Where(a => a.Hashtags == "運動科技" && a.IsEnabled)
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

        #region 運動醫學
        public ActionResult sportMedicine(int page = 1, int pageSize = 9)
        {
            try
            {
                ViewBag.Title = "運動醫學";
                Session["ReturnUrl"] = Request.Url.ToString();

                page = Math.Max(1, page); //確保頁碼至少為 1

                var list = _db.ArticleContent
                    .Where(a => a.Hashtags == "運動醫學" && a.IsEnabled)
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

        #region 運動生理
        public ActionResult sportsPhysiology(int page = 1, int pageSize = 9)
        {
            try
            {
                ViewBag.Title = "運動生理";
                Session["ReturnUrl"] = Request.Url.ToString();

                page = Math.Max(1, page); //確保頁碼至少為 1

                var list = _db.ArticleContent.
                    Where(a => a.Hashtags == "運動生理" && a.IsEnabled)
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

        #region 運動心理
        public ActionResult sportsPsychology(int page = 1, int pageSize = 9)
        {
            try
            {
                ViewBag.Title = "運動心理";
                Session["ReturnUrl"] = Request.Url.ToString();

                page = Math.Max(1, page); //確保頁碼至少為 1

                var list = _db.ArticleContent
                    .Where(a => a.Hashtags == "運動心理" && a.IsEnabled)
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

        #region 體能訓練
        public ActionResult physicalTraining(int page = 1, int pageSize = 9)
        {
            try
            {
                ViewBag.Title = "體能訓練";
                Session["ReturnUrl"] = Request.Url.ToString();

                page = Math.Max(1, page); //確保頁碼至少為 1

                var list = _db.ArticleContent
                    .Where(a => a.Hashtags == "體能訓練" && a.IsEnabled)
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

        #region 運動營養
        public ActionResult sportsNutrition(int page = 1, int pageSize = 9)
        {
            try
            {
                ViewBag.Title = "運動營養";
                Session["ReturnUrl"] = Request.Url.ToString();

                page = Math.Max(1, page); //確保頁碼至少為 1

                var list = _db.ArticleContent
                    .Where(a => a.Hashtags == "運動營養" && a.IsEnabled)
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

        #region 兒少科普
        public ActionResult childrenScience(int page = 1, int pageSize = 9)
        {
            try
            {
                ViewBag.Title = "兒少科普";
                Session["ReturnUrl"] = Request.Url.ToString();

                page = Math.Max(1, page); //確保頁碼至少為 1

                // 修改篩選邏輯以處理多個 Hashtags
                var list = _db.ArticleContent
                    .Where(a => a.IsEnabled && a.Hashtags != null)
                    .AsEnumerable() // 將資料拉至記憶體中處理
                    .Where(a => a.Hashtags.Split(',').Select(tag => tag.Trim()).Contains("兒少科普")) // 檢查是否包含目標標籤
                    .OrderByDescending(a => a.CreateDate)
                    .ToList();

                //計算總數和總頁數
                var totalArticles = list.Count();
                var totalPages = (int)Math.Ceiling(totalArticles / (double)pageSize);

                page = Math.Min(page, totalPages); //確保頁碼不超過最大頁數

                // 分頁處理
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

        #region 科普海報下載專區
        public ActionResult SciencePosterDownLoad(int page = 1, int pageSize = 9)
        {
            try
            {
                ViewBag.Title = "科普海報下載專區";
                Session["ReturnUrl"] = Request.Url.ToString();

                page = Math.Max(1, page); //確保頁碼至少為 1

                var list = _db.ArticleContent
                    .Where(a => a.Hashtags.Contains("科普海報下載專區") && a.IsEnabled)
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