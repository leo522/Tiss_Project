using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using TISS_Web.Models;

namespace TISS_Web.Controllers
{
    public class PublicInfoController : Controller
    {
        private TISS_WebEntities _db = new TISS_WebEntities(); //資料庫
        private readonly WebContentService _contentService;
        private readonly FileUploadService _fileUploadService;

        #region 檔案上傳共用服務
        public PublicInfoController()
        {
            try
            {
                _fileUploadService = new FileUploadService(new TISS_WebEntities());
                _contentService = new WebContentService(new TISS_WebEntities()); //網頁內容存檔共用服務
            }
            catch (Exception ex)
            {

                throw ex;
            }
        }
        #endregion

        #region 公開資訊_上傳文件檔案
        // 上傳計畫文件
        [HttpPost]
        public ActionResult UploadPlanDocument(HttpPostedFileBase file, int? page)
        {
            try
            {
                _fileUploadService.UploadFile(file, "PlanDocument");
                return RedirectToAction("Plan", new { page });
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

        //上傳法規文件
        [HttpPost]
        public ActionResult UploadRegulationDocument(HttpPostedFileBase file, int? page)
        {
            try
            {
                _fileUploadService.UploadFile(file, "RegulationDocument");
                return RedirectToAction("Regulation", new { page });
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

        //上傳辦法及要點文件
        [HttpPost]
        public ActionResult UploadProcedureDocument(HttpPostedFileBase file, int? page)
        {
            try
            {
                _fileUploadService.UploadFile(file, "ProcedureDocument");
                return RedirectToAction("Procedure", new { page });
            }
            catch (Exception ex)
            {

                throw ex;
            }
        }

        // 上傳下載專區文件
        [HttpPost]
        public ActionResult UploadDownloadDocument(HttpPostedFileBase file, int? page)
        {
            try
            {
                _fileUploadService.UploadFile(file, "DownloadDocument");
                return RedirectToAction("download", new { page });
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

        // 上傳預算與決算文件
        [HttpPost]
        public ActionResult UploadBudgetDocument(HttpPostedFileBase file, int? page)
        {
            try
            {
                _fileUploadService.UploadFile(file, "BudgetDocument");
                return RedirectToAction("budget", new { page });
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

        // 上傳其他文件
        [HttpPost]
        public ActionResult UploadOtherDocument(HttpPostedFileBase file, int? page)
        {
            try
            {
                _fileUploadService.UploadFile(file, "OtherDocument");
                return RedirectToAction("other", new { page });
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

        // 上傳採購作業實施規章文件
        [HttpPost]
        public ActionResult UploadPurchaseDocument(HttpPostedFileBase file, int? page)
        {
            try
            {
                _fileUploadService.UploadFile(file, "PurchaseDocument");
                return RedirectToAction("purchase", new { page });
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

        //上傳性別平等專區文件
        [HttpPost]
        public ActionResult UploadGenderEqualityDocument(HttpPostedFileBase file, string url)
        {
            try
            {
                _fileUploadService.UploadFile(file, "GenderEqualityDocument");
                return RedirectToAction("GenderEquality");
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }
        #endregion

        #region 公開資訊
        public ActionResult Public_Info(int page = 1, int pageSize = 7)
        {
            try
            {
                ViewBag.Title = "公開資訊";
                Session["ReturnUrl"] = Request.Url.ToString();

                page = Math.Max(1, page); //確保頁碼至少為 1

                var list = _db.Documents.Where(d => d.IsActive && d.Category == "Regulation")
                                .OrderByDescending(d => d.UploadTime)
                                .ToList();

                var totalDocuments = list.Count(); //計算總數和總頁數

                var totalPages = (int)Math.Ceiling(totalDocuments / (double)pageSize);

                page = Math.Min(page, totalPages); //確保頁碼不超過最大頁數

                var dtos = list.Skip((page - 1) * pageSize).Take(pageSize).Select(d => new DocumentModel
                {
                    DocumentID = d.DocumentID,
                    DocumentName = d.DocumentName,
                    DocumentType = d.DocumentType,
                    UploadTime = d.UploadTime,
                    Creator = d.Creator,
                    FileSize = d.FileSize,
                    IsActive = d.IsActive,
                }).ToList();

                ViewBag.CurrentPage = page;
                ViewBag.TotalPages = totalPages;

                return View(dtos);
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }
        #endregion

        #region 法規
        public ActionResult regulation(int page = 1, int pageSize = 7)
        {
            try
            {
                ViewBag.Title = "法規";
                Session["ReturnUrl"] = Request.Url.ToString();

                page = Math.Max(1, page); //確保頁碼至少為 1

                var list = _db.Documents.Where(d => d.IsActive && d.Category == "Regulation")
                                .OrderByDescending(d => d.UploadTime)
                                .ToList();

                //計算總數和總頁數
                var totalDocuments = list.Count();
                var totalPages = (int)Math.Ceiling(totalDocuments / (double)pageSize);

                page = Math.Min(page, totalPages); //確保頁碼不超過最大頁數

                var dtos = list.Skip((page - 1) * pageSize).Take(pageSize).Select(d => new DocumentModel
                {
                    DocumentID = d.DocumentID,
                    DocumentName = d.DocumentName,
                    DocumentType = d.DocumentType,
                    UploadTime = d.UploadTime,
                    Creator = d.Creator,
                    FileSize = d.FileSize,
                    IsActive = d.IsActive,
                }).ToList();

                ViewBag.CurrentPage = page;
                ViewBag.TotalPages = totalPages;

                return View(dtos);
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }
        #endregion

        #region 辦法及要點
        public ActionResult procedure(int page = 1, int pageSize = 10)
        {
            try
            {
                ViewBag.Title = "辦法及要點";
                Session["ReturnUrl"] = Request.Url.ToString();

                page = Math.Max(1, page); //確保頁碼至少為 1

                var list = _db.Documents.Where(d => d.Category == "Procedure" && d.IsActive)
                                        .OrderByDescending(d => d.UploadTime).ToList();

                //計算總數和總頁數
                var totalDocuments = list.Count();
                var totalPages = (int)Math.Ceiling(totalDocuments / (double)pageSize);

                page = Math.Min(page, totalPages); //確保頁碼不超過最大頁數

                var dtos = list.Skip((page - 1) * pageSize).Take(pageSize).Select(d => new DocumentModel
                {
                    DocumentID = d.DocumentID,
                    DocumentName = d.DocumentName,
                    DocumentType = d.DocumentType,
                    UploadTime = d.UploadTime,
                    Creator = d.Creator,
                    FileSize = d.FileSize,
                    IsActive = d.IsActive
                }).ToList();

                ViewBag.CurrentPage = page;
                ViewBag.TotalPages = totalPages;

                return View(dtos);
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }
        #endregion

        #region 計畫
        public ActionResult plan(int page = 1, int pageSize = 10)
        {
            try
            {
                ViewBag.Title = "計畫";
                Session["ReturnUrl"] = Request.Url.ToString();

                page = Math.Max(1, page); //確保頁碼至少為 1

                var list = _db.Documents.Where(d => d.IsActive && d.Category == "Plan")
                                .OrderByDescending(d => d.UploadTime)
                                .ToList();

                //計算總數和總頁數
                var totalDocuments = list.Count();
                var totalPages = (int)Math.Ceiling(totalDocuments / (double)pageSize);

                page = Math.Min(page, totalPages); //確保頁碼不超過最大頁數

                var dtos = list.Skip((page - 1) * pageSize).Take(pageSize).Select(d => new DocumentModel
                {
                    DocumentID = d.DocumentID,
                    DocumentName = d.DocumentName,
                    DocumentType = d.DocumentType,
                    UploadTime = d.UploadTime,
                    Creator = d.Creator,
                    FileSize = d.FileSize,
                    IsActive = d.IsActive,
                }).ToList();

                ViewBag.CurrentPage = page;
                ViewBag.TotalPages = totalPages;

                return View(dtos);
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }
        #endregion

        #region 預算與決算
        public ActionResult budget(int page = 1, int pageSize = 10)
        {
            try
            {
                ViewBag.Title = "預算與決算";
                Session["ReturnUrl"] = Request.Url.ToString();

                page = Math.Max(1, page); //確保頁碼至少為 1

                var list = _db.Documents.Where(d => d.IsActive && d.Category == "Budget")
                                .OrderByDescending(d => d.UploadTime)
                                .ToList();

                //計算總數和總頁數
                var totalDocuments = list.Count();
                var totalPages = (int)Math.Ceiling(totalDocuments / (double)pageSize);

                page = Math.Min(page, totalPages); //確保頁碼不超過最大頁數

                var dtos = list.Skip((page - 1) * pageSize).Take(pageSize).Select(d => new DocumentModel
                {
                    DocumentID = d.DocumentID,
                    DocumentName = d.DocumentName,
                    DocumentType = d.DocumentType,
                    UploadTime = d.UploadTime,
                    Creator = d.Creator,
                    FileSize = d.FileSize,
                    IsActive = d.IsActive,
                }).ToList();

                ViewBag.CurrentPage = page;
                ViewBag.TotalPages = totalPages;

                return View(dtos);
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }
        #endregion

        #region 下載專區
        public ActionResult download(int page = 1, int pageSize = 10)
        {
            try
            {
                ViewBag.Title = "下載專區";
                Session["ReturnUrl"] = Request.Url.ToString();

                page = Math.Max(1, page); //確保頁碼至少為 1

                var list = _db.Documents.Where(d => d.IsActive && d.Category == "Download").OrderByDescending(d => d.UploadTime)
                    .ToList();

                //計算總數和總頁數
                var totalDocuments = list.Count();
                var totalPages = (int)Math.Ceiling(totalDocuments / (double)pageSize);

                page = Math.Min(page, totalPages);

                var dtos = list.Skip((page - 1) * pageSize).Take(pageSize).Select(d => new DocumentModel
                {
                    DocumentID = d.DocumentID,
                    DocumentName = d.DocumentName,
                    DocumentType = d.DocumentType,
                    UploadTime = d.UploadTime,
                    Creator = d.Creator,
                    FileSize = d.FileSize,
                    IsActive = d.IsActive,
                }).ToList();

                ViewBag.CurrentPage = page;
                ViewBag.TotalPages = totalPages;

                return View(dtos);
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }
        #endregion

        #region 性別平等專區
        public ActionResult GenderEquality()
        {
            try
            {
                ViewBag.Title = "性別平等專區";
                Session["ReturnUrl"] = Request.Url.ToString();

                var dto = _db.GenderEqualityDocument.Where(d => d.IsActive)
                        .OrderByDescending(d => d.UploadTime) // 按 UploadTime 降序排序
                        .ToList(); //獲取資料列表

                var Websites = _db.DomainsURL.Where(d => d.IsActive).ToList(); // 獲取相關網站資料

                ViewBag.Websites = Websites; // 將資料傳遞到視圖

                return View(dto);
            }
            catch (Exception ex)
            {

                throw ex;
            }
        }
        #endregion

        #region 其他
        public ActionResult other(int page = 1, int pageSize = 10)
        {
            try
            {
                ViewBag.Title = "其他";
                Session["ReturnUrl"] = Request.Url.ToString();

                page = Math.Max(1, page); //確保頁碼至少為 1

                var list = _db.Documents.Where(d => d.Category == "Other" && d.IsActive)
                                .OrderByDescending(d => d.UploadTime).ToList();

                //計算總數和總頁數
                var totalDocuments = list.Count();
                var totalPages = (int)Math.Ceiling(totalDocuments / (double)pageSize);

                page = Math.Min(page, totalPages); //確保頁碼不超過最大頁數

                var dtos = list.Skip((page - 1) * pageSize).Take(pageSize).Select(d => new DocumentModel
                {
                    DocumentID = d.DocumentID,
                    DocumentName = d.DocumentName,
                    DocumentType = d.DocumentType,
                    UploadTime = d.UploadTime,
                    Creator = d.Creator,
                    FileSize = d.FileSize,
                    IsActive = d.IsActive,
                }).ToList();

                ViewBag.CurrentPage = page;
                ViewBag.TotalPages = totalPages;

                return View(dtos);
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }
        #endregion

        #region 國外參訪及工作報告
        public ActionResult overseawork(int page = 1, int pageSize = 10)
        {
            try
            {
                ViewBag.Title = "國外參訪及工作報告";
                Session["ReturnUrl"] = Request.Url.ToString();

                page = Math.Max(1, page); //確保頁碼至少為 1

                var list = _db.Documents.Where(d => d.Category == "OverseaWork" && d.IsActive)
                                .OrderByDescending(d => d.UploadTime).ToList();

                //計算總數和總頁數
                var totalDocuments = list.Count();
                var totalPages = (int)Math.Ceiling(totalDocuments / (double)pageSize);

                page = Math.Min(page, totalPages); //確保頁碼不超過最大頁數

                var dtos = list.Skip((page - 1) * pageSize).Take(pageSize).Select(d => new DocumentModel
                {
                    DocumentID = d.DocumentID,
                    DocumentName = d.DocumentName,
                    DocumentType = d.DocumentType,
                    UploadTime = d.UploadTime,
                    Creator = d.Creator,
                    FileSize = d.FileSize,
                    IsActive = d.IsActive,
                }).ToList();

                ViewBag.CurrentPage = page;
                ViewBag.TotalPages = totalPages;

                return View(dtos);
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }
        #endregion
    }
}