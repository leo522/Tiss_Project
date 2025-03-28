using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using System.Web;
using System.Web.Mvc;
using TISS_Web.Models;

namespace TISS_Web
{
    public class EmailService
    {
        private readonly TISS_WebEntities _db;

        public EmailService()
        {
            _db = new TISS_WebEntities();
        }

        public void SendEmail(string toEmail, string subject, string body, string attachmentPath = null)
        {
            var gmailService = new GmailApiService(); // 使用現成的 Gmail 發信邏輯

            try
            {
                gmailService.SendEmail(toEmail, subject, body, attachmentPath);
                Console.WriteLine("郵件已成功發送");

                LogEmail(toEmail, subject, body, "Sent", null); // 寫入紀錄
            }
            catch (Exception ex)
            {
                Console.WriteLine($"錯誤: {ex.Message}");
                LogEmail(toEmail, subject, body, "Failed", ex.Message); // 寫入失敗紀錄
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
    }
}