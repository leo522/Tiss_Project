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

namespace TISS_Web.Controllers
{
    public class TissController : Controller
    {
        private TISS_WebEntities _db = new TISS_WebEntities();

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


        #region 修改密碼-待修改為生成重設連結並發送電子郵件
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

        #region 上傳"法規"文件檔案
        public ActionResult UploadDocument()
        {
            var uploadedFiles = _db.RegulationDocument
                           .Select(d => new RegulationDocumentModel
                           {
                               ID = d.ID,
                               PId = d.PId,
                               DocumentName = d.DocumentName,
                               UploadTime = d.UploadTime.GetValueOrDefault(),
                               Creator = d.Creator,
                               DocumentType = d.DocumentType,
                               FileSize = d.FileSize.GetValueOrDefault(),
                               ModifiedTime = d.ModifiedTime.GetValueOrDefault(),
                               IsActive = d.IsActive,
                           })
                           .ToList();

            return View(uploadedFiles);
        }

        [HttpPost]
        public ActionResult UploadDocument(HttpPostedFileBase file)
        {
            if (file != null && file.ContentLength > 0)
            {
                try
                {
                    string fileName = Path.GetFileName(file.FileName);
                    string fileExtension = Path.GetExtension(fileName).ToLower();

                    // 檢查文件類型是否符合要求
                    if (fileExtension == ".pdf" || fileExtension == ".doc" ||
                        fileExtension == ".docx" || fileExtension == ".odt" || fileExtension == ".xls" || fileExtension == ".xlsx")
                    {
                        string filePath = Path.Combine(Server.MapPath("~/storage/media/attachments"), fileName);

                        file.SaveAs(filePath);

                        // 查詢當前DB中最大的 PId，並生成新的 PId
                        int maxPId = _db.RegulationDocument.Max(d => (int?)d.PId) ?? 0;
                        int newPId = maxPId + 1;

                        string UserId = Session["UserName"].ToString();

                        RegulationDocument document = new RegulationDocument
                        {
                            PId = newPId,
                            DocumentName = fileName,
                            UploadTime = DateTime.Now,
                            Creator = UserId,
                            DocumentType = fileExtension,
                            FileSize = file.ContentLength,
                            IsActive = true
                        };

                        _db.RegulationDocument.Add(document);
                        _db.SaveChanges();

                        ViewBag.Message = "文件上傳成功！";
                    }
                }
                catch (Exception ex)
                {
                    ViewBag.Message = "上傳文件時出錯： " + ex.Message;
                }
            }
            else
            {
                ViewBag.Message = "請選擇要上傳的文件。";
            }

            var uploadedFiles = _db.RegulationDocument
                            .OrderByDescending(d => d.UploadTime)  // 根據 UploadTime 降序排序
                            .Select(d => new RegulationDocumentModel
                            {
                                DocumentName = d.DocumentName,
                                UploadTime = d.UploadTime ?? DateTime.MinValue,  // 處理 Nullable DateTime
                                Creator = d.Creator,
                                DocumentType = d.DocumentType,
                                FileSize = d.FileSize ?? 0,  // 處理 Nullable int
                                IsActive = d.IsActive,
                            })
                            .ToList();

            return View("regulation", uploadedFiles);
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
            return View();
        }

        /// <summary>
        /// 新聞發布
        /// </summary>
        /// <returns></returns>
        public ActionResult press()
        {
            Session["ReturnUrl"] = Request.Url.ToString();
            return View();
        }

        /// <summary>
        /// 中心訊息
        /// </summary>
        /// <returns></returns>
        public ActionResult institute()
        {
            Session["ReturnUrl"] = Request.Url.ToString();
            return View();
        }

