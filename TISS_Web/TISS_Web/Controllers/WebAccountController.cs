using System;
using System.Collections.Generic;
using System.Data.Entity.Validation;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Web;
using System.Web.Mvc;
using System.Web.Security;
using TISS_Web.Models;

namespace TISS_Web.Controllers
{
    public class WebAccountController : Controller
    {
        private TISS_WebEntities _db = new TISS_WebEntities(); //資料庫

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
                var adminEmail = "chiachi.pan@tiss.org.tw";
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
                var pendingUsers = _db.Users.Where(u => !(u.IsApproved ?? false))
                        .Select(u => new UserModel
                        {
                            UserName = u.UserName,
                            Email = u.Email,
                            CreatedDate = DateTime.Now
                        }).ToList();

                // **提供所有角色給前端**
                ViewBag.Roles = _db.Roles.ToList();

                return View(pendingUsers);
            }
            catch (Exception ex)
            {
                Console.WriteLine("其他錯誤: " + ex.Message);
                return View("Error");
            }
        }

        [HttpPost]
        public JsonResult ApproveUser(string userName, List<int> roleIds)
        {
            try
            {
                var user = _db.Users.SingleOrDefault(u => u.UserName == userName);
                if (user == null)
                {
                    return Json(new { success = false, error = "用戶不存在" });
                }

                user.IsApproved = true;

                // **更新 InternalEmployees 註冊狀態**
                var employee = _db.InternalEmployees.FirstOrDefault(e => e.EmailAddress == user.Email);
                if (employee != null)
                {
                    employee.IsRegistered = true;
                }

                // **清除舊的角色，然後重新分配**
                var existingRoles = _db.UserRoles.Where(ur => ur.UserID == user.UserID).ToList();
                _db.UserRoles.RemoveRange(existingRoles);

                if (roleIds != null && roleIds.Count > 0)  // ✅ 修正 `null` 檢查
                {
                    foreach (var roleId in roleIds)
                    {
                        _db.UserRoles.Add(new UserRoles { UserID = user.UserID, RoleID = roleId });
                    }
                }

                _db.SaveChanges();

                // **發送通知**
                var emailBody = $"您的帳號 {user.UserName} 已通過審核，現在可以登入系統！";
                SendEmail(user.Email, "國家運動科學中心，網頁管理者帳號審核通過通知", emailBody, null);

                return Json(new { success = true, message = "審核成功" });
            }
            catch (Exception ex)
            {
                Console.WriteLine("其他錯誤: " + ex.Message);
                return Json(new { success = false, error = "伺服器錯誤：" + ex.Message });
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
            Session.Remove("LoggedIn"); //清除所有的 Session 資訊

            FormsAuthentication.SignOut(); //清除所有的 Forms 認證 Cookies

            // 取得登出前的頁面路徑，如果沒有則預設為首頁
            string returnUrl = Request.UrlReferrer != null ? Request.UrlReferrer.ToString() : Url.Action("Home", "Tiss");

            return Redirect(returnUrl);  //重定向到 Home 頁面
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

        #region 生成文章點閱率報表
        public ActionResult GenerateArticleClickReport()
        {
            var reportService = new ReportService();

            string reportPath = reportService.GenerateReport();

            if (!string.IsNullOrEmpty(reportPath))
            {
                // 使用 Split 將收件人字串分割成單個地址的陣列
                string[] toEmail = "edithsu@tiss.org.tw,chiachi.pan@tiss.org.tw".Split(',');
                //string[] toEmail = "chiachi.pan522@gmail.com,00048@tiss.org.tw".Split(',');

                string subject = "運科中心專欄文章點閱量報表";
                string body = "您好，請參閱附件中的運科中心專欄文章點閱量報表。";
                // 迴圈發送郵件給每個收件人
                foreach (string email in toEmail)
                {
                    SendEmail(email.Trim(), subject, body, reportPath); //Trim() 確保去除多餘的空格
                }
            }

            return Content("報表產生完成");
        }
        #endregion
    }
}