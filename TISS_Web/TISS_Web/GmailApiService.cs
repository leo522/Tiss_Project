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

namespace TISS_Web
{
    public class GmailApiService
    {
        static string[] Scopes = { GmailService.Scope.GmailSend };
        //static string ApplicationName = "運科中心專欄文章瀏覽率報表"; // 替換成你的應用程式名稱
        static string ApplicationName = "TissTransAPI";

        public void SendEmail(string toEmail, string subject, string body, string attachmentPath = null)
        {
            UserCredential credential;

            using (var stream =
                new FileStream(@"C:\運科中心網頁專案\client_secret_562908734561-2ah8nilm2teje23cflss2h79cunhqrri.apps.googleusercontent.com.json", FileMode.Open, FileAccess.Read))
            {
                string credPath = @"C:\運科中心網站\token.json";
                credential = GoogleWebAuthorizationBroker.AuthorizeAsync(
                    GoogleClientSecrets.Load(stream).Secrets,
                    Scopes,
                    "user",
                    CancellationToken.None,
                    new FileDataStore(credPath, true)).Result;
                Console.WriteLine("Credential file saved to: " + credPath);
            }
            // 使用 Server.MapPath 將相對路徑轉換為絕對路徑
            //string clientSecretPath = System.Web.Hosting.HostingEnvironment.MapPath("~/client_secret.json");
            //string clientSecretPath = System.Web.Hosting.HostingEnvironment.MapPath("~/client_secret_562908734561-2ah8nilm2teje23cflss2h79cunhqrri.apps.googleusercontent.com.json");

            //using (var stream = new FileStream(clientSecretPath, FileMode.Open, FileAccess.Read))
            //{
            //    //string credPath = "token.json";
            //    //string credPath = @"C:\運科中心網站token.json";
            //    string credPath = System.Web.Hosting.HostingEnvironment.MapPath("~/token.json");
            //    credential = GoogleWebAuthorizationBroker.AuthorizeAsync(
            //        GoogleClientSecrets.Load(stream).Secrets,
            //        Scopes,
            //        "user",
            //        CancellationToken.None,
            //        new FileDataStore(credPath, true)).Result;
            //    Console.WriteLine("Credential file saved to: " + credPath);
            //}

            //創建 Gmail API 服務
            var service = new GmailService(new BaseClientService.Initializer()
            {
                HttpClientInitializer = credential,
                ApplicationName = ApplicationName,
            });

            //使用 MimeKit 創建郵件
            var message = new MimeMessage();
            message.From.Add(new MailboxAddress("運科中心", "00048@tiss.org.tw")); //Gmail帳戶
            message.To.Add(new MailboxAddress("", toEmail));
            message.Subject = subject;

            var bodyBuilder = new BodyBuilder { HtmlBody = body }; // 設置 HTML 郵件內容

            //如果有附件，則添加到郵件
            if (!string.IsNullOrEmpty(attachmentPath))
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

            message.Body = bodyBuilder.ToMessageBody(); // 設置郵件主體

            var messageRaw = "";
            using (var memoryStream = new MemoryStream())
            {
                // 使用 MimeMessage 的 WriteTo 方法將郵件內容寫入 MemoryStream
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
    }
}