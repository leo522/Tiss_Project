using OfficeOpenXml;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Web;
using TISS_Web.Models;

namespace TISS_Web
{
    public class ReportService
    {
        private TISS_WebEntities _db = new TISS_WebEntities();

        public void GenerateReport()
        {
            string timestamp = DateTime.Now.ToString("yyyyMMddHHmm");
            string excelPath = $@"D:\文章瀏覽率報表\report_{timestamp}.xlsx";

            try
            {
                var reportData = _db.Database.SqlQuery<ArticleReportModel>("EXEC GetArticleClickReport").ToList();

                using (var package = new ExcelPackage())
                {
                    var worksheet = package.Workbook.Worksheets.Add("Report");

                    worksheet.Cells["A1"].LoadFromCollection(reportData, true);

                    package.SaveAs(new FileInfo(excelPath));
                }

                Console.WriteLine("報表產生完成");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"發生錯誤: {ex.Message}");
            }
        }
    }
}