using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text.RegularExpressions;
using System.Web;
using System.Web.Mvc;
using TISS_Web.Models;

namespace TISS_Web.Controllers
{
    public class MessageBoardController : Controller
    {
        private TISS_WebEntities _db = new TISS_WebEntities(); //資料庫

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
                    return RedirectToAction("Error404","Error");
                }

                // 檢查留言頻率
                var lastComment = _db.MessageBoard
                    .Where(c => c.UserName == userName)
                    .OrderByDescending(c => c.CommentDate)
                    .FirstOrDefault();

                if (lastComment != null && (DateTime.Now - lastComment.CommentDate).TotalMinutes < 1)
                {
                    // 防止頻繁留言
                    return RedirectToAction("Error404", "Error");
                }

                // 防範 XSS 攻擊
                var encodedCommentText = SanitizeComment(commentText);
                var encodedUserName = SanitizeComment(userName);

                string[] blackList = new[] { "script", "iframe", "src=", "onerror", "onload", "<", ">" };

                if (blackList.Any(bad => commentText.IndexOf(bad, StringComparison.OrdinalIgnoreCase) >= 0))
                {
                    return RedirectToAction("Error404", "Error");
                }
                if (blackList.Any(bad => userName.IndexOf(bad, StringComparison.OrdinalIgnoreCase) >= 0))
                {
                    return RedirectToAction("Error404", "Error");
                }
               

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

                return RedirectToAction("Home","Tiss");
            }
            catch (Exception ex)
            {
                // Log 攻擊 IP + 留言內容
                var ip = Request.UserHostAddress;
                System.IO.File.AppendAllText(Server.MapPath("~/App_Data/CommentAttackLog.txt"),
                    $"[{DateTime.Now}] IP: {ip}, UserName: {userName}, Content: {commentText}, Error: {ex.Message}\n");

                throw ex;
            }
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

        #region 在留言儲存前進行內容白名單過濾
        public static string SanitizeComment(string input)
        {
            if (string.IsNullOrWhiteSpace(input)) return string.Empty;

            // 移除 script/style/iframe 相關標籤（避免未來使用者繞過編碼）
            string sanitized = Regex.Replace(input, @"<script[\s\S]*?>[\s\S]*?</script>", "", RegexOptions.IgnoreCase);
            sanitized = Regex.Replace(sanitized, @"<iframe[\s\S]*?>[\s\S]*?</iframe>", "", RegexOptions.IgnoreCase);
            sanitized = Regex.Replace(sanitized, @"<style[\s\S]*?>[\s\S]*?</style>", "", RegexOptions.IgnoreCase);
            sanitized = Regex.Replace(sanitized, @"on\w+\s*=\s*(['""]?).*?\1", "", RegexOptions.IgnoreCase); // 事件屬性移除，如 onclick="..."
            sanitized = Regex.Replace(sanitized, @"<.*?>", ""); // 移除所有 HTML 標籤

            return HttpUtility.HtmlEncode(sanitized);
        }
        #endregion
    }
}