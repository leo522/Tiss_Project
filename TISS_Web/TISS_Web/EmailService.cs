using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using System.Web;

namespace TISS_Web
{
    public class EmailService
    {
        private readonly HttpClient _httpClient;

        public EmailService()
        {
            _httpClient = new HttpClient();
        }

        public async Task<bool> SendEmailAsync(string toEmail, string subject, string body, string attachmentPath = null)
        {
            var requestUri = "https://your-cloud-function-url"; // 替換成你的Google Cloud Function URL

            var emailData = new
            {
                toEmail,
                subject,
                body,
                attachmentPath
            };

            var json = JsonConvert.SerializeObject(emailData);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            try
            {
                var response = await _httpClient.PostAsync(requestUri, content);
                return response.IsSuccessStatusCode;
            }
            catch (Exception ex)
            {
                // 處理錯誤
                Console.WriteLine("郵件發送失敗: " + ex.Message);
                return false;
            }
        }
    }
}