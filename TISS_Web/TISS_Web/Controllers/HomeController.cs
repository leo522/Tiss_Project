using Google.Apis.Services;
using Google.Apis.YouTube.v3;
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
            Session["ReturnUrl"] = Request.Url.ToString();

            var dtos = _db.ArticleContent
                .Where(a => a.IsPublished.HasValue && a.IsPublished.Value)
                .OrderByDescending(a => a.PublishedDate)
                .Select(a => new ArticleContentModel
                {
                    Title = a.Title,
                    ImageContent = a.ImageContent,
                    ContentType = a.ContentType,
                    Hashtags = a.Hashtags,
                    EncryptedUrl = a.EncryptedUrl,
                }).Take(4).ToList();
            return View(dtos);
        }

        public ActionResult GetArticles(int contentTypeId)
        {
            var dtos = _db.ArticleContent
                .Where(a => a.IsPublished.HasValue && a.IsPublished.Value && a.ContentTypeId == contentTypeId)
                .OrderByDescending(a => a.PublishedDate)
                .Select(a => new ArticleContentModel
                {
                    Title = a.Title,
                    ImageContent = a.ImageContent,
                    ContentType = a.ContentType,
                    Hashtags = a.Hashtags,
                    EncryptedUrl = a.EncryptedUrl,
                }).Take(4).ToList();

            return PartialView("_ArticleListPartial", dtos);
        }

        public ActionResult ViewArticle(string encryptedUrl)
        {
            try
            {
                //Session["ReturnUrl"] = Request.Url.ToString();

                var article = _db.ArticleContent.FirstOrDefault(a => a.EncryptedUrl == encryptedUrl);
                if (article == null)
                {
                    return HttpNotFound();
                }

                article.ClickCount += 1; //增加點閱率次數

                // 查找同一標籤下的上一篇和下一篇文章
                var articlesWithSameTag = _db.ArticleContent
                    .Where(a => a.Hashtags == article.Hashtags)
                    .OrderBy(a => a.PublishedDate)
                    .ToList();

                int currentIndex = articlesWithSameTag.FindIndex(a => a.Id == article.Id);

                // 找到上一篇和下一篇
                var previousArticle = currentIndex > 0 ? articlesWithSameTag[currentIndex - 1] : null;
                var nextArticle = currentIndex < articlesWithSameTag.Count - 1 ? articlesWithSameTag[currentIndex + 1] : null;

                ViewBag.PreviousArticle = previousArticle;
                ViewBag.NextArticle = nextArticle;

                // 字典來管理父目錄及其子目錄
                var parentDirectories = new Dictionary<string, List<string>>
                {
                    { "科普專欄", new List<string> { "運動醫學", "運動科技", "運動科學", "運動生理", "運動心理", "體能訓練", "運動營養" } },
                    { "最新消息", new List<string> { "中心成果", "新聞發佈", "活動紀錄","影音專區","中心訊息","國家運動科學中心", "徵才招募", "運動資訊" , "行政管理人資組", "MOU簽署", "人物專訪","運動科技論壇",} },
                };
                var currentSubDirectory = article.Hashtags; //文章的子目錄可以通過 ContentType 獲得
                var parentDirectory = parentDirectories.FirstOrDefault(pd => pd.Value.Contains(currentSubDirectory)).Key;
                ViewBag.ParentDirectory = parentDirectory;

                _db.SaveChanges();

                return View(article);
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [ValidateInput(false)]
        public ActionResult ViewArticle(ArticleContent dto)
        {
            try
            {
                if (ModelState.IsValid)
                {
                    var exist = _db.ArticleContent.Find(dto.Id);

                    if (exist != null)
                    {
                        exist.ContentBody = dto.ContentBody; //文章內容
                        exist.UpdatedDate = DateTime.Now; //更新時間
                        exist.UpdatedUser = Session["UserName"] as string; //更新人員

                        _db.SaveChanges();

                        return RedirectToAction("ViewArticle", new { encryptedUrl = exist.EncryptedUrl });
                    }
                }
                return View(dto);
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

        public List<string> GetFile() 
        {
            var dto =(from f in _db.FileDocument select f.DocumentName).ToList();

            var dtos = dto.Select(f => System.IO.Path.GetFileNameWithoutExtension(f)).ToList();
            return dtos;
        }

        private readonly string _apiKey = "AIzaSyCHWwoGD3o2uuHOQp4ejbi9wZ7yuDfLOQg";
        public ActionResult ytVideo() 
        {
            var youtubeService = new YouTubeService(new BaseClientService.Initializer
            {
                ApiKey = _apiKey,
                ApplicationName = "Your App Name"
            });

            var channelsListRequest = youtubeService.Channels.List("snippet,contentDetails,statistics");
            channelsListRequest.Id = "UCfpGsfNSwowlOk3eiJeHSWA"; // 替換成您要取得的頻道ID
            var channelResponse = channelsListRequest.Execute();

            var playlistItemsListRequest = youtubeService.PlaylistItems.List("snippet,contentDetails");
            playlistItemsListRequest.PlaylistId = channelResponse.Items[0].ContentDetails.RelatedPlaylists.Uploads;
            playlistItemsListRequest.MaxResults = 20; // 設定要取得的影片數量
            var playlistItemsResponse = playlistItemsListRequest.Execute();

            return View(playlistItemsResponse.Items);
        }
    }
}