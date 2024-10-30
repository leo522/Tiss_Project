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

namespace TISS_Web.Controllers
{
    public class TissController : Controller
    {
        private TISS_WebEntities _db = new TISS_WebEntities(); //資料庫
        private readonly string _apiKey = "AIzaSyCHWwoGD3o2uuHOQp4ejbi9wZ7yuDfLOQg"; //yt Data API KEY
        private readonly EmailService _emailService;

        #region 檔案上傳共用服務

        private readonly FileUploadService _fileUploadService;

        public TissController()
        {
            try
            {
                _fileUploadService = new FileUploadService(new TISS_WebEntities());
                _contentService = new WebContentService(new TISS_WebEntities()); //網頁內容存檔共用服務
                _emailService = new EmailService();
            }
            catch (Exception ex)
            {

                throw ex;
            }
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
            try
            {
                var user = _db.Users.FirstOrDefault(u => u.UserName == UserName);

                if (user == null)
                {
                    ViewBag.ErrorMessage = "帳號或密碼錯誤";
                    return View();
                }

                // 檢查帳號是否被鎖定
                if (user.IsLocked ?? false)
                {
                    if (user.LockoutEndTime.HasValue && DateTime.Now < user.LockoutEndTime.Value)
                    {
                        ViewBag.ErrorMessage = "帳號已被鎖住，請稍後再試。";
                        return View();
                    }
                    else
                    {
                        // 鎖定時間已過，解鎖帳號
                        user.IsLocked = false;
                        user.FailedLoginAttempts = 0;
                        user.LockoutEndTime = null;
                        _db.SaveChanges();
                    }
                }
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
                    return RedirectToAction("Home", "Tiss");
                }
                else
                {
                    // 密碼錯誤，增加失敗次數
                    user.FailedLoginAttempts++;
                    if (user.FailedLoginAttempts >= 3)
                    {
                        user.IsLocked = true;
                        user.LockoutEndTime = DateTime.Now.AddMinutes(15); // 鎖定 15 分鐘
                    }

                    _db.SaveChanges();

                    user = _db.Users.FirstOrDefault(u => u.UserName == UserName); //再次從資料庫中讀取最新的失敗次數

                    ViewBag.ErrorMessage = (user.IsLocked ?? false)
                        ? "帳號已被鎖住，請稍後再試。"
                        : string.Format("帳號或密碼錯誤，已經 {0} 次錯誤。", user.FailedLoginAttempts);

                    return View();
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("其他錯誤: " + ex.Message);
                return View("Error");
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
        public ActionResult Register(string EmployeeID, string UserName, string pwd, string Email)
        {
            try
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

                // 驗證員工編號
                var employee = _db.InternalEmployees.FirstOrDefault(e => e.EmployeeID == EmployeeID && e.IsRegistered == false);
                if (employee == null)
                {
                    ViewBag.ErrorMessage = "無效的員工編號或該員工已經註冊";
                    return View();
                }

                //Email格式驗證
                var emailRegex = new System.Text.RegularExpressions.Regex(@"^[^@\s]+@[^@\s]+\.[^@\s]+$");
                if (!emailRegex.IsMatch(Email))
                {
                    ViewBag.ErrorMessage = "無效的Email格式";
                    return View();
                }

                // 密碼加密處理
                var Pwd = ComputeSha256Hash(pwd);
                var newUser = new Users
                {
                    UserName = UserName,
                    Password = Pwd,
                    Email = Email,
                    CreatedDate = DateTime.Now,
                    LastLoginDate = DateTime.Now,
                    IsActive = true,
                    UserAccount = UserName, // 假設 UserAccount 和 UserName 相同
                    changeDate = DateTime.Now,
                    IsApproved = false // 預設未開通，註冊後需要管理員審核
                };


                _db.Users.Add(newUser);
                _db.SaveChanges();

                // 發送Email通知管理員
                var adminEmail = "00048@tiss.org.tw";
                var emailBody = $"新使用者註冊，請審核：<br/>帳號: {UserName}<br/>Email: {Email}<br/>註冊時間: {DateTime.Now}<br/><a href='{Url.Action("PendingRegistrations", "Tiss", null, Request.Url.Scheme)}'>點擊這裡審核</a>";
                SendEmail(adminEmail, "新使用者註冊通知", emailBody, null);

                // 設定訊息給 TempData
                TempData["RegisterMessage"] = "您的帳號已註冊成功，待管理員審核後將發送通知到您的Email。";

                return RedirectToAction("Login");
            }
            catch (Exception ex)
            {
                Console.WriteLine("其他錯誤: " + ex.Message);
            }
            return View();
        }
        #endregion

        #region 管理員審核
        public ActionResult PendingRegistrations()
        {
            try
            {
                var pendingUsers = _db.Users
                        .Where(u => !(u.IsApproved ?? false))
                        .Select(u => new UserModel
                        {
                            UserName = u.UserName,
                            Email = u.Email,
                            CreatedDate = DateTime.Now
                        })
                        .ToList();

                return View(pendingUsers);
            }
            catch (Exception ex)
            {
                Console.WriteLine("其他錯誤: " + ex.Message);
                return View("Error");
            }
        }

        [HttpPost]
        public ActionResult ApproveUser(string userName)
        {
            try
            {
                var user = _db.Users.SingleOrDefault(u => u.UserName == userName);

                if (user != null)
                {
                    user.IsApproved = true;

                    // 更新 InternalEmployees 表中的 IsRegistered 欄位
                    var employee = _db.InternalEmployees.FirstOrDefault(e => e.EmailAddress == user.Email);
                    if (employee != null)
                    {
                        employee.IsRegistered = true;
                    }

                    _db.SaveChanges();

                    // 發送Email通知使用者
                    var emailBody = $"您的帳號 {userName} 已通過審核，現在可以登入使用。";
                    SendEmail(user.Email, "國家運動科學中心，網頁管理者帳號審核通過通知", emailBody, null);
                }

                return RedirectToAction("PendingRegistrations");
            }
            catch (Exception ex)
            {
                Console.WriteLine("其他錯誤: " + ex.Message);
                return View("Error");
            }
        }

        [HttpPost]
        public ActionResult RejectUser(string userName)
        {
            try
            {
                var user = _db.Users.SingleOrDefault(u => u.UserName == userName);
                if (user != null)
                {
                    _db.Users.Remove(user);
                    _db.SaveChanges();
                }
                return RedirectToAction("PendingRegistrations");
            }
            catch (Exception ex)
            {
                Console.WriteLine("其他錯誤: " + ex.Message);
                return View("Error");
            }
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

        #region 忘記密碼
        public ActionResult ForgotPassword()
        {
            return View();
        }

        // 發送重置密碼鏈接
        [HttpPost]
        public ActionResult SendResetLink(string Email)
        {
            try
            {
                var user = _db.Users.FirstOrDefault(u => u.Email == Email);

                if (user == null)
                {
                    ViewBag.ErrorMessage = "此Email尚未註冊";
                    return View("ForgotPassword");
                }

                // 生成重置密碼令牌（這裡使用 Guid 作為示例）
                var resetToken = Guid.NewGuid().ToString();

                // 保存重置令牌和過期時間
                var resetPW = new PasswordResetRequests
                {
                    Email = Email,
                    Token = resetToken,
                    ExpiryDate = DateTime.Now.AddMinutes(5), // 設定有效時間為5分鐘
                    UserAccount = user.UserName,
                    changeDate = DateTime.Now
                };
                _db.PasswordResetRequests.Add(resetPW);
                _db.SaveChanges();

                // 發送重置密碼郵件
                var resetLink = Url.Action("ResetPassword", "Tiss", new { token = resetToken }, Request.Url.Scheme);

                var emailBody = $"請點擊以下連結重置您的密碼：{resetLink}，連結有效時間為5分鐘";

                SendEmail(Email, "重置密碼", emailBody, null);

                ViewBag.Message = "重置密碼連結已發送至您的郵箱";
                return View("ForgotPassword");
            }
            catch (Exception ex)
            {
                Console.WriteLine("其他錯誤: " + ex.Message);
                return View("Error");
            }
        }

        //重置密碼頁面
        public ActionResult ResetPassword(string token)
        {
            try
            {
                // 查找重置請求
                var resetRequest = _db.PasswordResetRequests.SingleOrDefault(r => r.Token == token && r.ExpiryDate > DateTime.Now);

                if (resetRequest == null)
                {
                    ViewBag.ErrorMessage = "無效或過期的要求";
                    return View("Error");
                }

                // 初始化 ResetPasswordViewModel 並傳遞到視圖
                var model = new ResetPasswordViewModel
                {
                    Token = token
                };

                return View(model);
            }
            catch (Exception ex)
            {
                Console.WriteLine("其他錯誤: " + ex.Message);
                return View("Error");
            }

        }

        // 處理重置密碼
        [HttpPost]
        public ActionResult ResetPassword(ResetPasswordViewModel model)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    // 顯示驗證錯誤
                    var errors = ModelState.Values.SelectMany(v => v.Errors);
                    foreach (var error in errors)
                    {
                        Console.WriteLine(error.ErrorMessage);
                    }
                    return View(model);
                }

                // 根據 Token 查找重置請求
                var resetRequest = _db.PasswordResetRequests
                    .FirstOrDefault(r => r.Token == model.Token && r.ExpiryDate > DateTime.Now);

                if (resetRequest == null)
                {
                    ViewBag.ErrorMessage = "無效或過期的要求";
                    return View("Error");
                }

                // 根據 Email 查找用戶
                var user = _db.Users
                    .FirstOrDefault(u => u.Email == resetRequest.Email);

                if (user == null)
                {
                    ViewBag.ErrorMessage = "無效的帳號";
                    return View("Error");
                }

                // 更新用戶的密碼
                user.Password = ComputeSha256Hash(model.NewPassword);
                //user.changeDate = DateTime.Now;

                // 更新 PasswordResetRequest 表中的 UserAccount 和 ChangeDate
                resetRequest.UserAccount = user.UserName;
                resetRequest.changeDate = DateTime.Now;

                // 刪除重置請求
                _db.PasswordResetRequests.Remove(resetRequest);
            }
            catch (Exception ex)
            {
                Console.WriteLine("其他錯誤: " + ex.Message);
                return View("Error");
            }