        /// <summary>
        /// 徵才招募
        /// </summary>
        /// <returns></returns>
        public ActionResult recruit()
        {
            Session["ReturnUrl"] = Request.Url.ToString();
            return View();
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

        public ActionResult Shotting()
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
        /// 儲存網頁新增的圖片
        /// </summary>
        /// <param name="textContent"></param>
        /// <param name="ImageSrc"></param>
        /// <returns></returns>
        [HttpPost]
        [ValidateInput(false)]
        public ActionResult aboutSaveContent(string textContent, string ImageSrc)
        {
            try
            {
                var userName = Session["UserName"] as string; //從Session中獲取已登錄的帳號

                byte[] imageBytes = null; // 將圖片數據轉換成byte[]

                if (!string.IsNullOrEmpty(ImageSrc))
                {
                    string[] ba64 = ImageSrc.Split(',');

                    if (ba64.Length == 2)
                    {
                        string mimeType = ba64[0].Split(':')[1].Split(';')[0];
                        string dto = ba64[1];

                        if (mimeType == "image/jpeg" || mimeType == "image/jpg" || mimeType == "image/png")
                        {
                            imageBytes = Convert.FromBase64String(dto);

                            // 檢查文件大小，確保小於等於2MB
                            if (imageBytes.Length > 2 * 1024 * 1024)
                            {
                                return Json(new { success = false, error = "圖片大小不能超過2MB" });
                            }
                        }
                        else
                        {
                            return Json(new { success = false, error = "只允許上傳jpg、jpeg或png格式的圖片" });
                        }
                    }
                }

                // 計算新的 FileNo
                int newFileNo = 1;
                var lastNo = _db.AboutPageContent.OrderByDescending(f => f.FileNo).FirstOrDefault();
                if (lastNo != null)
                {
                    newFileNo = lastNo.FileNo + 1;
                }

                var dtos = new AboutPageContent() //中心介紹
                {
                    UserAccount = userName,
                    TextContent = textContent,
                    TextUpdateTime = DateTime.Now,
                    ImageContent = imageBytes,
                    ImageUpdateTime = DateTime.Now,
                    FileNo = newFileNo
                };
                _db.AboutPageContent.Add(dtos);
                _db.SaveChanges();

                string imagePath = $"data:image/jpeg;base64,{Convert.ToBase64String(dtos.ImageContent)}";
                return Json(new { success = true, imagePath = imagePath });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, error = ex.Message });
            }
        }


