using OfficeOpenXml;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Web;
using TISS_Web.Models;
using System.Threading;

namespace TISS_Web
{
    public class ReportService
    {
        private TISS_WebEntities _db = new TISS_WebEntities();

        public string GenerateReport()
        {
            ExcelPackage.LicenseContext = LicenseContext.NonCommercial; //設置許可上下文

            string timestamp = DateTime.Now.ToString("yyyyMMddHHmm");
            //string excelPath = $@"D:\文章瀏覽率報表\report_{timestamp}.xlsx";
            string reportDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Reports");
            //string reportDir = Path.Combine("D:\\Reports"); // 使用較短的路徑

            // 確保資料夾存在
            if (!Directory.Exists(reportDir))
            {
                Directory.CreateDirectory(reportDir);
            }
            string excelPath = Path.Combine(reportDir, $"report_{timestamp}.xlsx");

            try
            {
                var reportData = _db.Database.SqlQuery<ArticleReportModel>("EXEC GetArticleClickReport").ToList();

                if (reportData.Count == 0)
                {
                    Console.WriteLine("查詢結果為空，無法生成報表。");
                    return null;
                }

                //var reportData = _db.Database.SqlQuery<ArticleReportModel>("EXEC GetArticleClickReport").ToList();

                using (var package = new ExcelPackage())
                {
                    var worksheet = package.Workbook.Worksheets.Add("Report");

                    worksheet.Cells["A1"].LoadFromCollection(reportData, true);

                    package.SaveAs(new FileInfo(excelPath));
                }

                Thread.Sleep(1000); //確保檔案已完全寫入

                FileInfo fileInfo = new FileInfo(excelPath); //檢查生成的檔案大小，Gmail 附件限制為 25MB
                if (fileInfo.Length > 25 * 1024 * 1024)
                {
                    throw new Exception("生成的報表文件過大，無法作為附件發送。");
                }

                Console.WriteLine("報表產生完成");
                return excelPath;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"發生錯誤: {ex.Message}");
                return null;
            }
        }

        private void LogMessage(string message)
        {
            string logFilePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Logs", "app.log");

            // 確保日誌資料夾存在
            string logDir = Path.GetDirectoryName(logFilePath);
            if (!Directory.Exists(logDir))
            {
                Directory.CreateDirectory(logDir);
            }

            using (StreamWriter writer = new StreamWriter(logFilePath, true))
            {
                writer.WriteLine($"{DateTime.Now}: {message}");
            }
        }
    }
}