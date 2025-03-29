using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text.RegularExpressions;
using System.Web;
using System.Web.Configuration;
using System.Web.Mvc;
using System.Web.Security;
using TISS_Web.Models;
using System.Net.NetworkInformation;
using System.Web.UI.WebControls;
using System.Security.Cryptography;
using System.Text;
using PagedList;
using System.Web.UI;
using System.Data.Entity;
using static TISS_Web.Models.ArticleModel;
using System.Collections;
using System.Net.Mime;
using Google.Apis.Services;
using Google.Apis.YouTube.v3;
using System.Threading.Tasks;
using System.Net.Mail;
using System.Data.Entity.Validation;
using Google.Apis.YouTube.v3.Data;
using Newtonsoft.Json;
using OfficeOpenXml;
using System.Windows.Documents;
using System.Net.Http;
using System.Net.Sockets;
using System.Web.Caching;
using System.Runtime.Caching;
using System.Xml.Linq;
using Microsoft.Ajax.Utilities;
using iText.Kernel.Pdf;
using iText.Kernel.XMP;
using iText.Kernel.XMP.Options;
using iText.Kernel.XMP.Properties;

namespace TISS_Web.Controllers
{
    public class TissController : Controller
    {
        private TISS_WebEntities _db = new TISS_WebEntities(); //資料庫
        private readonly string _apiKey = "AIzaSyCHWwoGD3o2uuHOQp4ejbi9wZ7yuDfLOQg"; //yt Data API KEY

        #region 首頁
        public ActionResult Home()
        {
            try
            {
                Session["ReturnUrl"] = Request.Url.ToString();

                // **合併查詢**
                var articles = _db.ArticleContent
                    .Where(a => a.IsPublished == true && a.IsEnabled == true)
                    .OrderByDescending(a => a.PublishedDate)
                    .Select(a => new ArticleContentModel
                    {
                        Title = a.Title,
                        ImageContent = a.ImageContent,
                        ContentType = a.ContentType,
                        Hashtags = a.Hashtags,
                        EncryptedUrl = a.EncryptedUrl,
                        PublishedDate = a.PublishedDate ?? DateTime.MinValue,
                    }).Take(5).ToList();

                ////首頁文章內容
                //var dtos = _db.ArticleContent
                //    .Where(a => a.IsPublished.HasValue && a.IsPublished.Value && a.IsEnabled == true)
                //    .OrderByDescending(a => a.PublishedDate)
                //    .Select(a => new ArticleContentModel
                //    {
                //        Title = a.Title,
                //        ImageContent = a.ImageContent,
                //        ContentType = a.ContentType,
                //        Hashtags = a.Hashtags,
                //        EncryptedUrl = a.EncryptedUrl,
                //        PublishedDate = a.PublishedDate.HasValue ? a.PublishedDate.Value : DateTime.MinValue,

                //    }).Take(5).ToList();

                //var dto = _db.ArticleContent
                //        .Where(a =>  a.IsPublished.HasValue && a.IsPublished.Value && a.IsEnabled == true)
                //        .OrderByDescending(a => a.CreateDate)
                //        .Select(a => new ArticleContentModel
                //        {
                //            Title = a.Title,
                //            ImageContent = a.ImageContent,
                //            ContentType = a.ContentType,
                //            Hashtags = a.Hashtags,
                //            EncryptedUrl = a.EncryptedUrl,
                //            PublishedDate = a.PublishedDate.HasValue ? a.PublishedDate.Value : DateTime.MinValue,
                //        })
                //        .FirstOrDefault(); // 取得最新的專欄文章

                //var latestArticle = dtos.FirstOrDefault();
                //var otherArticles = dtos.Skip(1).ToList();

                //var viewModel = new HomeViewModel //首頁的部份視圖
                //{
                //    LatestArticle = dto,
                //    OtherArticles = otherArticles,
                //    Videos = null // 不需要再從這裡獲取影片數據
                //};
                var viewModel = new HomeViewModel
                {
                    LatestArticle = articles.FirstOrDefault(),  // **避免額外查詢**
                    OtherArticles = articles.Skip(1).ToList(),
                    Videos = null  // 影片部分可改 AJAX 動態載入
                };
                return View(viewModel);
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

        //首頁Partial View 最新消息清單使用
        public ActionResult GetArticles(int? contentTypeId)
        {
            try
            {
                var query = _db.ArticleContent
                .Where(a => a.IsPublished.HasValue && a.IsPublished.Value && a.IsEnabled == true);

                if (contentTypeId.HasValue && contentTypeId.Value > 0)
                {
                    query = query.Where(a => a.ContentTypeId == contentTypeId.Value);
                }

                var dtos = query
                    .OrderByDescending(a => a.PublishedDate)
                    .Select(a => new ArticleContentModel
                    {
                        Title = a.Title,
                        ImageContent = a.ImageContent,
                        ContentType = a.ContentType,
                        Hashtags = a.Hashtags,
                        EncryptedUrl = a.EncryptedUrl,
                        PublishedDate = a.PublishedDate.HasValue ? a.PublishedDate.Value : DateTime.MinValue,
                    }).Take(5).ToList();

                return PartialView("_ArticleListPartial", dtos);

            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

        //處理DB影片的尺寸
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

        #region 網站導覽
        public ActionResult SiteMap()
        {
            ViewBag.Title = "網站導覽";
            return View();
        }
        #endregion

        #region 獲取首頁影片區塊的資料
        public ActionResult GetHomeVideos()
        {
            try
            {
                var relatedHashtags = new List<string>
                {
                    "人物專訪","中心成果","運動科技論壇", "影音專區"
                };

                // 先查詢文章內容
                var videoArticles = _db.ArticleContent
                    .Where(a => relatedHashtags.Contains(a.Hashtags) && a.IsEnabled)
                    .ToList();

                // 查詢所有類別
                var articleCategories = _db.ArticleCategory.ToList();

                // 提取影片中的 iframe 並賦值 ContentType
                var videos = videoArticles.Select(a => new ArticleContentModel
                {
                    Title = a.Title,
                    VideoIframe = ExtractIframe(a.ContentBody),
                    ImageContent = a.ImageContent,
                    Hashtags = a.Hashtags,
                    PublishedDate = a.PublishedDate.GetValueOrDefault(DateTime.MinValue),
                    ContentType = articleCategories.FirstOrDefault(c => c.Id == a.ContentTypeId)?.CategoryName
                })
                .Where(a => !string.IsNullOrEmpty(a.VideoIframe))
                .ToList();

                return PartialView("_VideoSection", videos);
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }
        #endregion

    }
}