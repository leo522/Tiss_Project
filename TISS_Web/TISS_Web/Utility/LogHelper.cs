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
    }
}