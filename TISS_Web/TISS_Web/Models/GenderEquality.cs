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
    
    public partial class GenderEquality
    {
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA2214:DoNotCallOverridableMethodsInConstructors")]
        public GenderEquality()
        {
            this.GenderEqualityDetails = new HashSet<GenderEqualityDetails>();
        }
    
        public int Id { get; set; }
        public string Category { get; set; }
        public string Title { get; set; }
        public Nullable<bool> IsEnabled { get; set; }
        public Nullable<System.DateTime> CreatedDate { get; set; }
        public Nullable<System.DateTime> UpdatedDate { get; set; }
        public string CreatedUser { get; set; }
    
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA2227:CollectionPropertiesShouldBeReadOnly")]
        public virtual ICollection<GenderEqualityDetails> GenderEqualityDetails { get; set; }
    }
}
