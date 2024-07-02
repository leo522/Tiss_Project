using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text.RegularExpressions;
using System.Web;
using System.Web.Configuration;
using System.Web.Mvc;
using System.Web.Security;
using TISS_Web.Models;
using System.Net.NetworkInformation;

namespace TISS_Web.Controllers
{
    public class TissController : Controller
    {
        private TISS_WebEntities _db = new TISS_WebEntities();
        private readonly ContentService _contentService;

        #region 登入&編輯
        public ActionResult Login()
        {
            return View();
        }

        [HttpPost]
        public ActionResult Login(string UserName, string pwd)
        {
            var dto = _db.Users.FirstOrDefault(u => u.UserName == UserName && u.Password == pwd);

            if (dto != null)
            {
                // 驗證成功，更新最後登入時間
                dto.LastLoginDate = DateTime.Now;
                _db.SaveChanges();

                // 設定 Session 狀態為已登入
                Session["LoggedIn"] = true;
                Session["UserName"] = dto.UserName;

                // 檢查是否有記錄的返回頁面
                string returnUrl = Session["ReturnUrl"] != null ? Session["ReturnUrl"].ToString() : Url.Action("Home", "Tiss");

                // 清除返回頁面的 Session 記錄
                Session.Remove("ReturnUrl");

                // 重定向到記錄的返回頁面
                return Redirect(returnUrl);
            }
            else
            {
                // 驗證失敗
                ViewBag.ErrorMessage = "帳號或密碼錯誤";
                return View();
            }
        }
        public ActionResult Logout()
        {
            // 清除所有的 Session 資訊
            Session.Clear();
            Session.Abandon();

            // 清除所有的 Forms 認證 Cookies
            FormsAuthentication.SignOut();

            // 取得登出前的頁面路徑，如果沒有則預設為首頁
            string returnUrl = Request.UrlReferrer != null ? Request.UrlReferrer.ToString() : Url.Action("Home", "Tiss");

            // 重定向到記錄的返回頁面
            return Redirect(returnUrl);
            // 重定向到 Home 頁面
        }

        public ActionResult editPage()
        {
            try
            {
                var dto = _db.AboutPageContent.FirstOrDefault();

                if (dto != null)
                {
                    return View((object)dto.TextContent);
                }
                return View();
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

        [HttpPost]
        [ValidateInput(false)]
        public JsonResult SavePageData(string editorContent)
        {
            try
            {
                var aboutPageContent = _db.AboutPageContent.FirstOrDefault();

                // 更新 AboutPageContent 的 TextContent
                aboutPageContent.TextContent = editorContent;
                aboutPageContent.TextUpdateTime = DateTime.Now;
                // 保存更改到數據庫
                _db.SaveChanges();

                return Json(new { success = true });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, error = ex.Message });
            }
        }
        #endregion

        #region 首頁
        /// <summary>
        /// 首頁
        /// </summary>
        /// <returns></returns>
        public ActionResult Home()
        {
            Session["ReturnUrl"] = Request.Url.ToString();
            return View();
        }
        #endregion

        #region 最新消息-中心公告

        /// <summary>
        /// 中心公告
        /// </summary>
        /// <returns></returns>
        public ActionResult announcement()
        {
            Session["ReturnUrl"] = Request.Url.ToString();
            return View();
        }

        /// <summary>
        /// 影音專區
        /// </summary>
        /// <returns></returns>
        public ActionResult video()
        {
            Session["ReturnUrl"] = Request.Url.ToString();
            return View();
        }

        /// <summary>
        /// 新聞發布
        /// </summary>
        /// <returns></returns>
        public ActionResult press()
        {
            Session["ReturnUrl"] = Request.Url.ToString();
            return View();
        }

        /// <summary>
        /// 中心訊息
        /// </summary>
        /// <returns></returns>
        public ActionResult institute()
        {
            Session["ReturnUrl"] = Request.Url.ToString();
            return View();
        }

        /// <summary>
        /// 徵才招募
        /// </summary>
        /// <returns></returns>
        public ActionResult recruit()
        {
            Session["ReturnUrl"] = Request.Url.ToString();
            return View();
        }

        /// <summary>
        /// 委託研究計劃
        /// </summary>
        /// <returns></returns>
        public ActionResult researchProject()
        {
            Session["ReturnUrl"] = Request.Url.ToString();
            return View();
        }

        /// <summary>
        /// 政府網站服務管理規範
        /// </summary>
        /// <returns></returns>
        public ActionResult GovernmentWebsite()
        {
            Session["ReturnUrl"] = Request.Url.ToString();
            return View();
        }

        public ActionResult Shotting()
        {
            Session["ReturnUrl"] = Request.Url.ToString();
            return View();
        }

        #endregion

        #region 中心介紹
        /// <summary>
        /// 中心介紹
        /// </summary>
        /// <returns></returns>
        public ActionResult about()
        {
            Session["ReturnUrl"] = Request.Url.ToString();
            //var dto = _db.AboutPageContent.FirstOrDefault();
            //var ba64Img = Convert.ToBase64String(dto.ImageContent);
            //ViewBag.ImgSrc = $"data:image/jpeg;base64,{ba64Img}";
            //return View(dto);
            return View();
        }

