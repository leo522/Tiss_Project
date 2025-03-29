using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using TISS_Web.Models;
using TISS_Web.Utility;

namespace TISS_Web.Controllers
{
    public class PublicInfoController : Controller
    {
        private TISS_WebEntities _db = new TISS_WebEntities(); //資料庫
        private readonly WebContentService _contentService;
        private readonly FileUploadService _fileUploadService;

        public PublicInfoController()
        {
            try
            {
                _fileUploadService = new FileUploadService(_db);
                _contentService = new WebContentService(_db);
            }
            catch (Exception ex)
            {
                LogHelper.WriteInternalPolicyLog("初始化錯誤", "PublicInfoController 初始化錯誤", ex);
            }
        }

        #region 共用方法：文件清單載入
        private ActionResult LoadDocuments(string title, string category, string viewName, int page, int pageSize)
        {
            try
            {
                ViewBag.Title = title;
                Session["ReturnUrl"] = Request.Url.ToString();
                page = Math.Max(1, page);

                var list = _db.Documents.Where(d => d.IsActive && d.Category == category)
                                        .OrderByDescending(d => d.UploadTime).ToList();

                int total = list.Count();
                int totalPages = (int)Math.Ceiling(total / (double)pageSize);
                page = Math.Min(page, totalPages);

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

                return View(viewName, dtos);
            }
            catch (Exception ex)
            {
                LogHelper.WriteInternalPolicyLog("載入", $"載入 {title} 錯誤", ex);
                return RedirectToAction("Error404", "Error");
            }
        }
        #endregion

        #region 共用方法：上傳文件
        [HttpPost]
        public ActionResult UploadDocument(HttpPostedFileBase file, string category, string redirectAction, int? page)
        {
            try
            {
                _fileUploadService.UploadFile(file, category);
                return RedirectToAction(redirectAction, new { page });
            }
            catch (Exception ex)
            {
                LogHelper.WriteInternalPolicyLog("上傳文件", "上傳文件錯誤", ex);
                return RedirectToAction("Error404", "Error");
            }
        }
        #endregion

        #region 公開資訊
        public ActionResult Public_Info(int page = 1, int pageSize = 10) => LoadDocuments("公開資訊", "Regulation", "Regulation", page, pageSize);
        #endregion

        #region 法規
        public ActionResult regulation(int page = 1, int pageSize = 10) => LoadDocuments("法規", "Regulation", "Regulation", page, pageSize);
        #endregion

        #region 辦法及要點
        public ActionResult procedure(int page = 1, int pageSize = 10) => LoadDocuments("辦法及要點", "Procedure", "Procedure", page, pageSize);
        #endregion

        #region 計畫
        public ActionResult plan(int page = 1, int pageSize = 10) => LoadDocuments("計畫", "Plan", "Plan", page, pageSize);
        #endregion

        #region 預算與決算
        public ActionResult budget(int page = 1, int pageSize = 10) => LoadDocuments("預算與決算", "Budget", "Budget", page, pageSize);
        #endregion

        #region 下載專區
        public ActionResult download(int page = 1, int pageSize = 10) => LoadDocuments("下載專區", "Download", "Download", page, pageSize);
        #endregion

        #region 其他
        public ActionResult other(int page = 1, int pageSize = 10) => LoadDocuments("其他", "Other", "Other", page, pageSize);
        #endregion

        #region 國外參訪及工作報告
        public ActionResult overseawork(int page = 1, int pageSize = 10) => LoadDocuments("國外參訪及工作報告", "OverseaWork", "OverseaWork", page, pageSize);
        #endregion

        #region 公開資訊文件隱藏(下架)
        [HttpPost]
        public ActionResult RevokeDocument(int documentId, int? page)
        {
            try
            {
                // 檢查是否已登入
                if (Session["LoggedIn"] == null || !(bool)Session["LoggedIn"])
                {
                    return new HttpStatusCodeResult(403, "未授權的存取");
                }

                var document = _db.Documents.FirstOrDefault(d => d.DocumentID == documentId);
                if (document == null)
                {
                    return HttpNotFound("找不到指定的文件");
                }

                document.IsActive = false; // 設定文件為「非啟用」，即下架狀態
                _db.SaveChanges();

                // 依據文件的分類回到對應的頁面
                switch (document.Category)
                {
                    case "Public_Info":
                        return RedirectToAction("Public_Info", new { page });
                    case "Regulation":
                        return RedirectToAction("Regulation", new { page });
                    case "Procedure":
                        return RedirectToAction("Procedure", new { page });
                    case "Plan":
                        return RedirectToAction("Plan", new { page });
                    case "Budget":
                        return RedirectToAction("Budget", new { page });
                    case "Download":
                        return RedirectToAction("Download", new { page });
                    case "Other":
                        return RedirectToAction("Other", new { page });
                    case "GenderEquality":
                        return RedirectToAction("GenderEquality", new { page });
                    case "OverseaWork":
                        return RedirectToAction("OverseaWork", new { page });
                    default:
                        return RedirectToAction("Public_Info", new { page });
                }
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }
        #endregion
    }
}