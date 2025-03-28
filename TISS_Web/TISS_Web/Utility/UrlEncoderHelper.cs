using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace TISS_Web.Utility
{
    public class UrlEncoderHelper
    {
        public static string EncryptUrl(string title)
        {
            try
            {
                var bytes = System.Text.Encoding.UTF8.GetBytes(title);
                var base64string = Convert.ToBase64String(bytes);
                return base64string.Replace("/", "_").Replace("+", "-"); // URL Safe Base64
            }
            catch
            {
                return string.Empty;
            }
        }
    }
}