using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TISS_Web.Models
{
    public interface IWebContent
    {
            int ID { get; set; }
            int FileNo { get; set; }
            string TextContent { get; set; }
            byte[] ImageContent { get; set; }
            DateTime FileUploadTime { get; set; }
            DateTime TextUpdateTime { get; set; }
            DateTime ImageUpdateTime { get; set; }
            string UserAccount { get; set; }
            DateTime UserLoginTime { get; set; }
            string VideoUrl { get; set; }
            DateTime VideoUpdateTime { get; set; }
            string WebsiteUrl { get; set; }
            DateTime WebsiteUpdateTime { get; set; }
    }
}
