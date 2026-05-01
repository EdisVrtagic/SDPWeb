using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
namespace SDPWebApp.Models
{
    public class ValidationIssue
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int DocumentId { get; set; }

        [ForeignKey("DocumentId")]
        public virtual Document? Document { get; set; }

        [Required]
        public string ErrorMessage { get; set; } = string.Empty;

        public string IssueType { get; set; } = string.Empty;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}
