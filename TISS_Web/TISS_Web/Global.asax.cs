using System;
using System.Collections.Generic;
using System.Data.Entity.Core.Metadata.Edm;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using System.Web.Optimization;
using System.Web.Routing;
using TISS_Web.Models;

namespace TISS_Web
{
    public class MvcApplication : System.Web.HttpApplication
    {
        protected void Application_Start()
        {
            AreaRegistration.RegisterAllAreas();
            FilterConfig.RegisterGlobalFilters(GlobalFilters.Filters);
            RouteConfig.RegisterRoutes(RouteTable.Routes);
            BundleConfig.RegisterBundles(BundleTable.Bundles);
        }

        protected void Application_BeginRequest(Object sender, EventArgs e)
        {
            HttpCookie objMyLanguage = Request.Cookies["MyLanguage"];

            if (objMyLanguage != null) 
            {
                System.Threading.Thread.CurrentThread.CurrentCulture = new System.Globalization.CultureInfo(objMyLanguage.Value);
                System.Threading.Thread.CurrentThread.CurrentUICulture = new System.Globalization.CultureInfo(objMyLanguage.Value);
            }
            //// �w�]���c�餤��
            //string culture = "zh-TW";

            //if (Request.Cookies["lang"] != null)
            //{
            //    culture = Request.Cookies["lang"].Value;
            //}

            //System.Threading.Thread.CurrentThread.CurrentCulture = new System.Globalization.CultureInfo(culture);
            //System.Threading.Thread.CurrentThread.CurrentUICulture = new System.Globalization.CultureInfo(culture);
        }

        protected void Application_Error()
        {
            Exception exception = Server.GetLastError();
            Server.ClearError();

            // �T�{�O�_�� 404 ���~
            var httpException = exception as HttpException;
            if (httpException != null && httpException.GetHttpCode() == 404)
            {
                Response.Redirect("~/Error404");
            }
            else
            {
                // �p�G���O 404 ���~�A�i�H��ܳq�Ϊ����~����
                Response.Redirect("~/Error404");
            }
        }
    }
}
