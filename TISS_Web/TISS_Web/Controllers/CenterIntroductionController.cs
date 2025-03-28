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
        [HttpPost, ValidateInput(false)] public ActionResult AboutSaveContent(string textContent, string ImageSrc) => SavePageContent<AboutPageContent>(textContent, ImageSrc);
        [HttpGet] public ActionResult AboutGetContent() => GetPageContent<AboutPageContent>();

        public ActionResult Objectives() => LoadView("使命、願景");
        [HttpPost, ValidateInput(false)] public ActionResult ObjectivesSaveContent(string textContent, string ImageSrc) => SavePageContent<ObjectivesPageContent>(textContent, ImageSrc);
        [HttpGet] public ActionResult ObjectivesGetContent() => GetPageContent<ObjectivesPageContent>();

        public ActionResult Mission() => LoadView("中心任務");
        [HttpPost, ValidateInput(false)] public ActionResult MissionSaveContent(string textContent, string ImageSrc) => SavePageContent<MissionPageContent>(textContent, ImageSrc);
        [HttpGet] public ActionResult MissionGetContent() => GetPageContent<MissionPageContent>();

        public ActionResult Organization() => LoadView("組織概況");
        [HttpPost, ValidateInput(false)] public ActionResult OrganizationSaveContent(string textContent, string ImageSrc) => SavePageContent<OrganizationPageContent>(textContent, ImageSrc);
        [HttpGet] public ActionResult OrganizationGetContent() => GetPageContent<OrganizationPageContent>();

        public ActionResult BOD() => LoadView("第1屆 董監事");
        [HttpPost, ValidateInput(false)] public ActionResult BODSaveContent(string textContent, string ImageSrc) => SavePageContent<BODPageContent>(textContent, ImageSrc);
        [HttpGet] public ActionResult BODGetContent() => GetPageContent<BODPageContent>();

        public ActionResult CEO() => LoadView("執行長");
        [HttpPost, ValidateInput(false)] public ActionResult CEOSaveContent(string textContent, string ImageSrc) => SavePageContent<CEOPageContent>(textContent, ImageSrc);
        [HttpGet] public ActionResult CEOGetContent() => GetPageContent<CEOPageContent>();

        public ActionResult Units() => LoadView("單位介紹");
        [HttpPost, ValidateInput(false)] public ActionResult UnitsSaveContent(string textContent, string ImageSrc) => SavePageContent<UnitsPageContent>(textContent, ImageSrc);
        [HttpGet] public ActionResult UnitsGetContent() => GetPageContent<UnitsPageContent>();
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