            try
            {
                // 儲存變更到資料庫
                _db.SaveChanges();
            }
            catch (DbEntityValidationException ex)
            {
                foreach (var validationErrors in ex.EntityValidationErrors)
                {
                    foreach (var validationError in validationErrors.ValidationErrors)
                    {
                        // 記錄錯誤信息
                        Console.WriteLine($"Property: {validationError.PropertyName}, Error: {validationError.ErrorMessage}");
                    }
                }
                throw;
            }

            ViewBag.Message = "您的密碼已成功重置";
            return RedirectToAction("Login");
        }

        #endregion

        #region 郵件發送方法
        public void SendEmail(string toEmail, string subject, string body, string attachmentPath)
        {
            var gmailService = new GmailApiService(); //使用 Gmail API 發送郵件

            //string attachmentBase64 = null; //如果有附件，則處理附件
            //if (!string.IsNullOrEmpty(attachmentPath))
            //{
            //    // 轉換附件為 base64 編碼
            //    byte[] attachmentBytes = System.IO.File.ReadAllBytes(attachmentPath);
            //    attachmentBase64 = Convert.ToBase64String(attachmentBytes);
            //}

            //發送郵件
            try
            {
                gmailService.SendEmail(toEmail, subject, body, attachmentPath);
                Console.WriteLine("郵件已成功發送");
                LogEmail(toEmail, subject, body, "Sent", null); // 記錄成功發送的郵件
            }
            catch (Exception ex)
            {
                Console.WriteLine($"錯誤: {ex.Message}");
                LogEmail(toEmail, subject, body, "Failed", ex.Message); // 記錄錯誤
            }



            //var mail = new MailMessage();
            //mail.From = new MailAddress("00048@tiss.org.tw");
            //mail.To.Add(toEmail);
            //mail.Subject = subject;
            //mail.Body = body;
            //mail.IsBodyHtml = true; // 支援 HTML 格式

            // 如果有附件，則加入附件
            //if (!string.IsNullOrEmpty(attachmentPath))
            //{
            //    Attachment attachment = new Attachment(attachmentPath);
            //    mail.Attachments.Add(attachment);
            //}

            //using (var smtpClient = new SmtpClient())
            //{
            //    smtpClient.Host = "smtp.gmail.com"; // MX Mail Server 的主機名稱
            //    smtpClient.Port = 587;                    // MX Mail Server 使用的端口，通常為 25 或特定端口
            //    smtpClient.EnableSsl = true;            // 根據伺服器要求設置 SSL
            //    smtpClient.Credentials = new NetworkCredential("00048@tiss.org.tw", "lctm hhfh bubx lwda");

            //    try
            //    {
            //        smtpClient.Send(mail);
            //        Console.WriteLine("郵件已成功發送");
            //        LogEmail(toEmail, subject, body, "Sent", null); // 記錄成功發送的郵件
            //    }
            //    catch (SmtpException smtpEx)
            //    {
            //        Console.WriteLine($"SMTP 錯誤: {smtpEx.Message}");
            //        Console.WriteLine(smtpEx.ToString());
            //        LogEmail(toEmail, subject, body, "Failed", smtpEx.Message); // 記錄 SMTP 錯誤
            //    }
            //    catch (Exception ex)
            //    {
            //        Console.WriteLine($"其他錯誤: {ex.Message}");
            //        Console.WriteLine(ex.ToString());
            //        LogEmail(toEmail, subject, body, "Failed", ex.Message); // 記錄其他錯誤
            //    }

            //}
        }
        private void LogEmail(string recipientEmail, string subject, string body, string status, string errorMessage)
        {
            var emailLog = new EmailLogs
            {
                RecipientEmail = recipientEmail,
                Subject = subject,
                Body = body,
                SentDate = DateTime.Now,
                Status = status,
                ErrorMessage = errorMessage
            };

            _db.EmailLogs.Add(emailLog);
            _db.SaveChanges();
        }

