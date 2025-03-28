using Google.Apis.Auth.OAuth2;
using Google.Apis.Gmail.v1;
using Google.Apis.Gmail.v1.Data;
using Google.Apis.Services;
using Google.Apis.Util.Store;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Mail;
using System.Text;
using System.Threading;
using System.Web;
using System.Web.Services.Description;
using TISS_Web.Models;
using MimeKit;
using System.Configuration;

namespace TISS_Web
{
    public class GmailApiService
    {
        private TISS_WebEntities _db = new TISS_WebEntities(); //資料庫
        static string[] Scopes = { GmailService.Scope.GmailSend };

        static string ApplicationName = "TissTransAPI";

        #region 寄送報表信件
        public void SendEmail(string toEmail, string subject, string body, string attachmentPath = null)
        {
            try
            {
                UserCredential credential;

                string clientSecretPath = ConfigurationManager.AppSettings["GoogleClientSecretPath"];
                string tokenPath = ConfigurationManager.AppSettings["GoogleTokenPath"];

                using (var stream = new FileStream(clientSecretPath, FileMode.Open, FileAccess.Read))
                {
                    credential = GoogleWebAuthorizationBroker.AuthorizeAsync(
                        GoogleClientSecrets.FromStream(stream).Secrets,
                        Scopes,
                        "user",
                        CancellationToken.None,
                        new FileDataStore(tokenPath, true)).Result;

                    Console.WriteLine("Token saved to: " + tokenPath);
                }

                //創建 Gmail API 服務
                var service = new GmailService(new BaseClientService.Initializer()
                {
                    HttpClientInitializer = credential,
                    ApplicationName = ApplicationName,
                });

                //使用 MimeKit 創建郵件
                var message = new MimeMessage();
                message.From.Add(new MailboxAddress("國家運動科學中心", credential.UserId)); //Gmail帳戶
                message.To.Add(new MailboxAddress("", toEmail));
                message.Subject = subject;

                var bodyBuilder = new BodyBuilder { HtmlBody = body }; // 設置 HTML 郵件內容

                //如果有附件，則添加到郵件
                if (!string.IsNullOrEmpty(attachmentPath) && System.IO.File.Exists(attachmentPath))
                {
                    try
                    {
                        bodyBuilder.Attachments.Add(attachmentPath);
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"附件處理失敗: {ex.Message}");
                        throw;
                    }
                }
                else if (!string.IsNullOrEmpty(attachmentPath))
                {
                    Console.WriteLine($"附件路徑不存在或為無效：{attachmentPath}");
                }

                message.Body = bodyBuilder.ToMessageBody(); // 設置郵件主體

                var messageRaw = "";
                using (var memoryStream = new MemoryStream())
                {
                    message.WriteTo(memoryStream);

                    // 轉換為 byte[]，然後用 base64 編碼
                    var byteArray = memoryStream.ToArray();
                    messageRaw = Convert.ToBase64String(byteArray)
                        .Replace('+', '-')
                        .Replace('/', '_')
                        .Replace("=", ""); // URL-safe base64
                }

                // 設置 Gmail API 的郵件物件
                var gmailMessage = new Google.Apis.Gmail.v1.Data.Message
                {
                    Raw = messageRaw
                };

                //發送郵件
                var request = service.Users.Messages.Send(gmailMessage, "me");
                request.Execute();
                Console.WriteLine("Email sent successfully!");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"錯誤: {ex}");
                LogEmail(toEmail, subject, body, "Failed", ex.ToString()); // 改成 ex.ToString()
            }
        }
        #endregion

        #region 處理信件錯誤訊息
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
    }
}