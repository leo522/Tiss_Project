//------------------------------------------------------------------------------
// <auto-generated>
//     這個程式碼是由範本產生。
//
//     對這個檔案進行手動變更可能導致您的應用程式產生未預期的行為。
//     如果重新產生程式碼，將會覆寫對這個檔案的手動變更。
// </auto-generated>
//------------------------------------------------------------------------------

namespace TISS_Web.Models
{
    using System;
    using System.Collections.Generic;
    
    public partial class PasswordResetRequests
    {
        public int Id { get; set; }
        public string Email { get; set; }
        public string Token { get; set; }
        public System.DateTime ExpiryDate { get; set; }
        public string UserAccount { get; set; }
        public System.DateTime changeDate { get; set; }
    }
}