        #endregion

        #region 公開資訊_上傳文件檔案

        // 上傳計畫文件
        [HttpPost]
        public ActionResult UploadPlanDocument(HttpPostedFileBase file, int? page)
        {
            try
            {
                _fileUploadService.UploadFile(file, "PlanDocument");
                return RedirectToAction("Plan", new { page });
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

        //上傳法規文件
        [HttpPost]
        public ActionResult UploadRegulationDocument(HttpPostedFileBase file, int? page)
        {
            try
            {
                _fileUploadService.UploadFile(file, "RegulationDocument");
                return RedirectToAction("Regulation", new { page });
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

        //上傳辦法及要點文件
        [HttpPost]
        public ActionResult UploadProcedureDocument(HttpPostedFileBase file, int? page)
        {
            try
            {
                _fileUploadService.UploadFile(file, "ProcedureDocument");
                return RedirectToAction("Procedure", new { page });
            }
            catch (Exception ex)
            {

                throw ex;
            }
        }

        // 上傳下載專區文件
        [HttpPost]
        public ActionResult UploadDownloadDocument(HttpPostedFileBase file, int? page)
        {
            try
            {
                _fileUploadService.UploadFile(file, "DownloadDocument");
                return RedirectToAction("download", new { page });
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

        // 上傳預算與決算文件
        [HttpPost]
        public ActionResult UploadBudgetDocument(HttpPostedFileBase file, int? page)
        {
            try
            {
                _fileUploadService.UploadFile(file, "BudgetDocument");
                return RedirectToAction("budget", new { page });
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

        // 上傳其他文件
        [HttpPost]
        public ActionResult UploadOtherDocument(HttpPostedFileBase file, int? page)
        {
            try
            {
                _fileUploadService.UploadFile(file, "OtherDocument");
                return RedirectToAction("other", new { page });
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

        // 上傳採購作業實施規章文件
        [HttpPost]
        public ActionResult UploadPurchaseDocument(HttpPostedFileBase file, int? page)
        {
            try
            {
                _fileUploadService.UploadFile(file, "PurchaseDocument");
                return RedirectToAction("purchase", new { page });
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

        //上傳性別平等專區文件
        [HttpPost]
        public ActionResult UploadGenderEqualityDocument(HttpPostedFileBase file, string url)
        {
            try
            {
                _fileUploadService.UploadFile(file, "GenderEqualityDocument");
                return RedirectToAction("GenderEquality");
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

        #endregion

        #region 首頁
        /// <summary>
        /// 首頁
        /// </summary>
        /// <returns></returns>
        public ActionResult Home()
        {
            try
            {
                Session["ReturnUrl"] = Request.Url.ToString();

                //首頁文章內容
                var dtos = _db.ArticleContent
                    .Where(a => a.IsPublished.HasValue && a.IsPublished.Value && a.IsEnabled == true)
                    .OrderByDescending(a => a.PublishedDate)
                    .Select(a => new ArticleContentModel
                    {
                        Title = a.Title,
                        ImageContent = a.ImageContent,
                        ContentType = a.ContentType,
                        Hashtags = a.Hashtags,
                        EncryptedUrl = a.EncryptedUrl,
                        PublishedDate = a.PublishedDate.HasValue ? a.PublishedDate.Value : DateTime.MinValue,

                    }).Take(4).ToList();

                var dto = _db.ArticleContent
                        .Where(a => a.ContentType == "中心訊息" && a.IsPublished.HasValue && a.IsPublished.Value && a.IsEnabled == true)
                        .OrderByDescending(a => a.CreateDate)
                        .Select(a => new ArticleContentModel
                        {
                            Title = a.Title,
                            ImageContent = a.ImageContent,
                            ContentType = a.ContentType,
                            Hashtags = a.Hashtags,
                            EncryptedUrl = a.EncryptedUrl,
                            PublishedDate = a.PublishedDate.HasValue ? a.PublishedDate.Value : DateTime.MinValue,
                        })
                        .FirstOrDefault(); // 取得最新的專欄文章

                var latestArticle = dtos.FirstOrDefault();
                var otherArticles = dtos.Skip(1).ToList();

                var viewModel = new HomeViewModel //首頁的部份視圖
                {
                    LatestArticle = dto,
                    OtherArticles = otherArticles,
                    Videos = null // 不需要再從這裡獲取影片數據
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
                    }).Take(4).ToList();

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
                //return match.Success ? match.Value : string.Empty;
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

        #region 最新消息-中心公告

        /// <summary>
        /// 中心公告
        /// </summary>
        /// <returns></returns>
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
            catch (Exception ex)
            {
                throw ex;
            }
        }

        /// <summary>
        /// 新聞發布
        /// </summary>
        /// <returns></returns>
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
            catch (Exception ex)
            {
                throw ex;
            }
        }

        /// <summary>
        /// 中心訊息
        /// </summary>
        /// <returns></returns>
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
            catch (Exception ex)
            {
                throw ex;
            }
        }

        /// <summary>
        /// 徵才招募
        /// </summary>
        /// <returns></returns>
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
            catch (Exception ex)
            {
                throw ex;
            }
        }

        /// <summary>
        /// 委託研究計劃
        /// </summary>
        /// <returns></returns>
        //public ActionResult researchProject()
        //{
        //    Session["ReturnUrl"] = Request.Url.ToString();


        //    int newsItemId = 1;
        //    var item = _db.ResearchProjectPageContent.FirstOrDefault(i => i.ID == newsItemId);
        //    if (item != null)
        //    {
        //        // 傳遞點擊數給前端
        //        ViewBag.ClickCount = item.ClickCount;
        //    }
        //    else
        //    {
        //        // 如果找不到對應的新聞項目，點擊數為 0
        //        ViewBag.ClickCount = 0;
        //    }

        //    return View();
        //}

        /// <summary>
        /// 政府網站服務管理規範
        /// </summary>
        /// <returns></returns>
        //public ActionResult GovernmentWebsite()
        //{
        //    Session["ReturnUrl"] = Request.Url.ToString();
        //    return View();
        //}

        #endregion

        #region 影音專區
        /// <summary>
        /// 影音專區
        /// </summary>
        /// <returns></returns>
        public ActionResult video(int page = 1, int pageSize = 9)
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

        /// <summary>
        /// 中心成果
        /// </summary>
        /// <returns></returns>
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

        /// <summary>
        /// 新聞影音
        /// </summary>
        /// <returns></returns>
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

        /// <summary>
        /// 活動紀錄
        /// </summary>
        /// <returns></returns>
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

        #region 中心介紹
        /// <summary>
        /// 中心介紹
        /// </summary>
        /// <returns></returns>
        public ActionResult about()
        {
            try
            {
                ViewBag.Title = "中心介紹";
                Session["ReturnUrl"] = Request.Url.ToString();
                return View();
            }
            catch (Exception ex)
            {
                throw ex;
            }
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
            try
            {
                ViewBag.Title = "使命、願景";
                Session["ReturnUrl"] = Request.Url.ToString();
                return View();
            }
            catch (Exception ex)
            {
                throw ex;
            }
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
        /// 中心任務
        /// </summary>
        /// <returns></returns>
        public ActionResult mission()
        {
            try
            {
                ViewBag.Title = "中心任務";
                Session["ReturnUrl"] = Request.Url.ToString();
                return View();
            }
            catch (Exception ex)
            {
                throw ex;
            }
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
            try
            {
                ViewBag.Title = "組織概況";
                Session["ReturnUrl"] = Request.Url.ToString();
                return View();
            }
            catch (Exception ex)
            {
                throw ex;
            }
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
            try
            {
                ViewBag.Title = "第1屆 董監事";
                Session["ReturnUrl"] = Request.Url.ToString();
                return View();
            }
            catch (Exception ex)
            {
                throw ex;
            }
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
            try
            {
                ViewBag.Title = "執行長";
                Session["ReturnUrl"] = Request.Url.ToString();
                return View();
            }
            catch (Exception ex)
            {
                throw ex;
            }
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
            try
            {
                ViewBag.Title = "單位介紹";
                Session["ReturnUrl"] = Request.Url.ToString();
                return View();
            }
            catch (Exception ex)
            {
                throw ex;
            }
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
            catch (Exception ex)
            {
                throw ex;
            }
        }

        /// <summary>
        /// 運動科學
        /// </summary>
        /// <returns></returns>
        public ActionResult sportScience(int page = 1, int pageSize = 9)
        {
            try
            {
                ViewBag.Title = "運動科學";
                Session["ReturnUrl"] = Request.Url.ToString();

                page = Math.Max(1, page); //確保頁碼至少為 1

                var list = _db.ArticleContent
                    .Where(a => a.Hashtags == "運動科學" || a.Hashtags == "運動管理" && a.IsEnabled)
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
            catch (Exception ex)
            {
                throw ex;
            }
        }

        /// <summary>
        /// 運動科技
        /// </summary>
        /// <returns></returns>
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
            catch (Exception ex)
            {
                throw ex;
            }
        }

        /// <summary>
        /// 運動醫學
        /// </summary>
        /// <returns></returns>
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
            catch (Exception ex)
            {
                throw ex;
            }
        }

        /// <summary>
        /// 運動生理
        /// </summary>
        /// <returns></returns>
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
            catch (Exception ex)
            {
                throw ex;
            }
        }

        /// <summary>
        /// 運動心理
        /// </summary>
        /// <returns></returns>
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
            catch (Exception ex)
            {
                throw ex;
            }
        }

        /// <summary>
        /// 體能訓練
        /// </summary>
        /// <returns></returns>
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
            catch (Exception ex)
            {
                throw ex;
            }

        }

        /// <summary>
        /// 運動營養
        /// </summary>
        /// <returns></returns>
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
            catch (Exception ex)
            {
                throw ex;
            }
        }

        /// <summary>
        /// 兒少科普
        /// </summary>
        /// <returns></returns>
        public ActionResult childrenScience(int page = 1, int pageSize = 9)
        {
            try
            {
                ViewBag.Title = "兒少科普";
                Session["ReturnUrl"] = Request.Url.ToString();

                page = Math.Max(1, page); //確保頁碼至少為 1

                var list = _db.ArticleContent
                    .Where(a => a.Hashtags == "兒少科普" && a.IsEnabled)
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
            catch (Exception ex)
            {
                throw ex;
            }
        }

        /// <summary>
        /// 科普海報下載專區
        /// </summary>
        /// <returns></returns>
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
            catch (Exception ex)
            {
                throw ex;
            }
        }
        #endregion

        #region 取得文件檔案
        /// <summary>
        /// 取得各文件檔案
        /// </summary>
        /// <returns></returns>
        //private List<string> GetFile()
        //{
        //    var dt = (from f in _db.FileDocument
        //              where f.IsEnabled == true
        //              select f.DocumentName).ToList();

        //    return dt;
        //}

        //public string GetFilePath(string fileName)
        //{
        //    var file = (from f in _db.FileDocument
        //                where f.DocumentName == fileName && f.IsEnabled == true
        //                select f).FirstOrDefault();

        //    if (file != null)
        //    {
        //        return Url.Content($"~/storage/media/attachments/{file.DocumentName}");
        //    }

        //    return "#";
        //}

        //[HttpGet]
        //public JsonResult GetFilePathApi(string fileName)
        //{
        //    var filePath = GetFilePath(fileName);

        //    return Json(filePath, JsonRequestBehavior.AllowGet);
        //}

        //public ActionResult DownloadFile(int documentId, string tableName)
        //{
        //    try
        //    {
        //        switch (tableName)
        //        {
        //            case "RegulationDocument":
        //                var regulationDocument = _db.RegulationDocument.Find(documentId);
        //                if (regulationDocument != null)
        //                {
        //                    var contentType = GetContentType(regulationDocument.DocumentName);
        //                    return File(regulationDocument.FileData, contentType, regulationDocument.DocumentName);
        //                }
        //                break;

        //            case "ProcedureDocument":
        //                var procedureDocument = _db.ProcedureDocument.Find(documentId);
        //                if (procedureDocument != null)
        //                {
        //                    var contentType = GetContentType(procedureDocument.DocumentName);
        //                    return File(procedureDocument.FileData, contentType, procedureDocument.DocumentName);
        //                }
        //                break;

        //            case "PlanDocument":
        //                var planDocument = _db.PlanDocument.Find(documentId);
        //                if (planDocument != null)
        //                {
        //                    var contentType = GetContentType(planDocument.DocumentName);
        //                    return File(planDocument.FileData, contentType, planDocument.DocumentName);
        //                }
        //                break;

        //            case "BudgetDocument":
        //                var budgetDocument = _db.BudgetDocument.Find(documentId);
        //                if (budgetDocument != null)
        //                {
        //                    var contentType = GetContentType(budgetDocument.DocumentName);
        //                    return File(budgetDocument.FileData, contentType, budgetDocument.DocumentName);
        //                }
        //                break;

        //            case "DownloadDocument":
        //                var downloadDocument = _db.DownloadDocument.Find(documentId);
        //                if (downloadDocument != null)
        //                {
        //                    var contentType = GetContentType(downloadDocument.DocumentName);
        //                    return File(downloadDocument.FileData, contentType, downloadDocument.DocumentName);
        //                }
        //                break;

        //            case "OtherDocument":
        //                var otherDocument = _db.OtherDocument.Find(documentId);
        //                if (otherDocument != null)
        //                {
        //                    var contentType = GetContentType(otherDocument.DocumentName);
        //                    return File(otherDocument.FileData, contentType, otherDocument.DocumentName);
        //                }
        //                break;

        //            case "GenderEqualityDocument":
        //                var genderEqualityDocument = _db.GenderEqualityDocument.Find(documentId);
        //                if (genderEqualityDocument != null)
        //                {
        //                    return File(genderEqualityDocument.FileData, "application/octet-stream", genderEqualityDocument.DocumentName);
        //                }
        //                break;

        //            // 根據需要添加其他表的處理邏輯

        //            default:
        //                return HttpNotFound();
        //        }

        //        return HttpNotFound();
        //    }
        //    catch (Exception ex)
        //    {
        //        throw ex;
        //    }
        //}


        #endregion

        #region 公開資訊

        /// <summary>
        /// 公開資訊
        /// </summary>
        /// <returns></returns>
        public ActionResult public_info(int page = 1, int pageSize = 7)
        {
            try
            {
                ViewBag.Title = "公開資訊";
                Session["ReturnUrl"] = Request.Url.ToString();

                page = Math.Max(1, page); //確保頁碼至少為 1

                var list = _db.Documents.Where(d => d.IsActive && d.Category == "Regulation")
                                .OrderByDescending(d => d.UploadTime)
                                .ToList();

                //計算總數和總頁數
                var totalDocuments = list.Count();
                var totalPages = (int)Math.Ceiling(totalDocuments / (double)pageSize);

                page = Math.Min(page, totalPages); //確保頁碼不超過最大頁數

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

                return View(dtos);
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

        /// <summary>
        /// 法規
        /// </summary>
        /// <returns></returns>
        public ActionResult regulation(int page = 1, int pageSize = 7)
        {
            try
            {
                ViewBag.Title = "法規";
                Session["ReturnUrl"] = Request.Url.ToString();

                page = Math.Max(1, page); //確保頁碼至少為 1

                var list = _db.Documents.Where(d => d.IsActive && d.Category == "Regulation")
                                .OrderByDescending(d => d.UploadTime)
                                .ToList();

                //計算總數和總頁數
                var totalDocuments = list.Count();
                var totalPages = (int)Math.Ceiling(totalDocuments / (double)pageSize);

                page = Math.Min(page, totalPages); //確保頁碼不超過最大頁數

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

                return View(dtos);
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

        /// <summary>
        /// 辦法及要點
        /// </summary>
        /// <returns></returns>
        public ActionResult procedure(int page = 1, int pageSize = 10)
        {
            try
            {
                ViewBag.Title = "辦法及要點";
                Session["ReturnUrl"] = Request.Url.ToString();

                page = Math.Max(1, page); //確保頁碼至少為 1

                var list = _db.Documents.Where(d => d.Category == "Procedure" && d.IsActive)
                                        .OrderByDescending(d => d.UploadTime).ToList();

                //計算總數和總頁數
                var totalDocuments = list.Count();
                var totalPages = (int)Math.Ceiling(totalDocuments / (double)pageSize);

                page = Math.Min(page, totalPages); //確保頁碼不超過最大頁數

                var dtos = list.Skip((page - 1) * pageSize).Take(pageSize).Select(d => new DocumentModel
                {
                    DocumentID = d.DocumentID,
                    DocumentName = d.DocumentName,
                    DocumentType = d.DocumentType,
                    UploadTime = d.UploadTime,
                    Creator = d.Creator,
                    FileSize = d.FileSize,
                    IsActive = d.IsActive
                }).ToList();

                ViewBag.CurrentPage = page;
                ViewBag.TotalPages = totalPages;

                return View(dtos);
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

        /// <summary>
        /// 計畫
        /// </summary>
        /// <returns></returns>
        public ActionResult plan(int page = 1, int pageSize = 10)
        {
            try
            {
                ViewBag.Title = "計畫";
                Session["ReturnUrl"] = Request.Url.ToString();

                page = Math.Max(1, page); //確保頁碼至少為 1

                var list = _db.Documents.Where(d => d.IsActive && d.Category == "Plan")
                                .OrderByDescending(d => d.UploadTime)
                                .ToList();

                //計算總數和總頁數
                var totalDocuments = list.Count();
                var totalPages = (int)Math.Ceiling(totalDocuments / (double)pageSize);

                page = Math.Min(page, totalPages); //確保頁碼不超過最大頁數

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

                return View(dtos);
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

        /// <summary>
        /// 預算與決算
        /// </summary>
        /// <returns></returns>
        public ActionResult budget(int page = 1, int pageSize = 10)
        {
            try
            {
                ViewBag.Title = "預算與決算";
                Session["ReturnUrl"] = Request.Url.ToString();

                page = Math.Max(1, page); //確保頁碼至少為 1

                var list = _db.Documents.Where(d => d.IsActive && d.Category == "Budget")
                                .OrderByDescending(d => d.UploadTime)
                                .ToList();

                //計算總數和總頁數
                var totalDocuments = list.Count();
                var totalPages = (int)Math.Ceiling(totalDocuments / (double)pageSize);

                page = Math.Min(page, totalPages); //確保頁碼不超過最大頁數

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

                return View(dtos);
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

        /// <summary>
        /// 下載專區
        /// </summary>
        /// <returns></returns>
        public ActionResult download(int page = 1, int pageSize = 10)
        {
            try
            {
                ViewBag.Title = "下載專區";
                Session["ReturnUrl"] = Request.Url.ToString();

                page = Math.Max(1, page); //確保頁碼至少為 1

                var list = _db.Documents.Where(d => d.IsActive && d.Category == "Download").OrderByDescending(d => d.UploadTime)
                    .ToList();

                //計算總數和總頁數
                var totalDocuments = list.Count();
                var totalPages = (int)Math.Ceiling(totalDocuments / (double)pageSize);

                page = Math.Min(page, totalPages);

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

                return View(dtos);
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

        /// <summary>
        /// 採購作業實施規章
        /// </summary>
        /// <returns></returns>
        public ActionResult purchase(int page = 1, int pageSize = 5)
        {
            try
            {
                ViewBag.Title = "採購作業實施規章";
                Session["ReturnUrl"] = Request.Url.ToString();

                page = Math.Max(1, page); //確保頁碼至少為 1

                var list = _db.Documents.Where(d => d.Category == "Purchase" && d.IsActive)
                                .OrderByDescending(d => d.UploadTime).ToList();

                //計算總數和總頁數
                var totalDocuments = list.Count();
                var totalPages = (int)Math.Ceiling(totalDocuments / (double)pageSize);

                page = Math.Min(page, totalPages); //確保頁碼不超過最大頁數

                var dtos = list.Skip((page - 1) * pageSize).Take(pageSize).Select(d => new DocumentModel
                {
                    DocumentID = d.DocumentID,
                    DocumentName = d.DocumentName,
                    DocumentType = d.DocumentType,
                    UploadTime = d.UploadTime,
                    Creator = d.Creator,
                    FileSize = d.FileSize,
                    IsActive = d.IsActive
                }).ToList();

                ViewBag.CurrentPage = page;
                ViewBag.TotalPages = totalPages;

                return View(dtos);
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

        /// <summary>
        /// 其他
        /// </summary>
        /// <returns></returns>
        public ActionResult other(int page = 1, int pageSize = 10)
        {
            try
            {
                ViewBag.Title = "其他";
                Session["ReturnUrl"] = Request.Url.ToString();

                page = Math.Max(1, page); //確保頁碼至少為 1

                var list = _db.Documents.Where(d => d.Category == "Other" && d.IsActive)
                                .OrderByDescending(d => d.UploadTime).ToList();

                //計算總數和總頁數
                var totalDocuments = list.Count();
                var totalPages = (int)Math.Ceiling(totalDocuments / (double)pageSize);

                page = Math.Min(page, totalPages); //確保頁碼不超過最大頁數

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

                return View(dtos);
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

        #endregion

        #region 性別平等專區

        public ActionResult GenderEquality()
        {
            try
            {
                ViewBag.Title = "性別平等專區";
                Session["ReturnUrl"] = Request.Url.ToString();

                var dto = _db.GenderEqualityDocument.Where(d => d.IsActive)
                        .OrderByDescending(d => d.UploadTime) // 按 UploadTime 降序排序
                        .ToList(); //獲取資料列表

                var Websites = _db.DomainsURL.Where(d => d.IsActive).ToList(); // 獲取相關網站資料

                ViewBag.Websites = Websites; // 將資料傳遞到視圖

                return View(dto);
            }
            catch (Exception ex)
            {

                throw ex;
            }
        }

        #endregion

        #region 贊助專區
        public ActionResult SponsorArea()
        {
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
        public ActionResult ArticleCreate(ArticleContent dto, HttpPostedFileBase imageFile, string[] tags, int contentTypeID, HttpPostedFileBase documentFile, string documentCategory)
        {
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
                    //dto.Hashtags = tag;
                    dto.IsEnabled = true;
                    dto.IsPublished = true;

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
                                // 如果 hashtag 不存在，則新增
                                var newHashtag = new Hashtag { Name = t };
                                _db.Hashtag.Add(newHashtag);
                            }
                        }
                    }
                    _db.ArticleContent.Add(dto);
                    _db.SaveChanges();

                    // 6. 儲存文件至 documents 表並關聯文章ID
                    if (documentFile != null && documentFile.ContentLength > 0)
                    {
                        var fileUploadResult = SaveDocumentFile(documentFile, dto.Id, documentCategory);
                        if (!fileUploadResult)
                        {
                            ModelState.AddModelError("", "文件上傳失敗。");
                        }
                    }

                    //根據ContentType進行重定向
                    string redirectAction = GetRedirectAction(dto.Hashtags);

                    return RedirectToAction(redirectAction, "Tiss");
                }
                catch (Exception)
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
                case "行政管理處人資組": return "recruit";
                case "委託研究計劃": return "announcement";
                case "MOU簽署": return "announcement";
                case "運動資訊": return "sportsInfo";
                case "全部文章": return "announcement";
                case "新聞發佈": return "press";
                case "中心訊息": return "institute";
                case "徵才招募": return "recruit";
                case "中心成果": return "achievement";
                case "新聞影音": return "news";
                case "活動紀錄": return "activityRecord";
                case "兒少科普": return "childrenScience";
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

        
        #endregion

        #region 文章內容顯示
        /// <summary>
        /// 增加alt屬性
        /// </summary>
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

        /// <summary>
        /// 文章內容URL解密
        /// </summary>
        /// <param name="encryptedUrl"></param>
        /// <returns></returns>
        private string DecryptUrl(string encryptedUrl)
        {
            try
            {
                //替換Base64 URL 安全字符如果已被替換）
                encryptedUrl = encryptedUrl.Replace("-", "/").Replace("_", "+");
                // 補齊Base64 字符串的填充
                int mod4 = encryptedUrl.Length % 4;
                if (mod4 > 0)
                {
                    encryptedUrl += new string('=', 4 - mod4);
                }

                // 將Base64字串解碼為字節數組
                var bytes = Convert.FromBase64String(encryptedUrl);
                var decodedString = System.Text.Encoding.UTF8.GetString(bytes);

                return decodedString;
            }
            catch (FormatException)
            {
                return string.Empty; // 或者根據需求返回 null 或拋出異常
            }
        }

        /// <summary>
        /// 文章內容附件檔案名稱顯示
        /// </summary>
        /// <param name="content"></param>
        /// <returns></returns>
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

        /// <summary>
        /// 文章內容顯示
        /// </summary>
        /// <returns></returns>
        public ActionResult ViewArticle(string encryptedUrl)
        {
            try
            {
                var decryptedUrl = DecryptUrl(encryptedUrl);
                if (string.IsNullOrEmpty(decryptedUrl))
                {
                    Console.WriteLine("[ERROR] 解密後的URL為空");
                    return RedirectToAction("Error404", "Error");
                }

                var article = _db.ArticleContent.FirstOrDefault(a => a.Title == decryptedUrl);

                if (article == null)
                {
                    Console.WriteLine($"[ERROR] 找不到文章: {decryptedUrl}");
                    return RedirectToAction("Error404", "Error");
                }

                article.ClickCount += 1; //增加點閱率次數
                //article.ContentBody = AddAccessibilityAttributes(article.ContentBody); //增加無障礙屬性
                //article.ContentBody = EnsureImageAltAttribute(article.ContentBody); // 在渲染之前，確保img標籤都包含 alt 屬性

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
                        ArticleId = d.ArticleId  ?? 0// 可用於下載鏈接
                    }).ToList();

                var model = new ArticleViewModel
                {
                    Article = article,
                    AssociatedDocuments = associatedDocuments
                };

                //// 將關聯文件資料存入 ViewBag，以供 View 中使用
                //ViewBag.AssociatedDocuments = associatedDocuments;

                ViewBag.DisplayHashtags = article.Hashtags?.Split(',').ToList(); //處理多個Hashtags顯示

                // 查找同一標籤下的上一篇和下一篇文章
                var articlesWithSameTag = _db.ArticleContent
                    .Where(a => a.Hashtags.Contains(article.Hashtags) && a.IsEnabled) // 使用 Contains 來匹配部分標籤
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

                ViewBag.Title = $"{article.Title}";// 設定網頁描述性的標題

                var menuList = new Dictionary<string, string> //子主題連結
                {
                    { "運動醫學", "/Tiss/sportMedicine" },
                    { "運動科技", "/Tiss/sportTech" },
                    { "運動科學", "/Tiss/sportScience" },
                    { "運動生理", "/Tiss/sportsPhysiology" },
                    { "運動心理", "/Tiss/sportsPsychology" },
                    { "體能訓練", "/Tiss/physicalTraining" },
                    { "運動營養", "/Tiss/sportsNutrition" },
                    { "新聞發佈", "/Tiss/press" },
                    { "中心訊息", "/Tiss/institute" },
                    { "徵才招募", "/Tiss/recruit" },
                    { "中心成果", "/Tiss/achievement" },
                    { "新聞影音", "/Tiss/news" },
                    { "活動紀錄", "/Tiss/activityRecord" },
                    { "兒少科普", "/Tiss/childrenScience" },
                    { "科普海報下載專區", "/Tiss/SciencePosterDownLoad" },
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

                _db.SaveChanges();

                return View(model);
                //return View(article);
            }
            catch (Exception ex)
            {
                throw ex;
                //return RedirectToAction("Error404", "Error");
            }
        }

        /// <summary>
        /// 處理上傳圖片
        /// </summary>
        /// <param name="dto"></param>
        /// <param name="imageFile"></param>
        /// <returns></returns>
        [HttpPost]
        [ValidateAntiForgeryToken]
        [ValidateInput(false)]
        public ActionResult ViewArticle(ArticleContent dto, HttpPostedFileBase imageFile, HttpPostedFileBase documentFile, string documentCategory)
        {
            try
            {
                if (ModelState.IsValid)
                {
                    var exist = _db.ArticleContent.Find(dto.Id);

                    if (exist != null)
                    {
                        // 如果有上傳新圖片，則處理圖片上傳
                        if (imageFile != null && imageFile.ContentLength > 0)
                        {
                            using (var binaryReader = new BinaryReader(imageFile.InputStream))
                            {
                                byte[] imageData = binaryReader.ReadBytes(imageFile.ContentLength);
                                // 將圖片儲存為二進制數據
                                exist.ImageContent = imageData;
                            }
                        }

                        // 新增文件上傳功能
                        if (documentFile != null && documentFile.ContentLength > 0)
                        {
                            var fileUploadResult = SaveDocumentFile(documentFile, dto.Id, documentCategory);
                            if (!fileUploadResult)
                            {
                                ModelState.AddModelError("", "文件上傳失敗。");
                            }
                        }

                        // 更新文章內容和其他欄位
                        exist.ContentBody = dto.ContentBody;
                        exist.UpdatedDate = DateTime.Now;
                        exist.UpdatedUser = Session["UserName"] as string;

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
        #endregion

        #region 儲存文件檔案

        private bool SaveDocumentFile(HttpPostedFileBase file, int articleId, string documentCategory)
        {
            try
            {
                if (file != null && file.ContentLength > 0)
                {
                    string fileName = Path.GetFileName(file.FileName);
                    string fileExtension = Path.GetExtension(fileName).ToLower();

                    if (fileExtension == ".pdf" || fileExtension == ".doc" || fileExtension == ".docx" || fileExtension == ".odt")
                    {
                        byte[] fileData;
                        using (var binaryReader = new BinaryReader(file.InputStream))
                        {
                            fileData = binaryReader.ReadBytes(file.ContentLength);
                        }

                        // 使用 this.HttpContext 取得 Session
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
                        _db.SaveChanges();
                        return true;
                    }
                    else
                    {
                        return false;
                    }
                }
            }
            catch (Exception ex)
            {
                // 可以記錄錯誤日志
                Console.WriteLine("文件上傳錯誤：" + ex.Message);
            }

            return false;
        }
        #endregion
        
        #region 切換語言
        public ActionResult ChangeLanguage(string lang)
        {
            if (!string.IsNullOrEmpty(lang))
            {
                var langCookie = new HttpCookie("lang", lang)
                {
                    Expires = DateTime.Now.AddYears(1)
                };
                Response.Cookies.Add(langCookie);
            }
            return Redirect(Request.UrlReferrer.ToString());
        }
        #endregion

        #region 留言認證
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult PostCommentWithCaptcha(int articleId, string userName, string commentText, string recaptchaResponse)
        {
            try
            {
                // 檢查是否為小編
                var isEditor = _db.Users.Any(u => u.UserName == userName && u.IsEditor);

                // 驗證 reCAPTCHA
                var recaptchaSecret = "6Lezbh4qAAAAADGP0PVQCGXgPDtujjwPtY-EdyAB";
                var client = new WebClient();
                var response = client.DownloadString($"https://www.google.com/recaptcha/api/siteverify?secret={recaptchaSecret}&response={recaptchaResponse}");
                dynamic json = JsonConvert.DeserializeObject(response);
                bool isCaptchaValid = json.success;

                if (!isCaptchaValid)
                {
                    // reCAPTCHA 驗證失敗
                    return RedirectToAction("Error");
                }

                // 檢查留言頻率
                var lastComment = _db.MessageBoard
                    .Where(c => c.UserName == userName)
                    .OrderByDescending(c => c.CommentDate)
                    .FirstOrDefault();

                if (lastComment != null && (DateTime.Now - lastComment.CommentDate).TotalMinutes < 1)
                {
                    // 防止頻繁留言
                    return RedirectToAction("Error");
                }

                // 防範 XSS 攻擊
                var encodedCommentText = HttpUtility.HtmlEncode(commentText);
                var encodedUserName = HttpUtility.HtmlEncode(userName);

                // 處理留言
                var comment = new MessageBoard
                {
                    ArticleId = articleId,
                    UserName = encodedUserName,
                    CommentText = encodedCommentText,
                    CommentDate = DateTime.Now,
                    IsApproved = true, // 默認為批准
                    IsEditor = isEditor // 設置是否為小編
                };

                _db.MessageBoard.Add(comment);
                _db.SaveChanges();

                var article = _db.ArticleContent.FirstOrDefault(a => a.Id == articleId);
                if (article != null)
                {
                    return RedirectToAction("ViewArticle", new { encryptedUrl = article.EncryptedUrl });
                }
                return RedirectToAction("Home");
                //return RedirectToAction("ViewArticle", new { encryptedUrl = articleId });
            }
            catch (Exception ex)
            {
                throw ex;
            }

        }
        #endregion

        #region 生成文章點閱率報表
        public ActionResult GenerateArticleClickReport()
        {
            var reportService = new ReportService();

            string reportPath = reportService.GenerateReport();

            if (!string.IsNullOrEmpty(reportPath))
            {
                // 使用 Split 將收件人字串分割成單個地址的陣列
                //string[] toEmail = "00009@tiss.org.tw,00048@tiss.org.tw".Split(',');
                string[] toEmail = "chiachi.pan522@gmail.com,00048@tiss.org.tw".Split(',');

                string subject = "運科中心專欄文章瀏覽率報表";
                string body = "您好，請參閱附件中的運科中心專欄文章瀏覽率報表。";
                // 迴圈發送郵件給每個收件人
                foreach (string email in toEmail)
                {
                    SendEmail(email.Trim(), subject, body, reportPath); //Trim() 確保去除多餘的空格
                }
            }

            return Content("報表產生完成");
        }
        #endregion

        #region 回覆留言
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult PostReply(int parentId, int articleId, string replyText, string replyName, string userAccount)
        {
            if (ModelState.IsValid)
            {
                // 檢查是否為小編
                var isEditor = _db.Users.Any(u => u.UserAccount == userAccount && u.IsEditor);

                var reply = new ReplyMessage
                {
                    MessageBoardId = parentId,
                    UserName = replyName, // 使用者輸入的名稱
                    ReplyText = replyText,
                    ReplyDate = DateTime.Now,
                    Id = articleId,
                    IsApproved = isEditor, // 小編自動批准
                    IsFromEditor = isEditor // 標記來自小編
                };


                _db.ReplyMessage.Add(reply);
                _db.SaveChanges();

                return RedirectToAction("ViewArticle", new { encryptedUrl = _db.ArticleContent.Find(articleId).EncryptedUrl });
            }

            return View("Error");
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
                    var allowedExtensions = new[] { ".jpg", ".jpeg", ".png", ".pdf", ".doc", ".docx", "odt" }; // 根據需求設定允許的副檔名

                    // 檢查是否為允許的檔案類型
                    //if (!allowedExtensions.Contains(extension))
                    //{
                    //    return Json(new { error = "不支援的檔案類型" });
                    //}

                    //// 確保 uploads 目錄存在（如果需要存檔案）
                    //var uploadPath = Server.MapPath("~/uploads");
                    //if (!Directory.Exists(uploadPath))
                    //{
                    //    Directory.CreateDirectory(uploadPath);
                    //}

                    // 讀取檔案的二進位數據
                    byte[] fileData;
                    using (var binaryReader = new BinaryReader(file.InputStream))
                    {
                        fileData = binaryReader.ReadBytes(file.ContentLength);
                    }
                    // 確認檔案大小
                    var fileSize = fileData.Length;
                    // 從 Session 中獲取使用者名稱
                    var creator = Session["UserName"]?.ToString() ?? "未知使用者";

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

                        Category = "TinyMCEFileUpload"  // 標示檔案類型
                    };
                    _db.Documents.Add(document);
                    _db.SaveChanges();

                    // 創建 URL 來顯示檔案，這個 URL 可以是從資料庫取得的檔案連結
                    var fileUrl = Url.Action("DownloadFile", "Tiss", new { id = document.DocumentID });

                    // 返回成功消息，包含新生成的 ID 和 URL
                    return Json(new { message = "檔案上傳成功", fileId = document.DocumentID, url = fileUrl });
                    // 將檔案儲存至資料庫（而不是本地儲存）
                    //var fileDocument = new FileDocument
                    //{
                    //    DocumentName = fileName,
                    //    UploadTime = DateTime.Now, // 當前時間
                    //    Creator = creator, // 假設使用者名稱來自身份驗證
                    //    DocumentType = extension, // 檔案類型
                    //    FileSize = fileSize, // 檔案大小
                    //    LastModifiedTime = DateTime.Now, // 當前時間
                    //    IsEnabled = true, // 根據需求設置
                    //    FileURL = fileUrl, // 將生成的FileURL保存到資料庫
                    //};

                    //_db.FileDocument.Add(fileDocument);
                    ////_db.SaveChanges();

                    //// 返回成功消息，並包含新生成的 ID 和 URL
                    //return Json(new { message = "檔案上傳成功", fileId = fileDocument.ID, url = fileUrl });
                }
                catch (Exception ex)
                {
                    return Json(new { error = "檔案上傳失敗: " + ex.Message });
                }
            }

            return Json(new { error = "檔案上傳失敗" });
        }

        #endregion

        #region 公開資訊_上傳文件的通用方法
        [HttpPost]
        public ActionResult UploadDocument(HttpPostedFileBase file, string category, int? page)
        {
            try
            {
                // 上傳文件
                var uploadResult = _fileUploadService.UploadFile(file, category);

                // 根據文件的 category 決定返回哪一個頁面
                switch (category)
                {
                    case "Public_Info":
                        return RedirectToAction("Public_Info", new { page });
                    case "Regulation":
                        return RedirectToAction("Regulation", new { page });
                    case "Procedure":
                        return RedirectToAction("Procedure", new { page });
                    case "Plan":
                        return RedirectToAction("Plan", new { page });
                    case "Budget":
                        return RedirectToAction("Budget", new { page });
                    case "Download":
                        return RedirectToAction("Download", new { page });
                    case "Other":
                        return RedirectToAction("Other", new { page });
                    case "GenderEquality":
                        return RedirectToAction("GenderEquality", new { page });
                    default:
                        // 如果 category 不符合任何條件，則返回到一個默認頁面
                        return RedirectToAction("Regulation", new { page });
                }
            }
            catch (Exception ex)
            {

                throw ex;
            }
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

                    // 如果文件是 PDF，則設置為 inline，其他文件保持下載行為
                    var disposition = document.DocumentType == ".pdf" ? "inline" : "attachment";

                    // 編碼文件名稱為 UTF-8，確保特殊字符正確顯示
                    var encodedFileName = Uri.EscapeDataString(document.DocumentName);

                    Response.Headers["Content-Disposition"] = $"{disposition}; filename=\"{document.DocumentName}\"";
                    ViewBag.Title = document.DocumentName;

                    return File(document.FileContent, contentType);
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
                default:
                    return "application/octet-stream"; // 其他文件類型預設為下載
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
    }
}