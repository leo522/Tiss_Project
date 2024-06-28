using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using System.Web.Security;
using TISS_Web.Models;

namespace TISS_Web.Controllers
{
    public class TissController : Controller
    {
        private TISS_WebEntities _db = new TISS_WebEntities();


        #region 登入&編輯
        public ActionResult Login()
        { 
            return View();
        }

        [HttpPost]
        public ActionResult Login(string UserName , string pwd)
        {
            var dto = _db.Users.FirstOrDefault(u => u.UserName == UserName && u.Password == pwd);

            if (dto != null)
            {
                // 驗證成功，更新最後登入時間
                dto.LastLoginDate = DateTime.Now;
                _db.SaveChanges();

                // 重定向到 editPage
                return RedirectToAction("editPage", "Tiss");
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
            // 登出
            FormsAuthentication.SignOut();

            // 重定向到 Home 頁面
            return RedirectToAction("Home", "Tiss");
        }

        public ActionResult editPage() 
        { 
            return View(); 
        }
        #endregion

        #region 首頁
        /// <summary>
        /// 首頁
        /// </summary>
        /// <returns></returns>
        public ActionResult Home()
        {
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
            return View();
        }

        /// <summary>
        /// 影音專區
        /// </summary>
        /// <returns></returns>
        public ActionResult video()
        {
            return View();
        }

        /// <summary>
        /// 新聞發布
        /// </summary>
        /// <returns></returns>
        public ActionResult press()
        {
            return View();
        }

        /// <summary>
        /// 中心訊息
        /// </summary>
        /// <returns></returns>
        public ActionResult institute() 
        {
            return View();
        }

        /// <summary>
        /// 徵才招募
        /// </summary>
        /// <returns></returns>
        public ActionResult recruit() 
        {
            return View();
        }

        /// <summary>
        /// 委託研究計劃
        /// </summary>
        /// <returns></returns>
        public ActionResult researchProject()
        {
            return View();
        }

        /// <summary>
        /// 政府網站服務管理規範
        /// </summary>
        /// <returns></returns>
        public ActionResult GovernmentWebsite() 
        {
            return View();
        }

        public ActionResult Shotting()
        {
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
            return View();
        }

        /// <summary>
        /// 使命願景
        /// </summary>
        /// <returns></returns>
        public ActionResult Objectives()
        {
            return View();
        }

        /// <summary>
        /// 任務
        /// </summary>
        /// <returns></returns>
        public ActionResult mission()
        {
            return View();
        }

        /// <summary>
        /// 組織概況
        /// </summary>
        /// <returns></returns>
        public ActionResult Organization()
        {
            return View();
        }

        /// <summary>
        /// 董監事
        /// </summary>
        /// <returns></returns>
        public ActionResult BOD()
        {
            return View();
        }

        /// <summary>
        /// 執行長
        /// </summary>
        /// <returns></returns>
        public ActionResult CEO()
        {
            return View();
        }

        /// <summary>
        /// 執行長
        /// </summary>
        /// <returns></returns>
        public ActionResult Units()
        {
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
            return View();
        }

        /// <summary>
        /// 運動科學研究
        /// </summary>
        /// <returns></returns>
        public ActionResult sportScience()
        {
            return View();
        }

        /// <summary>
        /// 運動科技與資訊開發
        /// </summary>
        /// <returns></returns>
        public ActionResult sportTech()
        {
            return View();
        }

        /// <summary>
        /// 運動醫學研究
        /// </summary>
        /// <returns></returns>
        public ActionResult sportMedicine()
        {
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
            return View();
        }

        /// <summary>
        /// 法規
        /// </summary>
        /// <returns></returns>
        public ActionResult regulation()
        {
            return View();
        }

        /// <summary>
        /// 辦法及要點
        /// </summary>
        /// <returns></returns>
        public ActionResult procedure()
        {
            return View();
        }

        /// <summary>
        /// 計畫
        /// </summary>
        /// <returns></returns>
        public ActionResult plan()
        {
            return View();
        }

        /// <summary>
        /// 預算與決算
        /// </summary>
        /// <returns></returns>
        public ActionResult budget()
        {
            return View();
        }

        /// <summary>
        /// 下載專區
        /// </summary>
        /// <returns></returns>
        public ActionResult download()
        {
            return View();
        }

        /// <summary>
        /// 採購作業實施規章
        /// </summary>
        /// <returns></returns>
        public ActionResult purchase()
        {
            return View();
        }

        /// <summary>
        /// 其他
        /// </summary>
        /// <returns></returns>
        public ActionResult other()
        {
            return View();
        }
        #endregion

        #region 性平專區
        public ActionResult GenderEquality() 
        {
            return View();
        }
        #endregion
    }
}