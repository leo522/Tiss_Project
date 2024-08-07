using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace TISS_Web.Controllers
{
    public class MaintenanceController : Controller
    {
        #region 網站停機維護公告

        public ActionResult Index()
        {
            return View("Index");
        }

        #endregion
    }
}