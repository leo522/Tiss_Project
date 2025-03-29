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

        #region 共用方法：取得文章清單
        private ActionResult LoadArticles(string title, List<string> hashtags, string viewName, int page, int pageSize)
        {
            try
            {
                ViewBag.Title = title;
                Session["ReturnUrl"] = Request.Url.ToString();
                page = Math.Max(1, page);

                var categoryMap = _db.ArticleCategory.ToDictionary(c => c.Id, c => c.CategoryName);

                var list = _db.ArticleContent
                    .Where(a => a.IsEnabled && a.Hashtags != null)
                    .AsEnumerable()
                    .Where(a => a.Hashtags.Split(',').Select(tag => tag.Trim()).Any(tag => hashtags.Contains(tag)))
                    .OrderByDescending(a => a.CreateDate)
                    .ToList();

                int totalArticles = list.Count();
                int totalPages = (int)Math.Ceiling(totalArticles / (double)pageSize);
                page = Math.Min(page, totalPages);

                var articles = list.Skip((page - 1) * pageSize).Take(pageSize).Select(s => new ArticleContentModel
                {
                    Title = s.Title,
                    EncryptedUrl = UrlEncoderHelper.EncryptUrl(s.Title),
                    ImageContent = s.ImageContent,
                    Hashtags = s.Hashtags,
                    FormattedCreateDate = (s.CreateDate ?? DateTime.MinValue).ToString("yyyy-MM-dd"),
                    ContentType = categoryMap.ContainsKey(s.ContentTypeId ?? 0) ? categoryMap[s.ContentTypeId ?? 0] : null
                }).ToList();

                ViewBag.CurrentPage = page;
                ViewBag.TotalPages = totalPages;

                return View(viewName, articles);
            }
            catch (Exception ex)
            {
                LogHelper.WritePopularScienceLog("載入科普文章錯誤", "載入科普文章錯誤", ex);
                return RedirectToAction("Error404", "Error");
            }
        }
        #endregion

        #region 科普專欄
        public ActionResult research(int page = 1, int pageSize = 9)
        {
            var tags = new List<string> { "運動醫學", "運動科技", "運動科學", "運動生理", "運動心理", "體能訓練", "運動營養", "兒少科普", "科普海報下載專區" };
            return LoadArticles("科普專欄", tags, "research", page, pageSize);
        }
        #endregion

        #region 運動科學
        public ActionResult sportScience(int page = 1, int pageSize = 9)
        {
            var tags = new List<string> { "運動科學", "運動管理" };
            return LoadArticles("運動科學", tags, "sportScience", page, pageSize);
        }
        #endregion

        #region 運動科技
        public ActionResult sportTech(int page = 1, int pageSize = 9)
        {
            var tags = new List<string> { "運動科技" };
            return LoadArticles("運動科技", tags, "sportTech", page, pageSize);
        }
        #endregion

        #region 運動醫學
        public ActionResult sportMedicine(int page = 1, int pageSize = 9)
        {
            var tags = new List<string> { "運動醫學" };
            return LoadArticles("運動醫學", tags, "sportMedicine", page, pageSize);
        }
        #endregion

        #region 運動生理
        public ActionResult sportsPhysiology(int page = 1, int pageSize = 9)
        {
            var tags = new List<string> { "運動生理" };
            return LoadArticles("運動生理", tags, "sportsPhysiology", page, pageSize);
        }
        #endregion

        #region 運動心理
        public ActionResult sportsPsychology(int page = 1, int pageSize = 9)
        {
            var tags = new List<string> { "運動心理" };
            return LoadArticles("運動心理", tags, "sportsPsychology", page, pageSize);
        }
        #endregion

        #region 體能訓練
        public ActionResult physicalTraining(int page = 1, int pageSize = 9)
        {
            var tags = new List<string> { "體能訓練" };
            return LoadArticles("體能訓練", tags, "physicalTraining", page, pageSize);
        }
        #endregion

        #region 運動營養
        public ActionResult sportsNutrition(int page = 1, int pageSize = 9)
        {
            var tags = new List<string> { "運動營養" };
            return LoadArticles("運動營養", tags, "sportsNutrition", page, pageSize);
        }
        #endregion

        #region 兒少科普
        public ActionResult childrenScience(int page = 1, int pageSize = 9)
        {
            var tags = new List<string> { "兒少科普" };
            return LoadArticles("兒少科普", tags, "childrenScience", page, pageSize);
        }
        #endregion

        #region 科普海報下載專區
        public ActionResult SciencePosterDownLoad(int page = 1, int pageSize = 9)
        {
            var tags = new List<string> { "科普海報下載專區" };
            return LoadArticles("科普海報下載專區", tags, "SciencePosterDownLoad", page, pageSize);
        }
        #endregion
    }
}