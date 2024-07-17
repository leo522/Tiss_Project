using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace TISS_Web.Models
{
    public class WebContentModel
    {
        public int ID { get; set; }
        public int FileNo { get; set; }
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
        public int ClickCount {  get; set; }
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

    public class SportsPhysiologyPageContentModel : WebContentModel
    {
    }
    
    public class SportsPsychologyPageContentModel : WebContentModel
    {
    }
    
    public class PhysicalTrainingContentModel : WebContentModel
    {
    }
    
    public class SportsNutritionPageContentModel : WebContentModel
    {
    }

    public class UnitsPageContentModel : WebContentModel
    {
    }

    public class VideoPageContentModel : WebContentModel
    {
    }

    public class FileDocumentModel
    {
        public int ID { get; set; }
        public int PId { get; set; }
        public string DocumentName { get; set; }
        public DateTime UploadTime { get; set; }
        public string Creator { get; set; }
        public string DocumentType { get; set; }
        public int FileSize { get; set; }
        public DateTime LastModifiedTime { get; set; }
        public int DownloadCount { get; set; }
    }

    public class RegulationDocumentModel
    {
        public int ID { get; set; }
        public int PId { get; set; }
        public string DocumentName { get; set; }
        public DateTime UploadTime { get; set; }
        public string Creator { get; set; }
        public string DocumentType { get; set; }
        public int FileSize { get; set; }
        public DateTime ModifiedTime { get; set; }
        public bool IsActive { get; set; }
    }

    public class ProcedureDocumentModel
    {
        public int ID { get; set; }
        public int PId { get; set; }
        public string DocumentName { get; set; }
        public DateTime UploadTime { get; set; }
        public string Creator { get; set; }
        public string DocumentType { get; set; }
        public int FileSize { get; set; }
        public DateTime ModifiedTime { get; set; }
        public bool IsActive { get; set; }
    }

    public class PlanDocumentModel
    {
        public int ID { get; set; }
        public int PId { get; set; }
        public string DocumentName { get; set; }
        public DateTime UploadTime { get; set; }
        public string Creator { get; set; }
        public string DocumentType { get; set; }
        public int FileSize { get; set; }
        public DateTime ModifiedTime { get; set; }
        public bool IsActive { get; set; }
    }

    public class BudgetDocumentModel
    {
        public int ID { get; set; }
        public int PId { get; set; }
        public string DocumentName { get; set; }
        public DateTime UploadTime { get; set; }
        public string Creator { get; set; }
        public string DocumentType { get; set; }
        public int FileSize { get; set; }
        public DateTime ModifiedTime { get; set; }
        public bool IsActive { get; set; }
    }

    public class DownloadDocumentModel
    {
        public int ID { get; set; }
        public int PId { get; set; }
        public string DocumentName { get; set; }
        public DateTime UploadTime { get; set; }
        public string Creator { get; set; }
        public string DocumentType { get; set; }
        public int FileSize { get; set; }
        public DateTime ModifiedTime { get; set; }
        public bool IsActive { get; set; }
    }

    public class PurchaseDocumentModel
    {
        public int ID { get; set; }
        public int PId { get; set; }
        public string DocumentName { get; set; }
        public DateTime UploadTime { get; set; }
        public string Creator { get; set; }
        public string DocumentType { get; set; }
        public int FileSize { get; set; }
        public DateTime ModifiedTime { get; set; }
        public bool IsActive { get; set; }
    }

    public class OtherDocumentModel
    {
        public int ID { get; set; }
        public int PId { get; set; }
        public string DocumentName { get; set; }
        public DateTime UploadTime { get; set; }
        public string Creator { get; set; }
        public string DocumentType { get; set; }
        public int FileSize { get; set; }
        public DateTime ModifiedTime { get; set; }
        public bool IsActive { get; set; }
    }

    public class ArticleContentModel
    {
        public int Id { get; set; }

        public string Title { get; set; }

        [AllowHtml]
        public string BodyContent { get; set; }

        public byte[] ImageContent { get; set; }

        public DateTime PublishedDate { get; set; }

        public string EncryptedUrl { get; set; }

        public int ClickCount { get; set; }

        public int ContentTypeId { get; set; } 

        public string ContentType { get; set; } 

        public string CreateUser { get; set; }

        public DateTime CreateDate { get; set; }

        public string Hashtags { get; set; }

        public DateTime UpdateDate { get; set; }

        public string UpdatedUser { get; set; }

        public bool IsEnabled { get; set; }

        public Dictionary<string, List<string>> ParentDirectories { get; set; } // 字典來管理父目錄及其子目錄
    }

    public class HashtagModle
    {
        public int Id { get; set; }

        public string Name { get; set; }
    }

    public class ArticleCategoryModel 
    {
        public int Id { get; set; }
        public string CategoryName { get; set; }
    }
}