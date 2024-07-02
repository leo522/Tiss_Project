using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Configuration;

namespace TISS_Web.Models
{
    public class WebContentModel : IWebContent
    {
        public int ID { get; set; }
        public string TextContent { get; set; }
        public byte[] ImageContent { get; set; }
        public DateTime FileUploadTime { get; set; }
        public DateTime TextUpdateTime { get; set; }
        public DateTime ImageUpdateTime { get; set; }
        public string UserAccount { get; set; }
        public DateTime UserLoginTime { get; set; }
        public string VideoUrl { get; set; }
        public DateTime VideoUpdateTime { get; set; }
        public string WebsiteUrl { get; set; }
        public DateTime WebsiteUpdateTime { get; set; }
        public int FileNo {  get; set; }
    }

    public class AboutPageContentModel : WebContentModel
    {

    }

    public class AnnouncementPageContentModel : WebContentModel
    {

    }

    public class BODPageContentModel : WebContentModel
    {

    }

    public class BudgetPageContentModel : WebContentModel
    {

    }

    public class CEOPageContentModel : WebContentModel
    {

    }

    public class DownloadPageContentModel : WebContentModel
    {

    }

    public class GenderEqualityPageContentModel : WebContentModel
    {

    }

    public class GovernmentWebsitePageContentModel : WebContentModel
    {

    }

    public class HomePageContentModel : WebContentModel
    {

    }

    public class InstitutePageContentModel : WebContentModel
    {

    }

    public class MissionPageContentModel : WebContentModel
    {

    }

    public class ObjectivesPageContentModel : WebContentModel
    {

    }

    public class OrganizationPageContentModel : WebContentModel
    {

    }

    public class OtherPageContentModel : WebContentModel
    {

    }

    public class PlanPageContentModel : WebContentModel
    {

    }

    public class PressPageContentModel : WebContentModel
    {

    }

    public class ProcedurePageContentModel : WebContentModel
    {

    }

    public class Public_InfoPageContentModel : WebContentModel
    {

    }

    public class PurchasePageContentModel : WebContentModel
    {

    }

    public class RecruitPageContentModel : WebContentModel
    {

    }

    public class RegulationPageContentModel : WebContentModel
    {

    }

    public class ResearchPageContentModel : WebContentModel
    {

    }

    public class ResearchProjectPageContentModel : WebContentModel
    {

    }

    public class SportMedicinePageContentModel : WebContentModel
    {

    }

    public class SportSciencePageContentModel : WebContentModel
    {

    }

    public class SportTechPageContentModel : WebContentModel
    {

    }

    public class UnitsPageContentModel : WebContentModel
    {

    }

    public class VideoPageContentModel : WebContentModel
    {

    }
}