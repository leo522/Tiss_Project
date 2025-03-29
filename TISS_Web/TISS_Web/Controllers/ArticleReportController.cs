using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using TISS_Web.Models;

namespace TISS_Web.Controllers
{
    public class ArticleReportController : Controller
    {
        private TISS_WebEntities _db = new TISS_WebEntities(); //資料庫

        #region 生成文章點閱率報表
        public ActionResult GenerateArticleClickReport()
        {
            var reportService = new ReportService();
            var emailService = new EmailService();

            string reportPath = reportService.GenerateReport();

            if (!string.IsNullOrEmpty(reportPath))
            {
                // 使用 Split 將收件人字串分割成單個地址的陣列
                string[] toEmail = "edithsu@tiss.org.tw,chiachi.pan@tiss.org.tw".Split(',');
                //string[] toEmail = "chiachi.pan@tiss.org.tw".Split(',');

                string subject = "運科中心專欄文章點閱量報表";
                string body = "您好，請參閱附件中的運科中心專欄文章點閱量報表。";
                
                foreach (string email in toEmail)
                {
                    emailService.SendEmail(email.Trim(), subject, body, reportPath); //Trim() 確保去除多餘的空格
                }
            }

            return Content("報表產生完成");
        }
        #endregion
    }
}