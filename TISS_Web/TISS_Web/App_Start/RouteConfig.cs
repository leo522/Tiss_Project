﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using System.Web.Routing;

namespace TISS_Web
{
    public class RouteConfig
    {
        public static void RegisterRoutes(RouteCollection routes)
        {
            routes.IgnoreRoute("{resource}.axd/{*pathInfo}");

            // 新增檔案上傳的路由
            routes.MapRoute(
                 name: "UploadPDF",
            url: "Tiss/UploadPDF",
            defaults: new { controller = "Tiss", action = "UploadPDF" }
            );

            // 預設路由
            routes.MapRoute(
                name: "Default",
                url: "{controller}/{action}/{id}",
                //defaults: new { controller = "Home", action = "Index", id = UrlParameter.Optional }
                defaults: new { controller = "Tiss", action = "Home", id = UrlParameter.Optional }
            );

            routes.MapRoute(
           name: "Error",
           url: "Error/{action}/{id}",
           defaults: new { controller = "Error", action = "Error404", id = UrlParameter.Optional }
       );

        }
    }
}