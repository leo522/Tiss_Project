using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Web;
using System.Xml.Linq;
using TISS_Web.Models;
using static TISS_Web.Models.ArticleModel;

namespace TISS_Web
{
    public class FileUploadService
    {
        private readonly TISS_WebEntities _context;

        public FileUploadService(TISS_WebEntities context)
        {
            _context = context;
        }

        public string UploadFile(HttpPostedFileBase file, string category, int? articleId = null)
        {
            if (file != null && file.ContentLength > 0)
            {
                try
                {
                    string fileName = Path.GetFileName(file.FileName);
                    string fileExtension = Path.GetExtension(fileName).ToLower();

                    //檢查文件類型是否符合要求
                    if (fileExtension == ".pdf" || fileExtension == ".doc" ||
                        fileExtension == ".docx" || fileExtension == ".odt" ||
                        fileExtension == ".xls" || fileExtension == ".xlsx")
                    {

                        // 檢查 InputStream 的長度
                        if (file.InputStream.Length == 0)
                        {
                            return "上傳文件的內容為空！";
                        }

                        byte[] fileData = null; //讀取文件二進制數據

                        using (var binaryReader = new BinaryReader(file.InputStream))
                        {
                            fileData = binaryReader.ReadBytes(file.ContentLength);
                        }

                        string userId = HttpContext.Current.Session["UserName"].ToString();

                        var document = new Documents
                        {
                            DocumentName = fileName,
                            UploadTime = DateTime.Now,
                            Creator = userId,
                            DocumentType = fileExtension,
                            FileSize = fileData.Length,
                            FileContent = fileData, //將文件二進制數據存入資料庫
                            IsActive = true,
                            Category = category, //使用category來區分文件類型
                            ArticleId = articleId, //關聯文章ID
                        };

                        _context.Documents.Add(document);
                        _context.SaveChanges();

                        return "文件上傳成功！";
                    }
                    else
                    {
                        return ("文件格式不符");
                    }
                }
                catch (Exception ex)
                {
                    throw ex;
                }          
            }
            else
            {
                return ("請選擇要上傳的文件");
            }
        }

        #region 性別平等專區上傳網址
        public string UploadUrl(string url)
        {
            if (!string.IsNullOrEmpty(url))
            {
                try
                {
                    string userId = HttpContext.Current.Session["UserName"].ToString();

                    var genderEqualityDocument = new GenderEqualityDocument
                    {
                        PId = GetNextPId("GenderEqualityDocument"),
                        URL = url,
                        UploadTime = DateTime.Now,
                        Creator = userId,
                        IsActive = true
                    };

                    _context.GenderEqualityDocument.Add(genderEqualityDocument);
                    _context.SaveChanges(); // 儲存變更到資料庫
                }
                catch (Exception ex)
                {
                    throw new Exception("URL 上傳失敗: " + ex.Message);
                }
            }
            return "URL 上傳成功";
        }
        #endregion

        private int GetNextPId(string tableName)
        {
            try
            {
                switch (tableName)
                {
                    case "RegulationDocument":
                        return (_context.RegulationDocument.Max(d => (int?)d.PId) ?? 0) + 1;
                    case "ProcedureDocument":
                        return (_context.ProcedureDocument.Max(d => (int?)d.PId) ?? 0) + 1;
                    case "PlanDocument":
                        return (_context.PlanDocument.Max(d => (int?)d.PId) ?? 0) + 1;
                    case "BudgetDocument":
                        return (_context.BudgetDocument.Max(d => (int?)d.PId) ?? 0) + 1;
                    case "DownloadDocument":
                        return (_context.DownloadDocument.Max(d => (int?)d.PId) ?? 0) + 1;
                    //case "OtherDocument":
                    //    return (_context.OtherDocument.Max(d => (int?)d.PId) ?? 0) + 1;
                    //case "GenderEqualityDocument":
                    //    return (_context.GenderEqualityDocument.Max(d => (int?)d.PId) ?? 0) + 1;
                    //case "OverseaWorkDocument":
                    //    return (_context.OverseaWorkDocument.Max(d => (int?)d.PId) ?? 0) + 1;
                    default:
                        throw new ArgumentException("Invalid table name.");
                }
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

        #region 文件下架
        public bool RevokeDocument(int documentId)
        {
            try
            {
                var document = _context.Documents.FirstOrDefault(d => d.DocumentID == documentId);
                if (document == null)
                {
                    return false; // 找不到文件
                }

                document.IsActive = false; // 設定為下架
                _context.SaveChanges();
                return true;
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

        #endregion
    }
}