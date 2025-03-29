using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Web;
using TISS_Web.Models;

namespace TISS_Web.Utility
{
    public class LogHelper
    {
        #region 影音專區Log檔
        public static void WriteAduioVideoLog(string action, string title, Exception ex)
        {
            try
            {
                using (var db = new TISS_WebEntities())
                {
                    var log = new AduioVideoAreaLog
                    {
                        ActionName = action,
                        LogTitle = title,
                        LogMessage = ex.Message,
                        StackTrace = ex.StackTrace,
                        LogTime = DateTime.Now
                    };

                    db.AduioVideoAreaLog.Add(log);
                    db.SaveChanges();
                }
            }
            catch
            {
            }
        }
        #endregion

        #region 內規文件Log檔
        public static void WriteInternalPolicyLog(string action, string title, Exception ex)
        {
            try
            {
                using (var db = new TISS_WebEntities())
                {
                    var log = new InternalPolicyLog
                    {
                        ActionName = action,
                        LogTitle = title,
                        LogMessage = ex.Message,
                        StackTrace = ex.StackTrace,
                        LogTime = DateTime.Now
                    };

                    db.InternalPolicyLog.Add(log);
                    db.SaveChanges();
                }
            }
            catch
            {
            }
        }
        #endregion

        #region 專欄文章Log檔
        public static void WritePopularScienceLog(string action, string title, Exception ex)
        {
            try
            {
                using (var db = new TISS_WebEntities())
                {
                    var log = new PopularScienceLog
                    {
                        ActionName = action,
                        LogTitle = title,
                        LogMessage = ex.Message,
                        StackTrace = ex.StackTrace,
                        LogTime = DateTime.Now
                    };

                    db.PopularScienceLog.Add(log);
                    db.SaveChanges();
                }
            }
            catch
            {
            }
        }
        #endregion
    }
}