        [HttpGet]
        public ActionResult aboutGetContent()
        {
            try
            {
                var content = _db.AboutPageContent
                        .OrderByDescending(c => c.FileNo)
                        .FirstOrDefault();

                if (content != null)
                {
                    // 去除多餘的空格和斷行符號
                    string textContent = System.Text.RegularExpressions.Regex.Replace(content.TextContent, @"\s+", " ").Trim();
                    string imagePath = content.ImageContent != null ? $"data:image/jpeg;base64,{Convert.ToBase64String(content.ImageContent)}" : string.Empty;

                    return Json(new
                    {
                        success = true,
                        textContent = content.TextContent,
                        imagePath = imagePath
                    }, JsonRequestBehavior.AllowGet);
                }
                else
                {
                    return Json(new { success = false, error = "讀取錯誤" }, JsonRequestBehavior.AllowGet);
                }
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

        /// <summary>
        /// 任務
        /// </summary>
        /// <returns></returns>
        public ActionResult mission()
        {
            Session["ReturnUrl"] = Request.Url.ToString();
            return View();
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

        /// <summary>
        /// 董監事
        /// </summary>
        /// <returns></returns>
        public ActionResult BOD()
        {
            Session["ReturnUrl"] = Request.Url.ToString();
            return View();
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
                var userName = Session["UserName"] as string; //從Session中獲取已登錄的帳號

                byte[] imageBytes = null; // 將圖片數據轉換成byte[]
                if (!string.IsNullOrEmpty(ImageSrc))
                {
                    string ba64 = ImageSrc.Split(',')[1];
                    imageBytes = Convert.FromBase64String(ba64);
                }
                // 計算新的 FileNo
                int newFileNo = 1;
                var lastNo = _db.CEOPageContent.OrderByDescending(f => f.FileNo).FirstOrDefault();
                if (lastNo != null && lastNo.FileNo.HasValue)
                {
                    newFileNo = lastNo.FileNo.Value + 1;
                }

                var dtos = new CEOPageContent()
                {
                    UserAccount = userName,
                    TextContent = textContent,
                    TextUpdateTime = DateTime.Now,
                    ImageContent = imageBytes,
                    ImageUpdateTime = DateTime.Now,
                    FileNo = newFileNo
                };
                _db.CEOPageContent.Add(dtos);
                _db.SaveChanges();


                //    var dto = _db.CEOPageContent.OrderByDescending(c => c.FileNo).FirstOrDefault();
                //    if (dto == null)
                //    {
                //        dto = new CEOPageContent();
                //        _db.CEOPageContent.Add(dto);
                //    }
                //    dto.UserAccount = userName;
                //    dto.TextContent = textContent;
                //    dto.TextUpdateTime = DateTime.Now;
                //    dto.ImageContent = imageBytes;
                //    dto.ImageUpdateTime = DateTime.Now;
                //    _db.SaveChanges();
                string imagePath = dtos.ImageContent != null
            ? $"data:image/jpeg;base64,{Convert.ToBase64String(dtos.ImageContent)}"
            : string.Empty;
                //return Json(new { success = true, imagePath = imagePath });
                //string imagePath = $"data:image/jpeg;base64,{Convert.ToBase64String(dtos.ImageContent)}";
                return Json(new { success = true, imagePath = imagePath });
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
                var content = _db.CEOPageContent
                        .OrderByDescending(c => c.FileNo)
                        .FirstOrDefault();

                if (content != null)
                {
                    string imagePath = content.ImageContent != null
                ? $"data:image/jpeg;base64,{Convert.ToBase64String(content.ImageContent)}"
                : string.Empty;

                    return Json(new
                    {
                        success = true,
                        textContent = content.TextContent,
                        imagePath = imagePath
                    }, JsonRequestBehavior.AllowGet);
                }
                else
                {
                    return Json(new { success = false, error = "讀取錯誤" }, JsonRequestBehavior.AllowGet);
                }
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

        #endregion

        #region 科普專欄

        /// <summary>
        /// 科普專欄
        /// </summary>
        /// <returns></returns>
        public ActionResult research()
        {
            Session["ReturnUrl"] = Request.Url.ToString();
            return View();
        }

        /// <summary>
        /// 運動科學
        /// </summary>
        /// <returns></returns>
        public ActionResult sportScience()
        {
            Session["ReturnUrl"] = Request.Url.ToString();
            return View();
        }

        /// <summary>
        /// 運動科技
        /// </summary>
        /// <returns></returns>
        public ActionResult sportTech()
        {
            Session["ReturnUrl"] = Request.Url.ToString();
            return View();
        }

        /// <summary>
        /// 運動醫學
        /// </summary>
        /// <returns></returns>
        public ActionResult sportMedicine()
        {
            Session["ReturnUrl"] = Request.Url.ToString();
            return View();
        }

        /// <summary>
        /// 運動生理
        /// </summary>
        /// <returns></returns>
        public ActionResult sportsPhysiology()
        {
            return View();
        }

        /// <summary>
        /// 運動心理
        /// </summary>
        /// <returns></returns>
        public ActionResult sportsPsychology()
        {
            return View();
        }

        /// <summary>
        /// 體能訓練
        /// </summary>
        /// <returns></returns>
        public ActionResult physicalTraining()
        {
            return View();
        }

        /// <summary>
        /// 運動營養
        /// </summary>
        /// <returns></returns>
        public ActionResult sportsNutrition()
        {
            return View();
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
                        .OrderByDescending(d => d.UploadTime)
                        .Select(d => new RegulationDocumentModel
                       {
                            DocumentName = d.DocumentName,
                            DocumentType = d.DocumentType,
                        }).ToPagedList(pageNumber, pageSize);

            return View(uploadedFiles);
        }

        /// <summary>
        /// 辦法及要點
        /// </summary>
        /// <returns></returns>
        public ActionResult procedure()
        {
            Session["ReturnUrl"] = Request.Url.ToString();
            return View();
        }

        /// <summary>
        /// 計畫
        /// </summary>
        /// <returns></returns>
        public ActionResult plan()
        {
            Session["ReturnUrl"] = Request.Url.ToString();
            return View();
        }

        /// <summary>
        /// 預算與決算
        /// </summary>
        /// <returns></returns>
        public ActionResult budget()
        {
            Session["ReturnUrl"] = Request.Url.ToString();
            return View();
        }

        /// <summary>
        /// 下載專區
        /// </summary>
        /// <returns></returns>
        public ActionResult download()
        {
            Session["ReturnUrl"] = Request.Url.ToString();
            return View();
        }

        /// <summary>
        /// 採購作業實施規章
        /// </summary>
        /// <returns></returns>
        public ActionResult purchase()
        {
            Session["ReturnUrl"] = Request.Url.ToString();
            return View();
        }

        /// <summary>
        /// 其他
        /// </summary>
        /// <returns></returns>
        public ActionResult other()
        {
            Session["ReturnUrl"] = Request.Url.ToString();
            return View();
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

        #region 通用
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

        #region 點擊率測試
        [HttpPost]
        public ActionResult IncreaseClickCount(int id)
        {
            var item = _db.ResearchProjectPageContent.FirstOrDefault(i => i.ID == id);
            if (item != null)
            {
                // 增加點擊數
                item.ClickCount++;

                // 更新資料庫
                item.UserLoginTime = DateTime.Now;
                _db.SaveChanges();

                // 回傳更新後的點擊數，供前端顯示
                return Json(new { clickCount = item.ClickCount });
            }
            else
            {
                return Json(new { error = "Item not found" });
            }
        }
        #endregion

        #region 測試點擊數
        /// <summary>
        /// // 獲取文章
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        public ActionResult ViewArticle(int id)
        {
            var dto = _db.Article.Find(id);

            if (dto == null || !dto.IsActive || (dto.UnpublishDate.HasValue && dto.UnpublishDate.Value <= DateTime.Now))
            {
                return HttpNotFound();
            }

            IncrementClickCount(id);

            var clickCount = _db.ArticleClickCount.SingleOrDefault(c => c.ArticleClickID == id)?.ClickCount ?? 0;
            ViewBag.ClickCount = clickCount;

            return View(dto);
        }

        /// <summary>
        /// 更新點擊次
        /// </summary>
        /// <param name="id"></param>
        private void IncrementClickCount(int id)
        {
            var clickCount = _db.ArticleClickCount.SingleOrDefault(c => c.ArticleClickID == id);

            if (clickCount == null)
            {
                clickCount = new ArticleClickCount
                {
                    ArticleClickID = id,
                    ClickCount = 1,
                    LastResetDate = DateTime.Now
                };
                _db.ArticleClickCount.Add(clickCount);
            }
            else
            {
                clickCount.ClickCount++;
            }
            _db.SaveChanges();
        }

        /// <summary>
        /// 重設點擊次數
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        public ActionResult ResetClickCount(int id)
        {
            var clickCount = _db.ArticleClickCount.SingleOrDefault(c => c.ArticleClickID == id);

            if (clickCount != null)
            {
                clickCount.ClickCount = 0;
                clickCount.LastResetDate = DateTime.Now;
                _db.SaveChanges();
            }
            return RedirectToAction("ViewArticle", new { id });
        }

        /// <summary>
        /// 新文章上架
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        public ActionResult PublishArticle(int id)
        {
            var article = _db.Article.Find(id);

            if (article != null)
            {
                article.IsActive = true;
                article.PublishDate = DateTime.Now;
                article.UnpublishDate = null;
                _db.SaveChanges();
            }
            return RedirectToAction("ViewArticle", new { id });
        }

        /// <summary>
        /// 文章下架
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        public ActionResult UnpublishArticle(int id)
        {
            var article = _db.Article.Find(id);
            if (article != null)
            {
                article.IsActive = false;
                article.UnpublishDate = DateTime.Now;
                _db.SaveChanges();
            }

            return RedirectToAction("ViewArticle", new { id });
        }
        #endregion
    }
}