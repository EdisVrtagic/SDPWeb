using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
namespace SDPWebApp.Models
{
    public class DocumentItem
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int DocumentId { get; set; }

        [ForeignKey("DocumentId")]
        public virtual Document? Document { get; set; }

        [Required]
        public string Description { get; set; } = string.Empty;

        public double Quantity { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal UnitPrice { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal LineTotal { get; set; }
    }
}
