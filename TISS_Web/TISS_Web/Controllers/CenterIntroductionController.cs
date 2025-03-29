using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using TISS_Web.Models;
using TISS_Web.Utility;

namespace TISS_Web.Controllers
{
    public class CenterIntroductionController : Controller
    {
        private TISS_WebEntities _db = new TISS_WebEntities(); //資料庫
        private readonly WebContentService _contentService; //網頁內容存檔共用服務
        private readonly FileUploadService _fileUploadService; //檔案上傳共用服務

        public CenterIntroductionController()
        {
            _contentService = new WebContentService(_db);
            _fileUploadService = new FileUploadService(_db);
        }

        #region 共用方法：儲存與讀取內容
        private ActionResult SavePageContent<T>(string textContent, string imageSrc) where T : class, new()
        {
            try
            {
                var userName = Session["UserName"] as string;
                var result = _contentService.SaveContent(userName, textContent, imageSrc, () => new T());
                return result; // 成功就不寫 Log
            }
            catch (Exception ex)
            {
                LogHelper.WriteAduioVideoLog("SavePageContent", $"儲存 {typeof(T).Name} 錯誤", ex);
                return Json(new { success = false, error = ex.Message });
            }
        }

        private ActionResult GetPageContent<T>() where T : class
        {
            try
            {
                return _contentService.GetContent<T>();
            }
            catch (Exception ex)
            {
                LogHelper.WriteAduioVideoLog("GetPageContent", $"讀取 {typeof(T).Name} 錯誤", ex);
                return Json(new { success = false, error = ex.Message }, JsonRequestBehavior.AllowGet);
            }
        }
        #endregion

        #region 各區塊頁面
        public ActionResult about() => LoadView("中心介紹");

        public ActionResult Objectives() => LoadView("使命、願景");

        public ActionResult Mission() => LoadView("中心任務");

        public ActionResult Organization() => LoadView("組織概況");

        public ActionResult BOD() => LoadView("第1屆 董監事");

        public ActionResult CEO() => LoadView("執行長");

        public ActionResult Units() => LoadView("單位介紹");
        #endregion

        #region 共用 View 載入
        private ActionResult LoadView(string title)
        {
            try
            {
                ViewBag.Title = title;
                Session["ReturnUrl"] = Request.Url.ToString();
                return View();
            }
            catch (Exception ex)
            {
                LogHelper.WriteAduioVideoLog("LoadView", $"載入 {title} 頁面錯誤", ex);
                return View("Error");
            }
        }
        #endregion
    }
}