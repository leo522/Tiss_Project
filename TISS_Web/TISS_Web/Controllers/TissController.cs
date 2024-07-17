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

namespace TISS_Web.Controllers
{
    public class TissController : Controller
    {
        private TISS_WebEntities _db = new TISS_WebEntities();

        #region 檔案上傳共用服務

        private readonly FileUploadService _fileUploadService;
        public TissController()
        {
            _fileUploadService = new FileUploadService(new TISS_WebEntities());
            _contentService = new WebContentService(new TISS_WebEntities()); //網頁內容存檔共用服務
        }
        #endregion

        #region 網頁內容存檔共用服務
        private readonly WebContentService _contentService;
        #endregion

        #region 登入
        public ActionResult Login()
        {
            return View();
        }

        [HttpPost]
        public ActionResult Login(string UserName, string pwd)
        {
            // 將使用者輸入的密碼進行SHA256加密
            string hashedPwd = ComputeSha256Hash(pwd);
            var dto = _db.Users.FirstOrDefault(u => u.UserName == UserName && u.Password == hashedPwd);

            if (dto != null)
            {
                // 驗證成功，更新最後登入時間
                dto.LastLoginDate = DateTime.Now;
                _db.SaveChanges();

                // 設定 Session 狀態為已登入
                Session["LoggedIn"] = true;
                Session["UserName"] = dto.UserName;

                // 檢查是否有記錄的返回頁面
                string returnUrl = Session["ReturnUrl"] != null ? Session["ReturnUrl"].ToString() : Url.Action("Home", "Tiss");

                // 清除返回頁面的 Session 記錄
                Session.Remove("ReturnUrl");

                // 重定向到記錄的返回頁面
                return Redirect(returnUrl);
            }
            else
            {
                // 驗證失敗
                ViewBag.ErrorMessage = "帳號或密碼錯誤";
                return View();
            }
        }
        #endregion

        #region 登出
        public ActionResult Logout()
        {
            // 清除所有的 Session 資訊
            //Session.Clear();
            //Session.Abandon();
            Session.Remove("LoggedIn");
            // 清除所有的 Forms 認證 Cookies
            FormsAuthentication.SignOut();

            // 取得登出前的頁面路徑，如果沒有則預設為首頁
            string returnUrl = Request.UrlReferrer != null ? Request.UrlReferrer.ToString() : Url.Action("Home", "Tiss");

            // 重定向到記錄的返回頁面
            return Redirect(returnUrl);
            // 重定向到 Home 頁面
        }
        #endregion

        #region 註冊帳號

        public ActionResult Register()
        {
            return View();
        }

        [HttpPost]
        public ActionResult Register(string UserName, string pwd, string Email)
        {
            if (pwd.Length < 6)
            {
                ViewBag.ErrorMessage = "密碼長度至少要6位數";
                return View();
            }

            if (_db.Users.Any(u => u.UserName == UserName))
            {
                ViewBag.ErrorMessage = "該帳號已存在";
                return View();
            }

            var Pwd = ComputeSha256Hash(pwd);
            var newUser = new Users
            {
                UserName = UserName,
                Password = Pwd,
                Email = Email,
                CreatedDate = DateTime.Now,
                LastLoginDate = DateTime.Now,
                IsActive = true
            };

            _db.Users.Add(newUser);
            _db.SaveChanges();

            return RedirectToAction("Login");
        }
        #endregion

        #region 密碼加密
        private static string ComputeSha256Hash(string rawData)
        {
            using (SHA256 sha256Hash = SHA256.Create())
            {
                byte[] bytes = sha256Hash.ComputeHash(Encoding.UTF8.GetBytes(rawData));

                StringBuilder builder = new StringBuilder();
                for (int i = 0; i < bytes.Length; i++)
                {
                    builder.Append(bytes[i].ToString("x2"));
                }
                return builder.ToString();
            }
        }
        #endregion


