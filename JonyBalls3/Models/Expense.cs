using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace JonyBalls3.Models
{
    public class Expense
    {
        [Key]
        public int Id { get; set; }
        
        [Required]
        public int ProjectId { get; set; }
        
        [ForeignKey("ProjectId")]
        public virtual Project Project { get; set; }
        
        public int? StageId { get; set; }
        
        [ForeignKey("StageId")]
        public virtual ProjectStage Stage { get; set; }
        
        [Required]
        [StringLength(200)]
        public string Name { get; set; } = "";
        
        [StringLength(500)]
        public string Description { get; set; } = "";
        
        [Required]
        [DataType(DataType.Currency)]
        public decimal Amount { get; set; }
        
        public ExpenseCategory Category { get; set; }
        
        [DataType(DataType.Date)]
        public DateTime Date { get; set; } = DateTime.Now;
        
        public string ReceiptUrl { get; set; } = "";
    }
    
    public enum ExpenseCategory
    {
        Materials = 1,
        Labor = 2,
        Tools = 3,
        Delivery = 4,
        Other = 5
    }
}
