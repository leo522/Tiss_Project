using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace TISS_Web.Models
{
    public class ContentService
    {
        private readonly TISS_WebEntities _db;

        public ContentService(TISS_WebEntities db)
        {
            _db = db;
        }

        public JsonResult SaveContent<T>(string textContent, string imageSrc, Func<T> createNewEntity) where T : class, IWebContent, new()
        {
            try
            {
                var userName = HttpContext.Current.Session["UserName"] as string; //從 Session 中獲取已登錄的用戶名

                byte[] imageBytes = null; // 將圖片數據轉換成 byte[]
                if (!string.IsNullOrEmpty(imageSrc))
                {
                    string ba64 = imageSrc.Split(',')[1];
                    imageBytes = Convert.FromBase64String(ba64);
                }

                // 計算新的 FileNo
                //int newFileNo = 1;
                //var lastContent = _db.Set<T>().OrderByDescending(c => c.FileNo).FirstOrDefault();

                //if (lastContent != null)
                //{
                //    newFileNo = lastContent.FileNo + 1;
                //}

                var entity = createNewEntity();
                entity.UserAccount = userName;
                entity.TextContent = textContent;
                entity.TextUpdateTime = DateTime.Now;
                entity.ImageContent = imageBytes;
                entity.ImageUpdateTime = DateTime.Now;
                //entity.FileNo = newFileNo;

                _db.Set<T>().Add(entity);
                _db.SaveChanges();

                string imagePath = $"data:image/jpeg;base64,{Convert.ToBase64String(entity.ImageContent)}";
                return new JsonResult { Data = new { success = true, imagePath = imagePath }, JsonRequestBehavior = JsonRequestBehavior.AllowGet };
            }
            catch (Exception ex)
            {
                return new JsonResult { Data = new { success = false, error = ex.Message }, JsonRequestBehavior = JsonRequestBehavior.AllowGet };
            }
        }

        public JsonResult GetContent<T>() where T : class, IWebContent
        {
            try
            {
                var content = _db.Set<T>().OrderByDescending(c => c.FileNo).FirstOrDefault();

                if (content != null)
                {
                    string imagePath = content.ImageContent != null
                        ? $"data:image/jpeg;base64,{Convert.ToBase64String(content.ImageContent)}"
                        : string.Empty;

                    return new JsonResult
                    {
                        Data = new
                        {
                            success = true,
                            textContent = content.TextContent,
                            imagePath = imagePath
                        },
                        JsonRequestBehavior = JsonRequestBehavior.AllowGet
                    };
                }
                else
                {
                    return new JsonResult { Data = new { success = false, error = "No content found." }, JsonRequestBehavior = JsonRequestBehavior.AllowGet };
                }
            }
            catch (Exception ex)
            {
                return new JsonResult { Data = new { success = false, error = ex.Message }, JsonRequestBehavior = JsonRequestBehavior.AllowGet };
            }
        }
    }
}