        #region 修改密碼-待修改為生成重設連結並發送電子郵件-尚未開始做
        [HttpGet]
        public ActionResult ChangePassword()
        {
            // 檢查用戶是否已登入，如果未登入則重定向到登入頁面
            if (Session["LoggedIn"] == null || !(bool)Session["LoggedIn"])
            {
                return RedirectToAction("Login");
            }

            // 獲取當前登入用戶的資料
            string userName = Session["UserName"] as string;
            var user = _db.Users.FirstOrDefault(u => u.UserName == userName);

            if (user == null)
            {
                ViewBag.ErrorMessage = "無法找到當前用戶";
                return View();
            }

            ViewData["UserName"] = userName;
            return View();
        }
        [HttpPost]
        public ActionResult ChangePassword(string userName, string oldPwd, string newPwd)
        {
            try
            {
                // 驗證舊密碼是否正確
                var user = _db.Users.FirstOrDefault(u => u.UserName == userName);

                if (user == null)
                {
                    ViewBag.ErrorMessage = "無法找到當前用戶";
                    return View();
                }

                if (user.Password != ComputeSha256Hash(oldPwd))
                {
                    ViewBag.ErrorMessage = "舊密碼輸入錯誤";
                    ViewBag.UserName = userName;
                    return View();
                }

                // 更新新密碼
                user.Password = ComputeSha256Hash(newPwd);

                try
                {
                    _db.SaveChanges();
                    ViewBag.SuccessMessage = "密碼修改成功";
                }
                catch (Exception ex)
                {
                    ViewBag.ErrorMessage = "修改密碼時發生錯誤：" + ex.Message;
                }

                return View();
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }
        #endregion

        #region 自己上傳圖片和文字使用

        public ActionResult editPage()
        {
            try
            {
                return View();
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

        [HttpPost]
        [ValidateInput(false)]
        public JsonResult SavePageData(string textContent, string imagePath)
        {
            try
            {
                var userName = Session["UserName"] as string;

                byte[] imageData = null;

                if (!string.IsNullOrEmpty(imagePath))
                {
                    // 解析圖片數據
                    byte[] binaryData = Convert.FromBase64String(imagePath);
                    imageData = binaryData;
                }

                // 計算新的 FileNo
                int newFileNo = 1;
                var lastNo = _db.PressPageContent.OrderByDescending(f => f.FileNo).FirstOrDefault();
                if (lastNo != null)
                {
                    //newFileNo = lastNo.FileNo.GetValueOrDefault() + 1;
                    newFileNo = lastNo.FileNo + 1;
                }

                // 創建新的 db 物件並設置屬性值
                var newContent = new PressPageContent
                {
                    //UserAccount = userName,
                    UserAccount = "00048",
                    TextContent = textContent,
                    TextUpdateTime = DateTime.Now,
                    ImageContent = imageData,
                    ImageUpdateTime = DateTime.Now,
                    FileNo = newFileNo
                };

                // 將新的 AnnouncementPageContent 物件添加到資料庫並保存變更
                _db.PressPageContent.Add(newContent);
                _db.SaveChanges();

                // 返回成功的 JSON 響應並包含圖片數據（可選）
                string imageBase64 = imageData != null ? $"data:image/jpeg;base64,{Convert.ToBase64String(imageData)}" : null;
                return Json(new { success = true, image = imageBase64 });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, error = ex.Message });
            }
        }
        #endregion

        #region 上傳文件檔案

        // 上傳計畫文件
        [HttpPost]
        public ActionResult UploadPlanDocument(HttpPostedFileBase file, int? page)
        {
            _fileUploadService.UploadFile(file, "PlanDocument");
            return RedirectToAction("Plan", new { page });
        }

        //上傳法規文件
        [HttpPost]
        public ActionResult UploadRegulationDocument(HttpPostedFileBase file, int? page)
        {
            _fileUploadService.UploadFile(file, "RegulationDocument");
            return RedirectToAction("Regulation", new { page });
        }

        //上傳辦法及要點文件
        [HttpPost]
        public ActionResult UploadProcedureDocument(HttpPostedFileBase file, int? page)
        {
            _fileUploadService.UploadFile(file, "ProcedureDocument");
            return RedirectToAction("Procedure", new { page });
        }

        // 上傳下載專區文件
        [HttpPost]
        public ActionResult UploadDownloadDocument(HttpPostedFileBase file, int? page)
        {
            _fileUploadService.UploadFile(file, "DownloadDocument");
            return RedirectToAction("download", new { page });
        }

        // 上傳預算與決算文件
        [HttpPost]
        public ActionResult UploadBudgetDocument(HttpPostedFileBase file, int? page)
        {
            _fileUploadService.UploadFile(file, "BudgetDocument");
            return RedirectToAction("budget", new { page });
        }

        // 上傳其他文件
        [HttpPost]
        public ActionResult UploadOtherDocument(HttpPostedFileBase file, int? page)
        {
            _fileUploadService.UploadFile(file, "OtherDocument");
            return RedirectToAction("other", new { page });
        }

        // 上傳採購作業實施規章文件
        [HttpPost]
        public ActionResult UploadPurchaseDocument(HttpPostedFileBase file, int? page)
        {
            _fileUploadService.UploadFile(file, "PurchaseDocument");
            return RedirectToAction("purchase", new { page });
        }
        #endregion

        #region 首頁
        /// <summary>
        /// 首頁
        /// </summary>
        /// <returns></returns>
        public ActionResult Home()
        {
            Session["ReturnUrl"] = Request.Url.ToString();
            return View();
        }
        #endregion

        #region 最新消息-中心公告

        /// <summary>
        /// 中心公告
        /// </summary>
        /// <returns></returns>
        public ActionResult announcement()
        {

            Session["ReturnUrl"] = Request.Url.ToString();
            var list = _db.ArticleContent.Where(a => a.ContentType == "中心訊息").ToList();
            var articles = list.Select(s => new ArticleContentModel
            {
                Title = s.Title,
                EncryptedUrl = EncryptUrl(s.Title),
                ImageContent = s.ImageContent,
                Hashtags = s.Hashtags,
                ContentType = _db.ArticleCategory.FirstOrDefault(c => c.Id == s.ContentTypeId)?.CategoryName
            }).ToList();

            return View(articles);
        }

        /// <summary>
        /// 新聞發布
        /// </summary>
        /// <returns></returns>
        public ActionResult press()
        {
            Session["ReturnUrl"] = Request.Url.ToString();
            var list = _db.ArticleContent.Where(a => a.ContentType == "新聞發佈").ToList();
            var articles = list.Select(s => new ArticleContentModel
            {
                Title = s.Title,
                EncryptedUrl = EncryptUrl(s.Title),
                ImageContent = s.ImageContent,
                Hashtags = s.Hashtags,
                ContentType = _db.ArticleCategory.FirstOrDefault(c => c.Id == s.ContentTypeId)?.CategoryName
            }).ToList();

            return View(articles);
        }

        /// <summary>
        /// 中心訊息
        /// </summary>
        /// <returns></returns>
        public ActionResult institute()
        {
            Session["ReturnUrl"] = Request.Url.ToString();
            var list = _db.ArticleContent.Where(a => a.ContentType == "中心訊息").ToList();
            var articles = list.Select(s => new ArticleContentModel
            {
                Title = s.Title,
                EncryptedUrl = EncryptUrl(s.Title),
                ImageContent = s.ImageContent,
                Hashtags = s.Hashtags,
                ContentType = _db.ArticleCategory.FirstOrDefault(c => c.Id == s.ContentTypeId)?.CategoryName
            }).ToList();

            return View(articles);
        }

        /// <summary>
        /// 徵才招募
        /// </summary>
        /// <returns></returns>
        public ActionResult recruit()
        {
            Session["ReturnUrl"] = Request.Url.ToString();
            var list = _db.ArticleContent.Where(a => a.ContentType == "徵才招募").ToList();
            var articles = list.Select(s => new ArticleContentModel
            {
                Title = s.Title,
                EncryptedUrl = EncryptUrl(s.Title),
                ImageContent = s.ImageContent,
                Hashtags = s.Hashtags,
                ContentType = _db.ArticleCategory.FirstOrDefault(c => c.Id == s.ContentTypeId)?.CategoryName
            }).ToList();

            return View(articles);
        }

        /// <summary>
        /// 委託研究計劃
        /// </summary>
        /// <returns></returns>
        public ActionResult researchProject()
        {
            Session["ReturnUrl"] = Request.Url.ToString();
            int newsItemId = 1;
            var item = _db.ResearchProjectPageContent.FirstOrDefault(i => i.ID == newsItemId);
            if (item != null)
            {
                // 傳遞點擊數給前端
                ViewBag.ClickCount = item.ClickCount;
            }
            else
            {
                // 如果找不到對應的新聞項目，點擊數為 0
                ViewBag.ClickCount = 0;
            }

            return View();
        }

        /// <summary>
        /// 政府網站服務管理規範
        /// </summary>
        /// <returns></returns>
        public ActionResult GovernmentWebsite()
        {
            Session["ReturnUrl"] = Request.Url.ToString();
            return View();
        }

        #endregion

        #region 影音專區
        /// <summary>
        /// 影音專區
        /// </summary>
        /// <returns></returns>
        public ActionResult video()
        {
            Session["ReturnUrl"] = Request.Url.ToString();
            return View();
        }

        /// <summary>
        /// 中心成果
        /// </summary>
        /// <returns></returns>
        public ActionResult achievement()
        {
            return View();
        }

        /// <summary>
        /// 新聞影音
        /// </summary>
        /// <returns></returns>
        public ActionResult news()
        {
            return View();
        }

        /// <summary>
        /// 活動紀錄
        /// </summary>
        /// <returns></returns>
        public ActionResult activityRecord()
        {
            return View();
        }
        #endregion

        #region 中心介紹
        /// <summary>
        /// 中心介紹
        /// </summary>
        /// <returns></returns>
        public ActionResult about()
        {
            Session["ReturnUrl"] = Request.Url.ToString();
            return View();
        }

        /// <summary>
        /// 儲存
        /// </summary>
        /// <returns></returns>
        [HttpPost]
        [ValidateInput(false)]
        public ActionResult aboutSaveContent(string textContent, string ImageSrc)
        {
            try
            {
                var userName = Session["UserName"] as string;
                return _contentService.SaveContent(userName, textContent, ImageSrc, () => new AboutPageContent());
            }
            catch (Exception ex)
            {
                return Json(new { success = false, error = ex.Message });
            }
        }

        /// <summary>
        /// 讀取
        /// </summary>
        /// <returns></returns>
        [HttpGet]
        public ActionResult aboutGetContent()
        {
            try
            {
                return _contentService.GetContent<AboutPageContent>();
            }
            catch (Exception ex)
            {
                return Json(new { success = false, error = ex.Message }, JsonRequestBehavior.AllowGet);
            }
        }

        /// <summary>
        /// 使命願景
        /// </summary>
        /// <returns></returns>
        public ActionResult Objectives()
        {
            Session["ReturnUrl"] = Request.Url.ToString();
            return View();
        }
        [HttpPost]
        [ValidateInput(false)]
        public ActionResult objectivesSaveContent(string textContent, string ImageSrc)
        {
            try
            {
                var userName = Session["UserName"] as string;
                return _contentService.SaveContent(userName, textContent, ImageSrc, () => new ObjectivesPageContent());
            }
            catch (Exception ex)
            {
                return Json(new { success = false, error = ex.Message });
            }
        }
        [HttpGet]
        public ActionResult objectivesGetContent()
        {
            try
            {
                return _contentService.GetContent<ObjectivesPageContent>();
            }
            catch (Exception ex)
            {
                return Json(new { success = false, error = ex.Message }, JsonRequestBehavior.AllowGet);
            }
        }

        /// <summary>
        /// 任務
        /// </summary>
        /// <returns></returns>
        public ActionResult mission()
        {
            Session["ReturnUrl"] = Request.Url.ToString();
            return View();
        }
        [HttpPost]
        [ValidateInput(false)]
        public ActionResult missionSaveContent(string textContent, string ImageSrc)
        {
            try
            {
                var userName = Session["UserName"] as string;
                return _contentService.SaveContent(userName, textContent, ImageSrc, () => new MissionPageContent());
            }
            catch (Exception ex)
            {
                return Json(new { success = false, error = ex.Message });
            }
        }
        [HttpGet]
        public ActionResult missionGetContent()
        {
            try
            {
                return _contentService.GetContent<MissionPageContent>();
            }
            catch (Exception ex)
            {
                return Json(new { success = false, error = ex.Message }, JsonRequestBehavior.AllowGet);
            }
        }

        /// <summary>
        /// 組織概況
        /// </summary>
        /// <returns></returns>
        public ActionResult Organization()
        {
            Session["ReturnUrl"] = Request.Url.ToString();
            return View();
        }
        [HttpPost]
        [ValidateInput(false)]
        public ActionResult organizationSaveContent(string textContent, string ImageSrc)
        {
            try
            {
                var userName = Session["UserName"] as string;
                return _contentService.SaveContent(userName, textContent, ImageSrc, () => new OrganizationPageContent());
            }
            catch (Exception ex)
            {
                return Json(new { success = false, error = ex.Message });
            }
        }
        [HttpGet]
        public ActionResult organizationGetContent()
        {
            try
            {
                return _contentService.GetContent<OrganizationPageContent>();
            }
            catch (Exception ex)
            {
                return Json(new { success = false, error = ex.Message }, JsonRequestBehavior.AllowGet);
            }
        }

        /// <summary>
        /// 董監事
        /// </summary>
        /// <returns></returns>
        public ActionResult BOD()
        {
            Session["ReturnUrl"] = Request.Url.ToString();
            return View();
        }
        [HttpPost]
        [ValidateInput(false)]
        public ActionResult BODSaveContent(string textContent, string ImageSrc)
        {
            try
            {
                var userName = Session["UserName"] as string;
                return _contentService.SaveContent(userName, textContent, ImageSrc, () => new BODPageContent());
            }
            catch (Exception ex)
            {
                return Json(new { success = false, error = ex.Message });
            }
        }
        [HttpGet]
        public ActionResult BODGetContent()
        {
            try
            {
                return _contentService.GetContent<BODPageContent>();
            }
            catch (Exception ex)
            {
                return Json(new { success = false, error = ex.Message }, JsonRequestBehavior.AllowGet);
            }
        }

        /// <summary>
        /// 執行長
        /// </summary>
        /// <returns></returns>
        public ActionResult CEO()
        {
            Session["ReturnUrl"] = Request.Url.ToString();
            return View();
        }
        [HttpPost]
        [ValidateInput(false)]
        public ActionResult CeoSaveContent(string textContent, string ImageSrc)
        {
            try
            {
                var userName = Session["UserName"] as string;
                return _contentService.SaveContent(userName, textContent, ImageSrc, () => new CEOPageContent());
            }
            catch (Exception ex)
            {
                return Json(new { success = false, error = ex.Message });
            }
        }
        [HttpGet]
        public ActionResult CeoGetContent()
        {
            try
            {
                return _contentService.GetContent<CEOPageContent>();
            }
            catch (Exception ex)
            {
                return Json(new { success = false, error = ex.Message }, JsonRequestBehavior.AllowGet);
            }
        }

        /// <summary>
        /// 單位介紹
        /// </summary>
        /// <returns></returns>
        public ActionResult Units()
        {
            Session["ReturnUrl"] = Request.Url.ToString();
            return View();
        }

        [HttpPost]
        [ValidateInput(false)]
        public ActionResult unitsSaveContent(string textContent, string ImageSrc)
        {
            try
            {
                var userName = Session["UserName"] as string;
                return _contentService.SaveContent(userName, textContent, ImageSrc, () => new UnitsPageContent());
            }
            catch (Exception ex)
            {
                return Json(new { success = false, error = ex.Message });
            }
        }

        [HttpGet]
        public ActionResult unitsGetContent()
        {
            try
            {
                return _contentService.GetContent<UnitsPageContent>();
            }
            catch (Exception ex)
            {
                return Json(new { success = false, error = ex.Message }, JsonRequestBehavior.AllowGet);
            }
        }
        #endregion

        #region 科普專欄

        /// <summary>
        /// 科普專欄
        /// </summary>
        /// <returns></returns>
        public ActionResult research(int page = 1, int pageSize = 5)
        {
            Session["ReturnUrl"] = Request.Url.ToString();

            var relatedHashtags = new List<string>
            {
                "運動醫學",
                "運動科技",
                "運動科學",
                "運動生理",
                "運動心理",
                "體能訓練",
                "運動營養"
            };
            // 查詢相關 hashtags 的文章
            var list = _db.ArticleContent
                .Where(a => relatedHashtags.Contains(a.Hashtags) && a.IsEnabled)
                .ToList();

            var totalArticles = list.Count();
            var totalPages = (int)Math.Ceiling(totalArticles / (double)pageSize);

            var articles = list.Skip((page - 1) * pageSize).Take(pageSize).Select(s => new ArticleContentModel
            {
                Title = s.Title,
                EncryptedUrl = EncryptUrl(s.Title),
                ImageContent = s.ImageContent,
                Hashtags = s.Hashtags,
                ContentType = _db.ArticleCategory.FirstOrDefault(c => c.Id == s.ContentTypeId)?.CategoryName
            }).ToList();

            ViewBag.CurrentPage = page;
            ViewBag.TotalPages = totalPages;

            return View(articles);
        }

        /// <summary>
        /// 運動科學
        /// </summary>
        /// <returns></returns>
        public ActionResult sportScience(int page = 1, int pageSize = 5)
        {
            Session["ReturnUrl"] = Request.Url.ToString();

            var list = _db.ArticleContent.Where(a => a.Hashtags == "運動科學" && a.IsEnabled).ToList();

            var totalArticles = list.Count();
            var totalPages = (int)Math.Ceiling(totalArticles / (double)pageSize);

            var articles = list.Skip((page - 1) * pageSize).Take(pageSize).Select(s => new ArticleContentModel
            {
                Title = s.Title,
                EncryptedUrl = EncryptUrl(s.Title),
                ImageContent = s.ImageContent,
                Hashtags = s.Hashtags,
                ContentType = _db.ArticleCategory.FirstOrDefault(c => c.Id == s.ContentTypeId)?.CategoryName
            }).ToList();

            ViewBag.CurrentPage = page;
            ViewBag.TotalPages = totalPages;

            return View(articles);
        }

        /// <summary>
        /// 運動科技
        /// </summary>
        /// <returns></returns>
        public ActionResult sportTech(int page = 1, int pageSize = 5)
        {
            Session["ReturnUrl"] = Request.Url.ToString();

            var list = _db.ArticleContent.Where(a => a.Hashtags == "運動科技" && a.IsEnabled).ToList();

            var totalArticles = list.Count();
            var totalPages = (int)Math.Ceiling(totalArticles / (double)pageSize);

            var articles = list.Skip((page - 1) * pageSize).Take(pageSize).Select(s => new ArticleContentModel
            {
                Title = s.Title,
                EncryptedUrl = EncryptUrl(s.Title),
                ImageContent = s.ImageContent,
                Hashtags = s.Hashtags,
                ContentType = _db.ArticleCategory.FirstOrDefault(c => c.Id == s.ContentTypeId)?.CategoryName
            }).ToList();

            ViewBag.CurrentPage = page;
            ViewBag.TotalPages = totalPages;

            return View(articles);
        }

        /// <summary>
        /// 運動醫學
        /// </summary>
        /// <returns></returns>
        public ActionResult sportMedicine(int page = 1, int pageSize = 5)
        {
            Session["ReturnUrl"] = Request.Url.ToString();

            var list = _db.ArticleContent.Where(a => a.Hashtags == "運動醫學" && a.IsEnabled).ToList();

            var totalArticles = list.Count();
            var totalPages = (int)Math.Ceiling(totalArticles / (double)pageSize);

            var articles = list.Skip((page - 1) * pageSize).Take(pageSize).Select(s => new ArticleContentModel
            {
                Title = s.Title,
                EncryptedUrl = EncryptUrl(s.Title),
                ImageContent = s.ImageContent,
                Hashtags = s.Hashtags,
                ContentType = _db.ArticleCategory.FirstOrDefault(c => c.Id == s.ContentTypeId)?.CategoryName
            }).ToList();

            ViewBag.CurrentPage = page;
            ViewBag.TotalPages = totalPages;

            return View(articles);
        }

        /// <summary>
        /// 運動生理
        /// </summary>
        /// <returns></returns>
        public ActionResult sportsPhysiology(int page = 1, int pageSize = 5)
        {
            Session["ReturnUrl"] = Request.Url.ToString();

            var list = _db.ArticleContent.Where(a => a.Hashtags == "運動生理" && a.IsEnabled).ToList();

            var totalArticles = list.Count();
            var totalPages = (int)Math.Ceiling(totalArticles / (double)pageSize);

            var articles = list.Skip((page - 1) * pageSize).Take(pageSize).Select(s => new ArticleContentModel
            {
                Title = s.Title,
                EncryptedUrl = EncryptUrl(s.Title),
                ImageContent = s.ImageContent,
                Hashtags = s.Hashtags,
                ContentType = _db.ArticleCategory.FirstOrDefault(c => c.Id == s.ContentTypeId)?.CategoryName
            }).ToList();

            ViewBag.CurrentPage = page;
            ViewBag.TotalPages = totalPages;

            return View(articles);
        }

        /// <summary>
        /// 運動心理
        /// </summary>
        /// <returns></returns>
        public ActionResult sportsPsychology(int page = 1, int pageSize = 5)
        {
            Session["ReturnUrl"] = Request.Url.ToString();
            var list = _db.ArticleContent.Where(a => a.Hashtags == "運動心理" && a.IsEnabled).ToList();

            var totalArticles = list.Count();
            var totalPages = (int)Math.Ceiling(totalArticles / (double)pageSize);

            var articles = list.Skip((page - 1) * pageSize).Take(pageSize).Select(s => new ArticleContentModel
            {
                Title = s.Title,
                EncryptedUrl = EncryptUrl(s.Title),
                ImageContent = s.ImageContent,
                Hashtags = s.Hashtags,
                ContentType = _db.ArticleCategory.FirstOrDefault(c => c.Id == s.ContentTypeId)?.CategoryName
            }).ToList();

            ViewBag.CurrentPage = page;
            ViewBag.TotalPages = totalPages;

            return View(articles);
        }

        /// <summary>
        /// 體能訓練
        /// </summary>
        /// <returns></returns>
        public ActionResult physicalTraining(int page = 1, int pageSize = 5)
        {
            Session["ReturnUrl"] = Request.Url.ToString();

            var list = _db.ArticleContent.Where(a => a.Hashtags == "體能訓練" && a.IsEnabled).ToList();

            var totalArticles = list.Count();
            var totalPages = (int)Math.Ceiling(totalArticles / (double)pageSize);

            var articles = list.Skip((page - 1) * pageSize).Take(pageSize).Select(s => new ArticleContentModel
            {
                Title = s.Title,
                EncryptedUrl = EncryptUrl(s.Title),
                ImageContent = s.ImageContent,
                Hashtags = s.Hashtags,
                ContentType = _db.ArticleCategory.FirstOrDefault(c => c.Id == s.ContentTypeId)?.CategoryName
            }).ToList();

            ViewBag.CurrentPage = page;
            ViewBag.TotalPages = totalPages;

            return View(articles);
        }

        /// <summary>
        /// 運動營養
        /// </summary>
        /// <returns></returns>
        public ActionResult sportsNutrition(int page = 1, int pageSize = 5)
        {
            Session["ReturnUrl"] = Request.Url.ToString();

            var list = _db.ArticleContent.Where(a => a.Hashtags == "運動營養" && a.IsEnabled).ToList();

            var totalArticles = list.Count();
            var totalPages = (int)Math.Ceiling(totalArticles / (double)pageSize);

            var articles = list.Skip((page - 1) * pageSize).Take(pageSize).Select(s => new ArticleContentModel
            {
                Title = s.Title,
                EncryptedUrl = EncryptUrl(s.Title),
                ImageContent = s.ImageContent,
                Hashtags = s.Hashtags,
                ContentType = _db.ArticleCategory.FirstOrDefault(c => c.Id == s.ContentTypeId)?.CategoryName
            }).ToList();

            ViewBag.CurrentPage = page;
            ViewBag.TotalPages = totalPages;

            return View(articles);
        }
        #endregion

        #region 取得文件檔案
        /// <summary>
        /// 取得各文件檔案
        /// </summary>
        /// <returns></returns>
        private List<string> GetFile()
        {
            var dt = (from f in _db.FileDocument select f.DocumentName).ToList();

            return dt;
        }
        public string GetFilePath(string fileName)
        {
            var file = (from f in _db.FileDocument
                        where f.DocumentName == fileName
                        select f).FirstOrDefault();

            if (file != null)
            {
                return Url.Content($"~/storage/media/attachments/{file.DocumentName}");
            }

            return "#";
        }
        [HttpGet]
        public JsonResult GetFilePathApi(string fileName)
        {
            var filePath = GetFilePath(fileName);

            return Json(filePath, JsonRequestBehavior.AllowGet);
        }
        #endregion

        #region 公開資訊

        /// <summary>
        /// 公開資訊
        /// </summary>
        /// <returns></returns>
        public ActionResult public_info()
        {
            Session["ReturnUrl"] = Request.Url.ToString();

            return View();
        }

        /// <summary>
        /// 法規
        /// </summary>
        /// <returns></returns>
        public ActionResult regulation(int? page)
        {
            int pageSize = 5; // 每頁顯示的項目數量
            int pageNumber = (page ?? 1);

            Session["ReturnUrl"] = Request.Url.ToString();
            var uploadedFiles = _db.RegulationDocument
                        .OrderBy(d => d.UploadTime)
                        .Select(d => new RegulationDocumentModel
                        {
                            DocumentName = d.DocumentName,
                            DocumentType = d.DocumentType,
                            UploadTime = d.UploadTime ?? DateTime.MinValue,  // 處理 Nullable DateTime
                            Creator = d.Creator,
                            FileSize = d.FileSize ?? 0,  // 處理 Nullable int
                            IsActive = d.IsActive,
                        }).ToPagedList(pageNumber, pageSize);

            return View(uploadedFiles);
        }

        /// <summary>
        /// 辦法及要點
        /// </summary>
        /// <returns></returns>
        public ActionResult procedure(int? page)
        {
            int pageSize = 5; // 每頁顯示的項目數量
            int pageNumber = (page ?? 1);

            Session["ReturnUrl"] = Request.Url.ToString();
            var uploadedFiles = _db.ProcedureDocument
                        .OrderBy(d => d.UploadTime)
                        .Select(d => new ProcedureDocumentModel
                        {
                            DocumentName = d.DocumentName,
                            DocumentType = d.DocumentType,
                            UploadTime = d.UploadTime ?? DateTime.MinValue,  // 處理 Nullable DateTime
                            Creator = d.Creator,
                            FileSize = d.FileSize ?? 0,  // 處理 Nullable int
                            IsActive = d.IsActive,
                        }).ToPagedList(pageNumber, pageSize);

            return View(uploadedFiles);
        }

        /// <summary>
        /// 計畫
        /// </summary>
        /// <returns></returns>
        public ActionResult plan(int? page)
        {
            int pageSize = 5; // 每頁顯示的項目數量
            int pageNumber = (page ?? 1);

            Session["ReturnUrl"] = Request.Url.ToString();
            var uploadedFiles = _db.PlanDocument
                        .OrderBy(d => d.UploadTime)
                        .Select(d => new PlanDocumentModel
                        {
                            DocumentName = d.DocumentName,
                            DocumentType = d.DocumentType,
                            UploadTime = d.UploadTime ?? DateTime.MinValue,  // 處理 Nullable DateTime
                            Creator = d.Creator,
                            FileSize = d.FileSize ?? 0,  // 處理 Nullable int
                            IsActive = d.IsActive,
                        }).ToPagedList(pageNumber, pageSize);

            return View(uploadedFiles);
        }

        /// <summary>
        /// 預算與決算
        /// </summary>
        /// <returns></returns>
        public ActionResult budget(int? page)
        {
            int pageSize = 5; // 每頁顯示的項目數量
            int pageNumber = (page ?? 1);

            Session["ReturnUrl"] = Request.Url.ToString();
            var uploadedFiles = _db.BudgetDocument
                        .OrderBy(d => d.UploadTime)
                        .Select(d => new BudgetDocumentModel
                        {
                            DocumentName = d.DocumentName,
                            DocumentType = d.DocumentType,
                            UploadTime = d.UploadTime ?? DateTime.MinValue,  // 處理 Nullable DateTime
                            Creator = d.Creator,
                            FileSize = d.FileSize ?? 0,  // 處理 Nullable int
                            IsActive = d.IsActive,
                        }).ToPagedList(pageNumber, pageSize);

            return View(uploadedFiles);
        }

        /// <summary>
        /// 下載專區
        /// </summary>
        /// <returns></returns>
        public ActionResult download(int? page)
        {
            int pageSize = 5; // 每頁顯示的項目數量
            int pageNumber = (page ?? 1);

            Session["ReturnUrl"] = Request.Url.ToString();
            var uploadedFiles = _db.DownloadDocument
                        .OrderBy(d => d.UploadTime)
                        .Select(d => new DownloadDocumentModel
                        {
                            DocumentName = d.DocumentName,
                            DocumentType = d.DocumentType,
                            UploadTime = d.UploadTime ?? DateTime.MinValue,  // 處理 Nullable DateTime
                            Creator = d.Creator,
                            FileSize = d.FileSize ?? 0,  // 處理 Nullable int
                            IsActive = d.IsActive,
                        }).ToPagedList(pageNumber, pageSize);

            return View(uploadedFiles);
        }

        /// <summary>
        /// 採購作業實施規章
        /// </summary>
        /// <returns></returns>
        public ActionResult purchase(int? page)
        {
            int pageSize = 5; // 每頁顯示的項目數量
            int pageNumber = (page ?? 1);

            Session["ReturnUrl"] = Request.Url.ToString();
            var uploadedFiles = _db.PurchaseDocument
                        .OrderBy(d => d.UploadTime)
                        .Select(d => new PurchaseDocumentModel
                        {
                            DocumentName = d.DocumentName,
                            DocumentType = d.DocumentType,
                            UploadTime = d.UploadTime ?? DateTime.MinValue,  // 處理 Nullable DateTime
                            Creator = d.Creator,
                            FileSize = d.FileSize ?? 0,  // 處理 Nullable int
                            IsActive = d.IsActive,
                        }).ToPagedList(pageNumber, pageSize);

            return View(uploadedFiles);
        }

        /// <summary>
        /// 其他
        /// </summary>
        /// <returns></returns>
        public ActionResult other(int? page)
        {
            int pageSize = 5; // 每頁顯示的項目數量
            int pageNumber = (page ?? 1);

            Session["ReturnUrl"] = Request.Url.ToString();
            var uploadedFiles = _db.OtherDocument
                        .OrderBy(d => d.UploadTime)
                        .Select(d => new OtherDocumentModel
                        {
                            DocumentName = d.DocumentName,
                            DocumentType = d.DocumentType,
                            UploadTime = d.UploadTime ?? DateTime.MinValue,  // 處理 Nullable DateTime
                            Creator = d.Creator,
                            FileSize = d.FileSize ?? 0,  // 處理 Nullable int
                            IsActive = d.IsActive,
                        }).ToPagedList(pageNumber, pageSize);

            return View(uploadedFiles);
        }
        #endregion

        #region 性平專區
        public ActionResult GenderEquality()
        {
            Session["ReturnUrl"] = Request.Url.ToString();
            return View();
        }
        #endregion

        #region 寫入文字和圖片
        public ActionResult webContent()
        {
            return View();
        }

        [HttpPost]
        public ActionResult UploadContent(PressPageContentModel model, HttpPostedFileBase imageFile)
        {
            if (ModelState.IsValid)
            {
                var dto = new PressPageContent
                {
                    FileNo = model.FileNo,
                    TextContent = model.TextContent,
                    FileUploadTime = DateTime.Now,
                    TextUpdateTime = DateTime.Now,
                    UserAccount = model.UserAccount,
                    UserLoginTime = DateTime.Now,
                    VideoURL = model.VideoUrl,
                    VideoUpdateTime = DateTime.Now,
                    ImageContent = model.ImageContent,
                    ImageUpdateTime = DateTime.Now,
                    WebsiteURL = model.WebsiteUrl,
                    WebsiteURLUpdateTime = DateTime.Now,
                };

                if (imageFile != null && imageFile.ContentLength > 0)
                {
                    using (var reader = new System.IO.BinaryReader(imageFile.InputStream))
                    {
                        dto.ImageContent = reader.ReadBytes(imageFile.ContentLength);
                    }
                    dto.ImageUpdateTime = DateTime.Now;
                }

                _db.PressPageContent.Add(dto);
                _db.SaveChanges();

                return RedirectToAction("WebContent");
            }
            return View("WebContent", model);

        }
        #endregion

        #region 通用_好像沒用處
        //public TissController()
        //{
        //    //var db = new TISS_WebEntities();
        //    _db = new TISS_WebEntities();
        //    _contentService = new ContentService(_db);
        //}

        /// <summary>
        /// 中心介紹_存檔
        /// </summary>
        /// <param name="textContent"></param>
        /// <param name="imageSrc"></param>
        /// <returns></returns>
        //[HttpPost]
        //[ValidateInput(false)]
        //public ActionResult SaveAboutPageContent(string textContent, string imageSrc)
        //{
        //    return _contentService.SaveContent<AboutPageContentModel>(textContent, imageSrc, () => new AboutPageContentModel());
        //}

        /// <summary>
        /// 使命願景_存檔
        /// </summary>
        /// <param name="textContent"></param>
        /// <param name="imageSrc"></param>
        /// <returns></returns>
        //[HttpPost]
        //[ValidateInput(false)]
        //public ActionResult SaveObjectivesPageContent(string textContent, string imageSrc)
        //{
        //    return _contentService.SaveContent<ObjectivesPageContentModel>(textContent, imageSrc, () => new ObjectivesPageContentModel());
        //}

        /// <summary>
        /// 中心任務_存檔
        /// </summary>
        /// <param name="textContent"></param>
        /// <param name="imageSrc"></param>
        /// <returns></returns>
        //[HttpPost]
        //[ValidateInput(false)]
        //public ActionResult SaveMissionPageContent(string textContent, string imageSrc)
        //{
        //    return _contentService.SaveContent<MissionPageContentModel>(textContent, imageSrc, () => new MissionPageContentModel());
        //}

        /// <summary>
        /// 中心介紹
        /// </summary>
        /// <returns></returns>
        //[HttpGet]
        //public ActionResult GetAboutPageContent()
        //{
        //    return _contentService.GetContent<AboutPageContentModel>();
        //}

        /// <summary>
        /// 使命願景
        /// </summary>
        /// <returns></returns>
        //[HttpGet]
        //public ActionResult GetObjectivesPageContent()
        //{
        //    return _contentService.GetContent<ObjectivesPageContentModel>();
        //}

        /// <summary>
        /// 中心任務
        /// </summary>
        /// <returns></returns>
        //[HttpGet]
        //public ActionResult GetMissionPageContent()
        //{
        //    return _contentService.GetContent<MissionPageContentModel>();
        //}


        /// <summary>
        /// CEO
        /// </summary>
        /// <returns></returns>
        //[HttpGet]
        //public ActionResult GetCEOPageContent()
        //{
        //    return _contentService.GetContent<CEOPageContentModel>();
        //}

        //[HttpPost]
        //[ValidateInput(false)]
        //public ActionResult SaveCEOPageContent(string textContent, string imageSrc)
        //{
        //    return _contentService.SaveContent<CEOPageContentModel>(textContent, imageSrc ,() => new CEOPageContentModel());
        //}

        #endregion

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
        public ActionResult ArticleCreate(ArticleContent dto, HttpPostedFileBase imageFile, string tag, int contentTypeID)
        {
            if (ModelState.IsValid)
            {
                try
                {
                    if (imageFile != null && imageFile.ContentLength > 0)
                    {
                        using (var binaryReader = new System.IO.BinaryReader(imageFile.InputStream))
                        {
                            dto.ImageContent = binaryReader.ReadBytes(imageFile.ContentLength);
                        }
                    }
                    var userName = Session["UserName"] as string;
                    dto.CreateUser = userName;
                    dto.PublishedDate = DateTime.Now;
                    dto.EncryptedUrl = EncryptUrl(dto.Title);
                    dto.CreateDate = DateTime.Now;
                    dto.ClickCount = 0;
                    dto.Hashtags = tag;
                    dto.IsEnabled = true;

                    var category = _db.ArticleCategory.FirstOrDefault(c => c.Id == contentTypeID);
                    if (category != null)
                    {
                        dto.ContentTypeId = category.Id;
                        dto.ContentType = category.CategoryName;
                    }

                    //處理hashtags
                    if (!string.IsNullOrEmpty(tag))
                    {
                        var existingHashtag = _db.Hashtag.FirstOrDefault(h => h.Name == tag);

                        if (existingHashtag == null)
                        {
                            // 如果 hashtag 不存在，則新增
                            var newHashtag = new Hashtag { Name = tag };
                            _db.Hashtag.Add(newHashtag);
                        }
                    }

                    _db.ArticleContent.Add(dto);
                    _db.SaveChanges();

                    //根據ContentType進行重定向
                    string redirectAction = GetRedirectAction(dto.Hashtags);

                    return RedirectToAction(redirectAction, "Tiss");
                }
                catch (Exception ex)
                {
                    ModelState.AddModelError("", "發生錯誤，請稍後再試。");
                }
            }
            // 確保返回正確的 ViewBag.Hashtags
            ViewBag.Hashtags = new SelectList(_db.Hashtag.ToList(), "Name", "Name");
            ViewBag.Categories = new SelectList(_db.ArticleCategory.ToList(), "Id", "CategoryName");

            return View(dto);
        }

        //重定向邏輯
        private string GetRedirectAction(string contentType)
        {
            switch (contentType)
            {
                case "運動醫學": return "sportMedicine";
                case "運動科學": return "sportScience";
                case "運動科技": return "sportTech";
                case "運動營養": return "sportsNutrition";
                case "運動生理": return "sportsPhysiology";
                case "運動心理": return "sportsPsychology";
                case "行政管理處人資組": return "administrationHR";
                case "委託研究計劃": return "commissionedResearch";
                case "MOU簽署": return "MOU";
                case "運動資訊": return "sportsInfo";
                case "全部文章": return "announcement";
                case "新聞發佈": return "press";
                case "中心訊息": return "institute";
                case "徵才招募": return "recruit";
                default: return "Home";
            }
        }

        //URL加密
        private string EncryptUrl(string input)
        {
            var bytes = System.Text.Encoding.UTF8.GetBytes(input);
            var base64 = Convert.ToBase64String(bytes);
            base64 = base64.Replace("/", "-").Replace("+", "_").Replace("=", "");

            return base64;
        }

        /// <summary>
        /// 文章內容顯示
        /// </summary>
        /// <returns></returns>
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
                    { "中心消息", new List<string> { "中心成果", "新聞影音", "活動紀錄" } }
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

        /// <summary>
        /// 導回對應頁面
        /// </summary>
        /// <param name="tag"></param>
        /// <returns></returns>
        public ActionResult Redirecttag(string tag)
        {
            string actionName = GetRedirectAction(tag);
            return RedirectToAction(actionName, "Tiss");
        }

        private string GetParentDirectoryByContentTypeId(int contentTypeId)
        {
            if (contentTypeId == 1) return "科普專欄";
            if (contentTypeId == 2) return "中心消息";

            return "其他";
        }
        #endregion
    }
}