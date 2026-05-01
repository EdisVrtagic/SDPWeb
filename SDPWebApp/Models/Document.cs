using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SDPWebApp.Models
{
    public enum DocumentStatus { Uploaded, NeedsReview, Validated, Rejected }
    public enum DocumentType { Invoice, PurchaseOrder, Unknown }

    public class Document
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [StringLength(255)]
        public string FileName { get; set; } = string.Empty;

        [Required]
        public string FilePath { get; set; } = string.Empty;

        public DateTime UploadedAt { get; set; } = DateTime.UtcNow;
        [Required]
        public DocumentStatus Status { get; set; } = DocumentStatus.Uploaded;

        [Required]
        public DocumentType Type { get; set; } = DocumentType.Unknown;
        public string? SupplierName { get; set; }
        public string? DocumentNumber { get; set; }
        public DateTime? IssueDate { get; set; }
        public DateTime? DueDate { get; set; }
        public string? Currency { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal Subtotal { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal TaxAmount { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal TotalAmount { get; set; }
        public virtual ICollection<DocumentItem> LineItems { get; set; } = [];
        public virtual ICollection<ValidationIssue> ValidationIssues { get; set; } = [];
    }
}