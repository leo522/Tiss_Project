using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Web;
using TISS_Web.Models;

namespace TISS_Web
{
    public class FileUploadService
    {
        private readonly TISS_WebEntities _context;

        public FileUploadService(TISS_WebEntities context)
        {
            _context = context;
        }

        public string UploadFile(HttpPostedFileBase file, string tableName)
        {
            if (file != null && file.ContentLength > 0)
            {
                string fileName = Path.GetFileName(file.FileName);
                string fileExtension = Path.GetExtension(fileName).ToLower();

                // 檢查文件類型是否符合要求
                if (fileExtension == ".pdf" || fileExtension == ".doc" ||
                    fileExtension == ".docx" || fileExtension == ".odt" ||
                    fileExtension == ".xls" || fileExtension == ".xlsx")
                {
                    string filePath = Path.Combine(HttpContext.Current.Server.MapPath("~/storage/media/attachments"), fileName);
                    file.SaveAs(filePath);

                    string userId = HttpContext.Current.Session["UserName"].ToString();

                    switch (tableName)
                    {
                        case "RegulationDocument":
                            var regulationDocument = new RegulationDocument
                            {
                                PId = GetNextPId("RegulationDocument"),
                                DocumentName = fileName,
                                UploadTime = DateTime.Now,
                                Creator = userId,
                                DocumentType = fileExtension,
                                FileSize = file.ContentLength,
                                IsActive = true
                            };
                            _context.RegulationDocument.Add(regulationDocument);
                            break;

                        case "ProcedureDocument":
                            var procedureDocument = new ProcedureDocument
                            {
                                PId = GetNextPId("ProcedureDocument"),
                                DocumentName = fileName,
                                UploadTime = DateTime.Now,
                                Creator = userId,
                                DocumentType = fileExtension,
                                FileSize = file.ContentLength,
                                IsActive = true
                            };
                            _context.ProcedureDocument.Add(procedureDocument);
                            break;
                        case "PlanDocument":
                            var planDocument = new PlanDocument
                            {
                                PId = GetNextPId("PlanDocument"),
                                DocumentName = fileName,
                                UploadTime = DateTime.Now,
                                Creator = userId,
                                DocumentType = fileExtension,
                                FileSize = file.ContentLength,
                                IsActive = true
                            };
                            _context.PlanDocument.Add(planDocument);
                            break;
                        case "BudgetDocument":
                            var budgetDocument = new BudgetDocument
                            {
                                PId = GetNextPId("BudgetDocument"),
                                DocumentName = fileName,
                                UploadTime = DateTime.Now,
                                Creator = userId,
                                DocumentType = fileExtension,
                                FileSize = file.ContentLength,
                                IsActive = true
                            };
                            _context.BudgetDocument.Add(budgetDocument);
                            break;
                        case "DownloadDocument":
                            var downloadDocument = new DownloadDocument
                            {
                                PId = GetNextPId("DownloadDocument"),
                                DocumentName = fileName,
                                UploadTime = DateTime.Now,
                                Creator = userId,
                                DocumentType = fileExtension,
                                FileSize = file.ContentLength,
                                IsActive = true
                            };
                            _context.DownloadDocument.Add(downloadDocument);
                            break;
                        case "PurchaseDocument":
                            var purchaseDocument = new PurchaseDocument
                            {
                                PId = GetNextPId("PurchaseDocument"),
                                DocumentName = fileName,
                                UploadTime = DateTime.Now,
                                Creator = userId,
                                DocumentType = fileExtension,
                                FileSize = file.ContentLength,
                                IsActive = true
                            };
                            _context.PurchaseDocument.Add(purchaseDocument);
                            break;
                        case "OtherDocument":
                            var otherDocument = new OtherDocument
                            {
                                PId = GetNextPId("OtherDocument"),
                                DocumentName = fileName,
                                UploadTime = DateTime.Now,
                                Creator = userId,
                                DocumentType = fileExtension,
                                FileSize = file.ContentLength,
                                IsActive = true
                            };
                            _context.OtherDocument.Add(otherDocument);
                            break;
                        default:
                            return ("上傳發生錯誤");
                    }

                    _context.SaveChanges();
                    return "文件上傳成功！";
                }
                else
                {
                    return ("文件格式不符");
                }
            }
            else
            {
                return ("請選擇要上傳的文件");
            }
        }

        private int GetNextPId(string tableName)
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
                case "PurchaseDocument":
                    return (_context.PurchaseDocument.Max(d => (int?)d.PId) ?? 0) + 1;
                case "OtherDocument":
                    return (_context.OtherDocument.Max(d => (int?)d.PId) ?? 0) + 1;
                default:
                    throw new ArgumentException("Invalid table name.");
            }
        }
    }
}