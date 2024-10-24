using Google.Apis.YouTube.v3.Data;
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
        public int ClickCount { get; set; }
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

        public string FormattedCreateDate { get; set; } // 新增發佈日期屬性

        public string Hashtags { get; set; }

        public DateTime UpdateDate { get; set; }

        public string UpdatedUser { get; set; }

        public bool IsEnabled { get; set; }

        public bool IsPublished { get; set; }

        public Dictionary<string, List<string>> ParentDirectories { get; set; } // 字典來管理父目錄及其子目錄

        // 新增的導航屬性，用於存儲相關的留言
        public ICollection<Messageboard> messageboards { get; set; }

        public string VideoIframe { get; set; } // 用於儲存 iframe 標籤
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

    //首頁的部份視圖
    public class HomeViewModel
    {
        public ArticleContentModel LatestArticle { get; set; }
        public List<ArticleContentModel> OtherArticles { get; set; }
        public List<ArticleContentModel> Videos { get; set; }
    }

    public class Menu
    {
        public int Id { get; set; }
        public string Title { get; set; }
        public virtual ICollection<MenuItemModel> MenuItems { get; set; }
    }

    public class MenuModel
    {
        public int Id { get; set; }
        public string Title { get; set; }
        public List<MenuItemModel> MenuItems { get; set; }
    }

    public class MenuItemModel
    {
        public int Id { get; set; }
        public int MenuId { get; set; }
        public string Name { get; set; }
        public string Url { get; set; }
        public MenuModel Menu { get; set; }
    }

    //忘記密碼
    public class PasswordResetRequest
    {
        public int Id { get; set; }
        public string Email { get; set; }
        public string Token { get; set; }
        public DateTime ExpiryDate { get; set; }
        public string UserAccount { get; set; }
        public DateTime? ChangeDate { get; set; }
    }

    //重置密碼
    public class ResetPasswordViewModel
    {
        public string Token { get; set; }
        public string NewPassword { get; set; }
    }

    public class UsersModel
    {
        public int Id { get; set; }
        public string UserName { get; set; }
        public string Password { get; set; }
        public string Email { get; set; }
        public DateTime CreatedDate { get; set; }
        public DateTime LastLoginDate { get; set; }
        public bool IsActive { get; set; }
        public string UserAccount { get; set; }
        public DateTime? changeDate { get; set; }
    }

    public class Messageboard
    {
        public int Id { get; set; }
        public int ArticleId { get; set; }  // 外鍵
        public string UserName { get; set; }
        public string CommentText { get; set; }
        public DateTime CommentDate { get; set; }
        public bool IsApproved { get; set; }

        // 導航屬性，用於關聯到文章
        public ArticleContentModel Article { get; set; }
        public ICollection<ReplyBoardModel> Replies { get; set; }
    }

    public class ReplyBoardModel
    {
        public int Id { get; set; }
        public int MessageId { get; set; } // 對應留言的外鍵
        public string ReplyName { get; set; }
        public string ReplyText { get; set; }
        public DateTime ReplyDate { get; set; }

        // 導航屬性，用於關聯到留言
        public Messageboard Message { get; set; }
    }

    public class LanguageModel
    {
        public string Lang_Key { get; set; }
        public string Lang_zhTW { get; set; }
        public string Lang_enUS { get; set; }
    }


    public class ArticleReportModel //生成Excel報表
    {
        public string Title { get; set; }
        public int ClickCount { get; set; }
        public string StartDate { get; set; }
        public string EndDate { get; set; }
    }

    public class DocumentModel //法規文件
    {
        public int DocumentID { get; set; }       // 文件 ID
        public string DocumentName { get; set; }  // 文件名稱
        public string DocumentType { get; set; }  // 文件類型 (PDF, DOC, ODT等)
        public DateTime UploadTime { get; set; }  // 上傳時間
        public string Creator { get; set; }       // 上傳者
        public int FileSize { get; set; }         // 文件大小
        public bool IsActive { get; set; }        // 是否啟用
    }
}