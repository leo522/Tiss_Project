using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using TISS_Web.Models;

namespace TISS_Web.Controllers
{
    public class CenterIntroductionController : Controller
    {
        private TISS_WebEntities _db = new TISS_WebEntities(); //資料庫

        #region 網頁內容存檔共用服務
        private readonly WebContentService _contentService;
        #endregion

        #region 檔案上傳共用服務
        private readonly FileUploadService _fileUploadService;

        public CenterIntroductionController()
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

        #region 中心介紹
        public ActionResult about()
        {
            try
            {
                ViewBag.Title = "中心介紹";
                Session["ReturnUrl"] = Request.Url.ToString();
                return View();
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }
        
        [HttpPost]
        [ValidateInput(false)]
        public ActionResult aboutSaveContent(string textContent, string ImageSrc)
        {
            try
            {
                var userName = Session["UserName"] as string;
                return _contentService.SaveContent(userName, textContent, ImageSrc, () => new AboutPageContent());
            }
            catch (Exception ex)
            {
                return Json(new { success = false, error = ex.Message });
            }
        }
      
        [HttpGet]
        public ActionResult aboutGetContent()
        {
            try
            {
                return _contentService.GetContent<AboutPageContent>();
            }
            catch (Exception ex)
            {
                return Json(new { success = false, error = ex.Message }, JsonRequestBehavior.AllowGet);
            }
        }
        #endregion

        #region 使命願景 
        public ActionResult Objectives()
        {
            try
            {
                ViewBag.Title = "使命、願景";
                Session["ReturnUrl"] = Request.Url.ToString();
                return View();
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

        [HttpPost]
        [ValidateInput(false)]
        public ActionResult objectivesSaveContent(string textContent, string ImageSrc)
        {
            try
            {
                var userName = Session["UserName"] as string;
                return _contentService.SaveContent(userName, textContent, ImageSrc, () => new ObjectivesPageContent());
            }
            catch (Exception ex)
            {
                return Json(new { success = false, error = ex.Message });
            }
        }
        [HttpGet]
        public ActionResult objectivesGetContent()
        {
            try
            {
                return _contentService.GetContent<ObjectivesPageContent>();
            }
            catch (Exception ex)
            {
                return Json(new { success = false, error = ex.Message }, JsonRequestBehavior.AllowGet);
            }
        }
        #endregion

        #region 中心任務   
        public ActionResult mission()
        {
            try
            {
                ViewBag.Title = "中心任務";
                Session["ReturnUrl"] = Request.Url.ToString();
                return View();
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

        [HttpPost]
        [ValidateInput(false)]
        public ActionResult missionSaveContent(string textContent, string ImageSrc)
        {
            try
            {
                var userName = Session["UserName"] as string;
                return _contentService.SaveContent(userName, textContent, ImageSrc, () => new MissionPageContent());
            }
            catch (Exception ex)
            {
                return Json(new { success = false, error = ex.Message });
            }
        }
        [HttpGet]
        public ActionResult missionGetContent()
        {
            try
            {
                return _contentService.GetContent<MissionPageContent>();
            }
            catch (Exception ex)
            {
                return Json(new { success = false, error = ex.Message }, JsonRequestBehavior.AllowGet);
            }
        }
        #endregion

        #region 組織概況
        public ActionResult Organization()
        {
            try
            {
                ViewBag.Title = "組織概況";
                Session["ReturnUrl"] = Request.Url.ToString();
                return View();
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

        [HttpPost]
        [ValidateInput(false)]
        public ActionResult organizationSaveContent(string textContent, string ImageSrc)
        {
            try
            {
                var userName = Session["UserName"] as string;
                return _contentService.SaveContent(userName, textContent, ImageSrc, () => new OrganizationPageContent());
            }
            catch (Exception ex)
            {
                return Json(new { success = false, error = ex.Message });
            }
        }
        [HttpGet]
        public ActionResult organizationGetContent()
        {
            try
            {
                return _contentService.GetContent<OrganizationPageContent>();
            }
            catch (Exception ex)
            {
                return Json(new { success = false, error = ex.Message }, JsonRequestBehavior.AllowGet);
            }
        }
        #endregion

        #region 董監事
        public ActionResult BOD()
        {
            try
            {
                ViewBag.Title = "第1屆 董監事";
                Session["ReturnUrl"] = Request.Url.ToString();
                return View();
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

        [HttpPost]
        [ValidateInput(false)]
        public ActionResult BODSaveContent(string textContent, string ImageSrc)
        {
            try
            {
                var userName = Session["UserName"] as string;
                return _contentService.SaveContent(userName, textContent, ImageSrc, () => new BODPageContent());
            }
            catch (Exception ex)
            {
                return Json(new { success = false, error = ex.Message });
            }
        }
        [HttpGet]
        public ActionResult BODGetContent()
        {
            try
            {
                return _contentService.GetContent<BODPageContent>();
            }
            catch (Exception ex)
            {
                return Json(new { success = false, error = ex.Message }, JsonRequestBehavior.AllowGet);
            }
        }
        #endregion

        #region 執行長
        public ActionResult CEO()
        {
            try
            {
                ViewBag.Title = "執行長";
                Session["ReturnUrl"] = Request.Url.ToString();
                return View();
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

        [HttpPost]
        [ValidateInput(false)]
        public ActionResult CeoSaveContent(string textContent, string ImageSrc)
        {
            try
            {
                var userName = Session["UserName"] as string;
                return _contentService.SaveContent(userName, textContent, ImageSrc, () => new CEOPageContent());
            }
            catch (Exception ex)
            {
                return Json(new { success = false, error = ex.Message });
            }
        }
        [HttpGet]
        public ActionResult CeoGetContent()
        {
            try
            {
                return _contentService.GetContent<CEOPageContent>();
            }
            catch (Exception ex)
            {
                return Json(new { success = false, error = ex.Message }, JsonRequestBehavior.AllowGet);
            }
        }
        #endregion

        #region 單位介紹
        public ActionResult Units()
        {
            try
            {
                ViewBag.Title = "單位介紹";
                Session["ReturnUrl"] = Request.Url.ToString();
                return View();
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

        [HttpPost]
        [ValidateInput(false)]
        public ActionResult unitsSaveContent(string textContent, string ImageSrc)
        {
            try
            {
                var userName = Session["UserName"] as string;
                return _contentService.SaveContent(userName, textContent, ImageSrc, () => new UnitsPageContent());
            }
            catch (Exception ex)
            {
                return Json(new { success = false, error = ex.Message });
            }
        }

        [HttpGet]
        public ActionResult unitsGetContent()
        {
            try
            {
                return _contentService.GetContent<UnitsPageContent>();
            }
            catch (Exception ex)
            {
                return Json(new { success = false, error = ex.Message }, JsonRequestBehavior.AllowGet);
            }
        }
        #endregion
    }
}