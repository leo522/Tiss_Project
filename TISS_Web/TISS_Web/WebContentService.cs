using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using TISS_Web.Models;

namespace TISS_Web
{
    public class WebContentService
    {
        private readonly TISS_WebEntities _context;

        public WebContentService(TISS_WebEntities context)
        {
            _context = context;
        }

        public JsonResult SaveContent<T>(string userName, string textContent, string imageSrc, Func<T> createEntity) where T : class
        {
            byte[] imageBytes = null;

            if (!string.IsNullOrEmpty(imageSrc))
            {
                string[] ba64 = imageSrc.Split(',');

                if (ba64.Length == 2)
                {
                    string mimeType = ba64[0].Split(':')[1].Split(';')[0];
                    string dto = ba64[1];

                    if (mimeType == "image/jpeg" || mimeType == "image/jpg" || mimeType == "image/png")
                    {
                        imageBytes = Convert.FromBase64String(dto);
                        if (imageBytes.Length > 5 * 1024 * 1024) return new JsonResult { Data = new { success = false, error = "圖片大小不能超過5MB" } };
                    }
                    else return new JsonResult { Data = new { success = false, error = "只允許上傳jpg、jpeg或png格式的圖片" } };
                }
            }

            int newFileNo = (_context.Set<T>().Max(d => (int?)d.GetType().GetProperty("FileNo").GetValue(d)) ?? 0) + 1;

            var entity = createEntity();
            entity.GetType().GetProperty("UserAccount").SetValue(entity, userName);
            entity.GetType().GetProperty("TextContent").SetValue(entity, textContent);
            entity.GetType().GetProperty("TextUpdateTime").SetValue(entity, DateTime.Now);
            entity.GetType().GetProperty("ImageContent").SetValue(entity, imageBytes);
            entity.GetType().GetProperty("ImageUpdateTime").SetValue(entity, DateTime.Now);
            entity.GetType().GetProperty("FileNo").SetValue(entity, newFileNo);

            _context.Set<T>().Add(entity);
            _context.SaveChanges();

            string imagePath = imageBytes != null ? $"data:image/jpeg;base64,{Convert.ToBase64String(imageBytes)}" : string.Empty;
            return new JsonResult { Data = new { success = true, imagePath } };
        }

        public JsonResult GetContent<T>() where T : class
        {
            try
            {
                var entityType = typeof(T);
                var property = entityType.GetProperty("FileNo");

                if (property == null)
                {
                    throw new Exception("FileNo property not found");
                }

                var content = _context.Set<T>().ToList().OrderByDescending(d => property.GetValue(d)).FirstOrDefault();

                if (content != null)
                {
                    var imageContentProperty = entityType.GetProperty("ImageContent");
                    var textContentProperty = entityType.GetProperty("TextContent");
                    var imageContent = (byte[])imageContentProperty.GetValue(content);
                    var textContent = (string)textContentProperty.GetValue(content);

                    string imagePath = imageContent != null ? $"data:image/jpeg;base64,{Convert.ToBase64String(imageContent)}" : string.Empty;
                    textContent = System.Text.RegularExpressions.Regex.Replace(textContent, @"\s+", " ").Trim();

                    return new JsonResult { Data = new { success = true, textContent, imagePath }, JsonRequestBehavior = JsonRequestBehavior.AllowGet };
                }
                else
                {
                    return new JsonResult { Data = new { success = false, error = "讀取錯誤" }, JsonRequestBehavior = JsonRequestBehavior.AllowGet };
                }
            }
            catch (Exception ex)
            {
                return new JsonResult { Data = new { success = false, error = ex.Message }, JsonRequestBehavior = JsonRequestBehavior.AllowGet };
            }
        }
    }
}