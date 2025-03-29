using iText.Kernel.Pdf;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Web;
using System.Web.Mvc;
using System.Web.Routing;
using TISS_Web.Models;
using TISS_Web.Utility;

namespace TISS_Web.Controllers
{
    public class ArticleController : Controller
    {
        private TISS_WebEntities _db = new TISS_WebEntities(); //資料庫

        #region 發佈文章
        public ActionResult ArticleCreate(int? id)
        {
            try
            {
                ArticleContent article = null;

                if (id.HasValue)
                {
                    article = _db.ArticleContent.FirstOrDefault(a => a.Id == id.Value);

                    if (article == null)
                    {
                        return HttpNotFound();
                    }
                }

                if (!ModelState.IsValid)
                {
                    foreach (var error in ModelState.Values.SelectMany(v => v.Errors))
                    {
                        Console.WriteLine(error.ErrorMessage);
                    }
                }

                ViewBag.Hashtags = new SelectList(_db.Hashtag.ToList(), "Name", "Name");
                ViewBag.Categories = new SelectList(_db.ArticleCategory.ToList(), "Id", "CategoryName");
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
        public ActionResult ArticleCreate(ArticleContent dto, HttpPostedFileBase imageFile, string[] tags, int contentTypeID, HttpPostedFileBase[] documentFiles, string documentCategory)
        {
            if (string.IsNullOrEmpty(documentCategory))
            {
                ModelState.AddModelError("documentCategory", "請選擇文件檔案分類。");
            }

            if (ModelState.IsValid)
            {
                try
                {
                    // 儲存封面圖片
                    if (imageFile != null && imageFile.ContentLength > 0)
                    {
                        using (var binaryReader = new System.IO.BinaryReader(imageFile.InputStream))
                        {
                            dto.ImageContent = binaryReader.ReadBytes(imageFile.ContentLength);
                        }
                    }

                    // 設定文章屬性
                    var userName = Session["UserName"] as string;
                    dto.CreateUser = userName;
                    dto.PublishedDate = DateTime.Now;
                    dto.EncryptedUrl = EncryptUrl(dto.Title);
                    dto.CreateDate = DateTime.Now;
                    dto.ClickCount = 0;
                    dto.Hashtags = string.Join(",", tags);
                    dto.IsEnabled = true;
                    dto.IsPublished = true;
                    dto.UpdatedDate = DateTime.Now;
                    dto.ExpireDate = Request["ExpireDate"] != null ? Convert.ToDateTime(Request["ExpireDate"]) : (DateTime?)null;
                    dto.UpdatedUser = userName;

                    //設定文章類型
                    var category = _db.ArticleCategory.FirstOrDefault(c => c.Id == contentTypeID);
                    if (category != null)
                    {
                        dto.ContentTypeId = category.Id;
                        dto.ContentType = category.CategoryName;
                    }

                    // 處理hashtags，多個標籤存為逗號分隔的字符串，儲存標籤
                    if (tags != null && tags.Length > 0)
                    {
                        dto.Hashtags = string.Join(",", tags);

                        foreach (var t in tags)
                        {
                            var existingHashtag = _db.Hashtag.FirstOrDefault(h => h.Name == t);

                            if (existingHashtag == null)
                            {
                                var newHashtag = new Hashtag { Name = t }; // 如果 hashtag 不存在，則新增
                                _db.Hashtag.Add(newHashtag);
                            }
                        }
                    }
                    _db.ArticleContent.Add(dto);
                    _db.SaveChanges();

                    // 6. 儲存文件至 documents 表並關聯文章ID
                    if (documentFiles != null && documentFiles.Length > 0)
                    {
                        var fileUploadResult = SaveDocumentFile(documentFiles, dto.Id, documentCategory);
                        if (!fileUploadResult)
                        {
                            ModelState.AddModelError("", "文件上傳失敗。");
                        }
                    }

                    if (documentFiles != null)
                    {
                        Console.WriteLine($"收到的檔案數量: {documentFiles.Length}");
                    }

                    var route = GetRedirectTarget(dto.ContentType);
                    return RedirectToAction(route.Item2, route.Item1);
                }
                catch (Exception)
                {
                    ModelState.AddModelError("", "發生錯誤，請稍後再試。");
                }
            }

            ViewBag.Hashtags = new SelectList(_db.Hashtag.ToList(), "Name", "Name");
            ViewBag.Categories = new SelectList(_db.ArticleCategory.ToList(), "Id", "CategoryName");

            return View(dto);
        }
        #endregion

        #region URL加密
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
        #endregion

        #region 文章內容正則表達式處理
        public string EnsureImageAltAttribute(string content)
        {
            try
            {
                // 使用正則表達式找到所有 img 標籤，並檢查是否有 alt 屬性
                var imgPattern = "<img(?![^>]*\\balt=)[^>]*>";
                var altAttributePattern = "<img([^>]*?)>";

                // 替換沒有 alt 的 img 標籤，添加具有描述性的 alt 屬性
                string updatedContent = Regex.Replace(content, imgPattern, match =>
                {
                    // 根據實際需求決定alt的內容
                    return Regex.Replace(match.Value, altAttributePattern, "<img$1 alt=\"圖片描述\">");
                });

                return updatedContent;
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }
        #endregion

        #region 文章內容URL解密
        private string DecryptUrl(string encryptedUrl)
        {
            try
            {
                encryptedUrl = encryptedUrl.Replace("-", "+").Replace("_", "/");

                int mod4 = encryptedUrl.Length % 4; // Base64 字串長度補齊
                if (mod4 > 0)
                {
                    encryptedUrl += new string('=', 4 - mod4);
                }

                var bytes = Convert.FromBase64String(encryptedUrl); // Base64 解碼
                return System.Text.Encoding.UTF8.GetString(bytes);
            }
            catch (FormatException ex)
            {
                Console.WriteLine($"[ERROR] Base64 解碼失敗: {ex.Message}");
                return string.Empty;
            }
        }
        #endregion

        #region 文章內容附件檔案名稱顯示
        public string AddAccessibilityAttributes(string content)
        {
            try
            {
                // 添加 `title` 和 `aria-label` 到所有的附件連結（如 .pdf, .docx）
                string updatedContent = Regex.Replace(content, @"<a\s+[^>]*href=""([^""]+\.(pdf|docx?))""[^>]*>(.*?)<\/a>", match =>
                {
                    string href = match.Groups[1].Value;
                    string fileName = href.Substring(href.LastIndexOf('/') + 1);
                    string linkText = match.Groups[3].Value;
                    return $"<a href=\"{href}\" target=\"_blank\" title=\"{fileName} _PDF檔案下載(另開新視窗)\" aria-label=\"下載 {fileName}\">{linkText}</a>";

                }, RegexOptions.IgnoreCase);

                // 添加 `alt` 到所有的圖片
                updatedContent = Regex.Replace(updatedContent, @"<img\s+([^>]*?)>", match =>
                {
                    string imgTag = match.Groups[1].Value;
                    if (!imgTag.Contains("alt="))
                    {
                        return $"<img {imgTag} alt=\"文章插圖\">";
                    }
                    return $"<img {imgTag}>";
                }, RegexOptions.IgnoreCase);

                return updatedContent;
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }
        #endregion

        #region 文章內容顯示
        public ActionResult ViewArticle(string encryptedUrl)
        {
            try
            {
                //AutoExpireArticles(); //自動下架過期文章

                var decryptedUrl = DecryptUrl(encryptedUrl);
                if (string.IsNullOrEmpty(decryptedUrl))
                {
                    Console.WriteLine("[ERROR] 解密後的URL為空");
                    return RedirectToAction("Error404", "Error");
                }

                //僅查詢已啟用的文章
                var article = _db.ArticleContent.FirstOrDefault(a => a.EncryptedUrl == encryptedUrl && a.IsEnabled);

                if (article == null)
                {
                    Console.WriteLine($"[ERROR] 找不到文章: {decryptedUrl}");
                    article = _db.ArticleContent.FirstOrDefault(a => a.Title == decryptedUrl);
                }

                // 如果仍然找不到
                if (article == null)
                {
                    Console.WriteLine($"[ERROR] 找不到文章 Title: {decryptedUrl}");
                    return RedirectToAction("Error404", "Error");
                }

                article.ClickCount += 1; //增加點閱率次數

                //查詢並取得關聯的文件檔案
                var associatedDocuments = _db.Documents
                    .Where(d => d.ArticleId == article.Id && d.IsActive)
                    .Select(d => new DocumentViewModel
                    {
                        DocumentID = d.DocumentID,
                        DocumentName = d.DocumentName,
                        DocumentType = d.DocumentType,
                        UploadTime = d.UploadTime,
                        FileSize = d.FileSize,
                        ArticleId = d.ArticleId ?? 0// 可用於下載鏈接
                    }).ToList();


                var model = new ArticleViewModel
                {
                    Article = article,
                    AssociatedDocuments = associatedDocuments
                };

                //處理多個Hashtags顯示
                ViewBag.DisplayHashtags = article.Hashtags?.Split(',').Select(tag => tag.Trim()).ToList();

                // 查找同一標籤下的上一篇和下一篇文章
                var targetHashtags = article.Hashtags.Split(',').Select(tag => tag.Trim()).ToList();
                var articlesWithSameTag = _db.ArticleContent
                    .Where(a => a.IsEnabled && a.Hashtags != null)
                    .AsEnumerable() // 將資料拉至記憶體中處理 Hashtags
                    .Where(a => a.Hashtags.Split(',').Any(tag => targetHashtags.Contains(tag.Trim())))
                    .OrderBy(a => a.PublishedDate)
                    .ToList();

                int currentIndex = articlesWithSameTag.FindIndex(a => a.Id == article.Id);
                var previousArticle = currentIndex > 0 ? articlesWithSameTag[currentIndex - 1] : null;
                var nextArticle = currentIndex < articlesWithSameTag.Count - 1 ? articlesWithSameTag[currentIndex + 1] : null;

                ViewBag.ArticleId = article.Id;
                ViewBag.Comments = _db.MessageBoard.Where(c => c.ArticleId == article.Id && c.IsApproved).ToList();
                ViewBag.CommentCount = ViewBag.Comments.Count;

                //顯示留言數量
                ViewBag.Comments = _db.MessageBoard.Where(c => c.ArticleId == article.Id && c.IsApproved).ToList();
                ViewBag.CommentCount = ViewBag.Comments.Count;

                //設定分頁
                ViewBag.PreviousArticle = previousArticle;
                ViewBag.NextArticle = nextArticle;

                var menus = _db.Menus.ToList(); //主題目錄
                var menuItems = _db.MenuItems.ToList(); //子主題目錄

                // 字典來管理父目錄及其子目錄
                var parentDirectories = new Dictionary<string, List<string>>
                {
                    { "科普專欄", new List<string> { "運動醫學", "運動科技", "運動科學研究", "運動生理研究", "運動心理", "體能訓練研究", "運動營養研究", "運動科技與資訊開發", "運動管理","兒少科普", "運動醫學研究", "科普海報下載專區", "運動心理研究" } },

                    { "中心公告", new List<string> { "新聞發佈", "中心訊息", "徵才招募",} },

                    { "影音專區", new List<string> { "中心成果", "新聞影音", "活動紀錄", } },
                };

                var currentSubDirectory = article.ContentType; // 文章的子目錄可以通過 ContentType 獲得
                var parentDirectory = parentDirectories.FirstOrDefault(pd => pd.Value.Contains(currentSubDirectory)).Key;
                ViewBag.ParentDirectory = parentDirectory;
                ViewBag.CurrentSubDirectory = currentSubDirectory;

                var parentMenu = menus.ToDictionary(m => m.Title, m => menuItems.Where(mi => mi.MenuId == m.Id).Select(mi => mi.Name).ToList());

                ViewBag.Menus = menus;
                ViewBag.ParentMenu = parentMenu;

                var comments = _db.MessageBoard.Where(m => m.ArticleId == article.Id && m.IsApproved).ToList();
                ViewBag.Comments = comments;
                ViewBag.CommentCount = comments.Count;

                ViewBag.Title = $"{article.Title}";// 設定網頁描述性的標題

                var menuList = new Dictionary<string, string> //子主題連結
                {
                    { "運動醫學", "/PopularScience/sportMedicine" },
                    { "運動科技", "/PopularScience/sportTech" },
                    { "運動科學", "/PopularScience/sportScience" },
                    { "運動生理", "/PopularScience/sportsPhysiology" },
                    { "運動心理", "/PopularScience/sportsPsychology" },
                    { "體能訓練", "/PopularScience/physicalTraining" },
                    { "運動營養", "/PopularScience/sportsNutrition" },
                    { "新聞發佈", "/CenterIntroduction/press" },
                    { "中心訊息", "/CenterIntroduction/institute" },
                    { "徵才招募", "/CenterIntroduction/recruit" },
                    { "中心成果", "/AduioVideoArea/achievement" },
                    { "新聞影音", "/AduioVideoArea/news" },
                    { "活動紀錄", "/AduioVideoArea/activityRecord" },
                    { "兒少科普", "/PopularScience/childrenScience" },
                    { "科普海報下載專區", "/PopularScience/SciencePosterDownLoad" },
                };

                ViewBag.MenuUrls = menuList;

                var currentParentDirectory = ViewBag.ParentDirectory as string;
                var menuIdMapping = new Dictionary<string, int>
                {
                    { "科普專欄", 1 },
                    { "中心公告", 2 },
                    { "影音專區", 3 }
                };

                // 將發佈日期格式化為 "yyyy-MM-dd"
                string formattedDate = article.PublishedDate.HasValue ? article.PublishedDate.Value.ToString("yyyy-MM-dd") : string.Empty;
                ViewBag.FormattedPublishedDate = formattedDate;

                // 根據當前主題獲取對應的 MenuId
                int menuId = menuIdMapping.TryGetValue(currentParentDirectory, out var id) ? id : 0; //默認值

                // 根據 MenuId 查找「全部文章」的連結
                var menuUrls = menuItems
                    .Where(item => item.MenuId == menuId)
                    .GroupBy(item => item.Name)
                    .ToDictionary(group => group.Key, group => group.Last().Url);

                var allArticlesUrl = menuItems
                    .Where(item => item.Name == "全部文章" && item.MenuId == menuId)
                    .Select(item => item.Url)
                    .FirstOrDefault();

                ViewBag.MenuUrls = menuUrls;
                ViewBag.AllArticlesUrl = allArticlesUrl ?? "#";

                //載入文章類型選項(供表單編輯用)
                ViewBag.Categories = new SelectList(_db.ArticleCategory.ToList(), "Id", "CategoryName", article.ContentTypeId);

                ViewBag.DisplayHashtags = article.Hashtags?.Split(',').ToList();
                //載入標籤選項(供表單編輯用)
                ViewBag.Hashtags = new MultiSelectList(_db.Hashtag.ToList(), "Name", "Name", article.Hashtags?.Split(','));
                ViewBag.Article = $"國家運動科學中心_{article.Title}";

                _db.SaveChanges();

                return View(model);
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }
        #endregion

        #region 自動隱藏過期文章
        private void AutoExpireArticles()
        {
            try
            {
                var now = DateTime.Now;
                var expiredArticles = _db.ArticleContent
                    .Where(a => a.ExpireDate != null && a.ExpireDate <= now && a.IsEnabled).ToList();

                foreach (var article in expiredArticles)
                {
                    article.IsEnabled = false;
                    article.UpdatedDate = DateTime.Now;
                    article.UpdatedUser = "SystemAutoExpire";
                }

                if (expiredArticles.Any())
                {
                    _db.SaveChanges();
                }
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }
        #endregion

        #region 處理修改編輯文章
        public ActionResult EditArticle(int id)
        {
            var article = _db.ArticleContent.Find(id);
            if (article == null) return HttpNotFound();

            var documents = _db.Documents
                .Where(d => d.ArticleId == article.Id && d.IsActive)
                .ToList();

            var documentViewModels = documents.Select(d => new DocumentViewModel
            {
                DocumentID = d.DocumentID,
                DocumentName = d.DocumentName,
                DocumentType = d.DocumentType,
                UploadTime = d.UploadTime,
                FileSize = d.FileSize,
                ArticleId = d.ArticleId ?? 0,
                Category = d.Category
            }).ToList();

            var viewModel = new ArticleViewModel
            {
                Article = article,
                AssociatedDocuments = documentViewModels
            };

            ViewBag.Categories = new SelectList(_db.ArticleCategory.ToList(), "Id", "CategoryName", article.ContentTypeId);
            ViewBag.Hashtags = new MultiSelectList(_db.Hashtag.ToList(), "Name", "Name", article.Hashtags?.Split(','));
            ViewBag.SelectedCategory = documentViewModels.FirstOrDefault()?.Category; // ✅ 加這行

            return View(viewModel);
        }


        [HttpPost]
        [ValidateAntiForgeryToken]
        [ValidateInput(false)]
        public ActionResult EditArticle(ArticleViewModel dto, HttpPostedFileBase imageFile, HttpPostedFileBase[] documentFiles, string documentCategory, string[] tags, HttpPostedFileBase[] documentFilePics)
        {
            try
            {
                if (ModelState.IsValid)
                {
                    var exist = _db.ArticleContent.Find(dto.Article.Id);

                    if (exist != null)
                    {
                        if (imageFile != null && imageFile.ContentLength > 0) //如果有上傳新圖片，則處理圖片上傳
                        {
                            using (var binaryReader = new BinaryReader(imageFile.InputStream))
                            {
                                byte[] imageData = binaryReader.ReadBytes(imageFile.ContentLength);

                                exist.ImageContent = imageData; //將圖片儲存為二進制數據
                            }
                        }

                        if (exist.Title != dto.Article.Title) //更新 `Title` 並同步 `EncryptedUrl`
                        {
                            exist.Title = dto.Article.Title;
                            exist.EncryptedUrl = EncryptUrl(dto.Article.Title); //確保 EncryptedUrl 更新
                        }

                        if (documentFiles != null && documentFiles.Length > 0) //新增文件上傳功能
                        {
                            var fileUploadResult = SaveDocumentFile(documentFiles, dto.Article.Id, documentCategory);
                            if (!fileUploadResult)
                            {
                                ModelState.AddModelError("", "文件上傳失敗。");
                            }
                        }
                        else if (documentFilePics != null && documentFilePics.Length > 0)
                        {
                            var fileUploadResult = SaveDocumentFile(documentFilePics, dto.Article.Id, documentCategory);
                            if (!fileUploadResult)
                            {
                                ModelState.AddModelError("", "圖片上傳失敗。");
                            }
                        }

                        //更新文章內容和其他欄位
                        exist.Title = dto.Article.Title;
                        exist.ContentBody = dto.Article.ContentBody;
                        exist.UpdatedDate = DateTime.Now;
                        exist.ContentType = dto.Article.ContentType;
                        exist.UpdatedUser = Session["UserName"] as string;
                        exist.Hashtags = string.Join(",", tags);
                        exist.ExpireDate = dto.Article.ExpireDate;

                        _db.SaveChanges();

                        var route = GetRedirectTarget(dto.Article.ContentType);
                        ViewBag.SuccessMessage = "文章保存成功！";
                        //TempData["SuccessMessage"] = "文章保存成功！"; // 使用 TempData 傳遞訊息
                        return RedirectToAction(route.Item2, route.Item1);
                    }
                }
                return View(dto);
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }
        #endregion

        #region 導回對應文章主題頁
        private Tuple<string, string> GetRedirectTarget(string contentType)
        {
            var routeMap = new Dictionary<string, Tuple<string, string>>
            {
                { "新聞發佈", Tuple.Create("LatestNews", "press") },
                { "中心訊息", Tuple.Create("LatestNews", "institute") },
                { "徵才招募", Tuple.Create("LatestNews", "recruit") },
                { "中心成果", Tuple.Create("AduioVideoArea", "achievement") },
                { "新聞影音", Tuple.Create("AduioVideoArea", "news") },
                { "活動紀錄", Tuple.Create("AduioVideoArea", "activityRecord") },
                { "運動醫學", Tuple.Create("PopularScience", "sportMedicine") },
                { "運動科技", Tuple.Create("PopularScience", "sportTech") },
                { "運動科學", Tuple.Create("PopularScience", "sportScience") },
                { "運動生理", Tuple.Create("PopularScience", "sportsPhysiology") },
                { "運動心理", Tuple.Create("PopularScience", "sportsPsychology") },
                { "體能訓練", Tuple.Create("PopularScience", "physicalTraining") },
                { "運動營養", Tuple.Create("PopularScience", "sportsNutrition") },
                { "兒少科普", Tuple.Create("PopularScience", "childrenScience") },
                { "科普海報下載專區", Tuple.Create("PopularScience", "SciencePosterDownLoad") }
            };

                return routeMap.TryGetValue(contentType, out var route) ? route
                : Tuple.Create("Home", "Tiss"); // 預設跳首頁
        }
        #endregion

        #region 刪除Tiny附件檔案
        [HttpPost]
        [ValidateAntiForgeryToken]
        public JsonResult DeleteFile(int documentId)
        {
            try
            {
                var document = _db.Documents.FirstOrDefault(d => d.DocumentID == documentId);
                if (document != null)
                {
                    // 取消啟用文件
                    document.IsActive = false;

                    // 取得目前使用者帳號
                    string userAccount = Session["UserName"] as string ?? "Unknown";

                    // 寫入紀錄 (可選：更新 Creator 為異動者)
                    document.UploadTime = DateTime.Now;
                    document.Creator = userAccount;

                    // 寫入 log
                    _db.DocumentLog.Add(new DocumentLog
                    {
                        DocumentID = document.DocumentID,
                        OperateAction = "Disable",
                        ModifiedBy = userAccount,
                        ModifiedTime = DateTime.Now
                    });

                    _db.SaveChanges();

                    return Json(new { success = true });
                }

                return Json(new { success = false, message = "找不到對應的文件。" });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "伺服器錯誤：" + ex.Message });
            }
        }
        #endregion

        #region 儲存文件檔案
        private bool SaveDocumentFile(HttpPostedFileBase[] files, int articleId, string documentCategory)
        {
            try
            {
                if (files == null || files.Length == 0)
                {
                    return false;
                }

                foreach (var file in files)
                {
                    if (file != null && file.ContentLength > 0)
                    {
                        string fileName = Path.GetFileName(file.FileName);
                        string fileExtension = Path.GetExtension(fileName).ToLower();

                        if (fileExtension == ".pdf" || fileExtension == ".doc" || fileExtension == ".docx" || fileExtension == ".odt" ||
                    fileExtension == ".jpeg" || fileExtension == ".jpg" || fileExtension == ".png")
                        {
                            byte[] fileData;

                            using (var binaryReader = new BinaryReader(file.InputStream))
                            {
                                fileData = binaryReader.ReadBytes(file.ContentLength);
                            }

                            var userName = this.HttpContext.Session["UserName"]?.ToString() ?? "Unknown";

                            var document = new Documents
                            {
                                DocumentName = fileName,
                                UploadTime = DateTime.Now,
                                Creator = userName,
                                DocumentType = fileExtension,
                                FileSize = fileData.Length,
                                FileContent = fileData,
                                IsActive = true,
                                Category = documentCategory,
                                ArticleId = articleId // 關聯文章ID
                            };

                            _db.Documents.Add(document);
                        }
                    }
                }

                _db.SaveChanges();
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine("文件上傳錯誤：" + ex.Message);
                return false;
            }
        }
        #endregion

        #region Tiny編輯器上傳文書檔案跟圖片
        [HttpPost]
        public ActionResult UploadPDF(HttpPostedFileBase file, int? articleId)
        {
            if (file != null && file.ContentLength > 0)
            {
                try
                {
                    var fileName = Path.GetFileName(file.FileName);
                    var extension = Path.GetExtension(fileName).ToLower();
                    var allowedExtensions = new[] { ".jpg", ".jpeg", ".png", ".pdf", ".doc", ".docx", "odt" }; //根據需求設定允許的副檔名


                    byte[] fileData; //讀取檔案的二進位數據

                    using (var binaryReader = new BinaryReader(file.InputStream))
                    {
                        fileData = binaryReader.ReadBytes(file.ContentLength);
                    }

                    var fileSize = fileData.Length; //確認檔案大小

                    var creator = Session["UserName"]?.ToString() ?? "未知使用者"; //從 Session 中獲取使用者名稱

                    // 儲存至資料庫
                    var document = new Documents
                    {
                        DocumentName = fileName,
                        UploadTime = DateTime.Now,
                        Creator = creator,
                        DocumentType = extension,
                        FileSize = fileSize,
                        FileContent = fileData,
                        IsActive = true,
                        ArticleId = articleId,

                        Category = "TinyMCEFileUpload" //標示檔案類型
                    };
                    _db.Documents.Add(document);
                    _db.SaveChanges();

                    // 創建 URL 來顯示檔案，這個 URL 可以是從資料庫取得的檔案連結
                    var fileUrl = Url.Action("DownloadFile", "Article", new { id = document.DocumentID });

                    // 返回成功消息，包含新生成的 ID 和 URL
                    return Json(new { message = "檔案上傳成功", fileId = document.DocumentID, url = fileUrl });
                }
                catch (Exception ex)
                {
                    return Json(new { error = "檔案上傳失敗: " + ex.Message });
                }
            }

            return Json(new { error = "檔案上傳失敗" });
        }
        #endregion

        #region 下載文件的通用方法
        public ActionResult DownloadFile(int documentId)
        {
            try
            {
                var document = _db.Documents.Find(documentId);
                if (document != null)
                {
                    var contentType = GetContentType(document.DocumentName);
                    var disposition = document.DocumentType == ".pdf" ? "inline" : "attachment";
                    var encodedFileName = Uri.EscapeDataString(document.DocumentName);

                    Response.Headers["Content-Disposition"] = $"{disposition}; filename=\"{document.DocumentName}\"; filename*=UTF-8''{encodedFileName}";

                    // 如果是 PDF 文件，則更新標題；如果是圖片，則直接使用原始內容
                    byte[] fileData;
                    if (document.DocumentType == ".pdf")
                    {
                        fileData = UpdatePdfTitle(document.FileContent, document.DocumentName);
                    }
                    else
                    {
                        fileData = document.FileContent; // 直接使用圖片的二進制數據
                    }

                    var stream = new MemoryStream(fileData);
                    return new FileStreamResult(stream, contentType);
                }
                return HttpNotFound();
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }
        #endregion

        #region 判斷文件類型，回傳適當的MIME類型
        private string GetContentType(string fileName)
        {
            try
            {
                var extension = Path.GetExtension(fileName).ToLower();
                switch (extension)
                {
                    case ".pdf":
                        return "application/pdf";
                    case ".doc":
                    case ".docx":
                        return "application/msword";
                    case ".odt":
                        return "application/vnd.oasis.opendocument.text"; // ODT 檔案的 MIME 類型
                    case ".jpeg":
                    case ".jpg":
                        return "image/jpeg"; // JPG 和 JPEG 的 MIME 類型
                    case ".png":
                        return "image/png"; // PNG 的 MIME 類型
                    default:
                        return "application/octet-stream"; // 其他文件類型預設為下載
                }
            }
            catch (Exception ex)
            {
                throw ex;
            }

        }
        #endregion

        #region 文件類別的頁面處理
        private ActionResult DocumentList(string category, string viewName, int page = 1, int pageSize = 10)
        {
            try
            {
                page = Math.Max(1, page); // 確保頁碼至少為 1

                var list = _db.Documents.Where(d => d.Category == category && d.IsActive)
                    .OrderByDescending(d => d.UploadTime).ToList();

                var totalDocuments = list.Count();
                var totalPages = (int)Math.Ceiling(totalDocuments / (double)pageSize);

                // 即便沒有文件資料，也應該返回空列表，頁面不應該報錯
                page = Math.Min(page, totalPages > 0 ? totalPages : 1); // 確保頁碼不超過最大頁數

                var dtos = list.Skip((page - 1) * pageSize).Take(pageSize).Select(d => new DocumentModel
                {
                    DocumentID = d.DocumentID,
                    DocumentName = d.DocumentName,
                    DocumentType = d.DocumentType,
                    UploadTime = d.UploadTime,
                    Creator = d.Creator,
                    FileSize = d.FileSize,
                    IsActive = d.IsActive,
                }).ToList();

                ViewBag.CurrentPage = page;
                ViewBag.TotalPages = totalPages;

                return View(viewName, dtos);
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

        #endregion

        #region 修改PDF標題名稱
        public byte[] UpdatePdfTitle(byte[] pdfBytes, string newTitle)
        {
            try
            {
                using (var inputMs = new MemoryStream(pdfBytes)) //原始 PDF 數據
                using (var outputMs = new MemoryStream()) //用於寫入修改後的 PDF 數據
                {

                    using (var reader = new PdfReader(inputMs)) //使用 PdfReader 讀取原始 PDF
                    using (var writer = new PdfWriter(outputMs))
                    {
                        // 創建 PdfDocument 以便修改 PDF 文件
                        using (var pdfDoc = new PdfDocument(reader, writer))
                        {
                            pdfDoc.GetDocumentInfo().SetTitle(newTitle); //設定 PDF 文件的 metadata

                            pdfDoc.Close();
                        }
                    }

                    return outputMs.ToArray(); // 返回新的 PDF 數據
                }
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }
        #endregion
    }
}