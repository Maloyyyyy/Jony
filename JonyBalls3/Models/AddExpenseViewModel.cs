using System.ComponentModel.DataAnnotations;

namespace JonyBalls3.Models
{
    public class AddExpenseViewModel
    {
        public int ProjectId { get; set; }
        public int? StageId { get; set; }

        [Required]
        [Display(Name = "Название")]
        public string Name { get; set; }

        [Display(Name = "Описание")]
        public string Description { get; set; }

        [Required]
        [Display(Name = "Сумма")]
        [DataType(DataType.Currency)]
        public decimal Amount { get; set; }

        [Required]
        [Display(Name = "Категория")]
        public string Category { get; set; }

        [Display(Name = "Дата")]
        [DataType(DataType.Date)]
        public DateTime Date { get; set; } = DateTime.Today;

        [Display(Name = "Чек (фото)")]
        public IFormFile ReceiptImage { get; set; }
    }
}