        [HttpPost]
        [ValidateInput(false)]
        public ActionResult SaveContent(string textContent, string ImageSrc)
        {
            try
            {
                var userName = Session["UserName"] as string; //從Session中獲取已登錄的帳號

                byte[] imageBytes = null; // 將圖片數據轉換成byte[]
                if (!string.IsNullOrEmpty(ImageSrc))
                {
                    string ba64 = ImageSrc.Split(',')[1];
                    imageBytes = Convert.FromBase64String(ba64);
                }
                // 計算新的 FileNo
                int newFileNo = 1;
                var lastNo = _db.AboutPageContent.OrderByDescending(f => f.FileNo).FirstOrDefault();
                if (lastNo != null)
                {
                    newFileNo = lastNo.FileNo + 1;
                }

                var dtos = new AboutPageContent() //中心介紹
                {
                    UserAccount = userName,
                    TextContent = textContent,
                    TextUpdateTime = DateTime.Now,
                    ImageContent = imageBytes,
                    ImageUpdateTime = DateTime.Now,
                    FileNo = newFileNo
                };
                _db.AboutPageContent.Add(dtos);
                _db.SaveChanges();

                string imagePath = $"data:image/jpeg;base64,{Convert.ToBase64String(dtos.ImageContent)}";
                return Json(new { success = true, imagePath = imagePath });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, error = ex.Message });
            }
        }

        [HttpGet]
        public ActionResult GetContent()
        {
            try
            {
                var content = _db.AboutPageContent
                        .OrderByDescending(c => c.FileNo)
                        .FirstOrDefault();

                if (content != null)
                {
                    string imagePath = content.ImageContent != null
                ? $"data:image/jpeg;base64,{Convert.ToBase64String(content.ImageContent)}"
                : string.Empty;

                    return Json(new
                    {
                        success = true,
                        textContent = content.TextContent,
                        imagePath = imagePath
                    }, JsonRequestBehavior.AllowGet);
                }
                else
                {
                    return Json(new { success = false, error = "讀取錯誤" }, JsonRequestBehavior.AllowGet);
                }
            }
            catch (Exception ex)
            {
                return Json(new { success = false, error = ex.Message }, JsonRequestBehavior.AllowGet);
            }
        }

        /// <summary>
        /// 使命願景
        /// </summary>
        /// <returns></returns>
        public ActionResult Objectives()
        {
            Session["ReturnUrl"] = Request.Url.ToString();
            return View();
        }



        /// <summary>
        /// 任務
        /// </summary>
        /// <returns></returns>
        public ActionResult mission()
        {
            Session["ReturnUrl"] = Request.Url.ToString();
            return View();
        }

        /// <summary>
        /// 組織概況
        /// </summary>
        /// <returns></returns>
        public ActionResult Organization()
        {
            Session["ReturnUrl"] = Request.Url.ToString();
            return View();
        }

        /// <summary>
        /// 董監事
        /// </summary>
        /// <returns></returns>
        public ActionResult BOD()
        {
            Session["ReturnUrl"] = Request.Url.ToString();
            return View();
        }

        /// <summary>
        /// 執行長
        /// </summary>
        /// <returns></returns>
        public ActionResult CEO()
        {
            Session["ReturnUrl"] = Request.Url.ToString();
            return View();
        }

        /// <summary>
        /// 執行長
        /// </summary>
        /// <returns></returns>
        public ActionResult Units()
        {
            Session["ReturnUrl"] = Request.Url.ToString();
            return View();
        }

        #endregion

        #region 科普專欄

        /// <summary>
        /// 科普專欄
        /// </summary>
        /// <returns></returns>
        public ActionResult research()
        {
            Session["ReturnUrl"] = Request.Url.ToString();
            return View();
        }

        /// <summary>
        /// 運動科學研究
        /// </summary>
        /// <returns></returns>
        public ActionResult sportScience()
        {
            Session["ReturnUrl"] = Request.Url.ToString();
            return View();
        }

        /// <summary>
        /// 運動科技與資訊開發
        /// </summary>
        /// <returns></returns>
        public ActionResult sportTech()
        {
            Session["ReturnUrl"] = Request.Url.ToString();
            return View();
        }

        /// <summary>
        /// 運動醫學研究
        /// </summary>
        /// <returns></returns>
        public ActionResult sportMedicine()
        {
            Session["ReturnUrl"] = Request.Url.ToString();
            return View();
        }

        #endregion

        #region 公開資訊
        /// <summary>
        /// 公開資訊
        /// </summary>
        /// <returns></returns>
        public ActionResult public_info()
        {
            Session["ReturnUrl"] = Request.Url.ToString();
            return View();
        }

        /// <summary>
        /// 法規
        /// </summary>
        /// <returns></returns>
        public ActionResult regulation()
        {
            Session["ReturnUrl"] = Request.Url.ToString();
            return View();
        }

        /// <summary>
        /// 辦法及要點
        /// </summary>
        /// <returns></returns>
        public ActionResult procedure()
        {
            Session["ReturnUrl"] = Request.Url.ToString();
            return View();
        }

        /// <summary>
        /// 計畫
        /// </summary>
        /// <returns></returns>
        public ActionResult plan()
        {
            Session["ReturnUrl"] = Request.Url.ToString();
            return View();
        }

        /// <summary>
        /// 預算與決算
        /// </summary>
        /// <returns></returns>
        public ActionResult budget()
        {
            Session["ReturnUrl"] = Request.Url.ToString();
            return View();
        }

        /// <summary>
        /// 下載專區
        /// </summary>
        /// <returns></returns>
        public ActionResult download()
        {
            Session["ReturnUrl"] = Request.Url.ToString();
            return View();
        }

        /// <summary>
        /// 採購作業實施規章
        /// </summary>
        /// <returns></returns>
        public ActionResult purchase()
        {
            Session["ReturnUrl"] = Request.Url.ToString();
            return View();
        }

        /// <summary>
        /// 其他
        /// </summary>
        /// <returns></returns>
        public ActionResult other()
        {
            Session["ReturnUrl"] = Request.Url.ToString();
            return View();
        }
        #endregion

        #region 性平專區
        public ActionResult GenderEquality()
        {
            Session["ReturnUrl"] = Request.Url.ToString();
            return View();
        }
        #endregion

        #region 寫入文字和圖片
        public ActionResult webContent()
        {
            return View();
        }

        [HttpPost]
        public ActionResult UploadContent(AboutPageContentModel model, HttpPostedFileBase imageFile)
        {
            if (ModelState.IsValid)
            {
                var aboutPageContent = new AboutPageContent
                {
                    TextContent = model.TextContent,
                    FileUploadTime = DateTime.Now,
                    TextUpdateTime = DateTime.Now,
                    UserAccount = model.UserAccount,
                    UserLoginTime = DateTime.Now,
                    VideoURL = model.VideoUrl,
                    VideoUpdateTime = DateTime.Now,
                    ImageContent = model.ImageContent,
                    ImageUpdateTime = DateTime.Now,
                    WebsiteURL = model.WebsiteUrl,
                    WebsiteURLUpdateTime = DateTime.Now,
                };

                if (imageFile != null && imageFile.ContentLength > 0)
                {
                    using (var reader = new System.IO.BinaryReader(imageFile.InputStream))
                    {
                        aboutPageContent.ImageContent = reader.ReadBytes(imageFile.ContentLength);
                    }
                    aboutPageContent.ImageUpdateTime = DateTime.Now;
                }

                _db.AboutPageContent.Add(aboutPageContent);
                _db.SaveChanges();

                return RedirectToAction("WebContent");
            }
            return View("WebContent", model);

        }
        #endregion

        #region 通用
        public TissController()
        {
            var db = new TISS_WebEntities();
            _contentService = new ContentService(db);
        }

        /// <summary>
        /// 中心介紹_存檔
        /// </summary>
        /// <param name="textContent"></param>
        /// <param name="imageSrc"></param>
        /// <returns></returns>
        [HttpPost]
        [ValidateInput(false)]
        public ActionResult SaveAboutPageContent(string textContent, string imageSrc)
        {
            return _contentService.SaveContent<AboutPageContentModel>(textContent, imageSrc, () => new AboutPageContentModel());
        }

        /// <summary>
        /// 使命願景_存檔
        /// </summary>
        /// <param name="textContent"></param>
        /// <param name="imageSrc"></param>
        /// <returns></returns>
        [HttpPost]
        [ValidateInput(false)]
        public ActionResult SaveObjectivesPageContent(string textContent, string imageSrc)
        {
            return _contentService.SaveContent<ObjectivesPageContentModel>(textContent, imageSrc, () => new ObjectivesPageContentModel());
        }

        /// <summary>
        /// 中心任務_存檔
        /// </summary>
        /// <param name="textContent"></param>
        /// <param name="imageSrc"></param>
        /// <returns></returns>
        [HttpPost]
        [ValidateInput(false)]
        public ActionResult SaveMissionPageContent(string textContent, string imageSrc)
        {
            return _contentService.SaveContent<MissionPageContentModel>(textContent, imageSrc, () => new MissionPageContentModel());
        }

        /// <summary>
        /// 中心介紹
        /// </summary>
        /// <returns></returns>
        [HttpGet]
        public ActionResult GetAboutPageContent()
        {
            return _contentService.GetContent<AboutPageContentModel>();
        }

        /// <summary>
        /// 使命願景
        /// </summary>
        /// <returns></returns>
        [HttpGet]
        public ActionResult GetObjectivesPageContent()
        {
            return _contentService.GetContent<ObjectivesPageContentModel>();
        }

        /// <summary>
        /// 中心任務
        /// </summary>
        /// <returns></returns>
        [HttpGet]
        public ActionResult GetMissionPageContent()
        {
            return _contentService.GetContent<MissionPageContentModel>();
        }
        #endregion
    }
}