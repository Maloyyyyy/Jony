using System.ComponentModel.DataAnnotations;

namespace JonyBalls3.Models
{
    public class ExpenseViewModel
    {
        public int Id { get; set; }
        public int ProjectId { get; set; }
        public int? StageId { get; set; }

        [Required(ErrorMessage = "Название обязательно")]
        [Display(Name = "Название")]
        public string Name { get; set; }

        [Display(Name = "Описание")]
        public string Description { get; set; }

        [Required(ErrorMessage = "Сумма обязательна")]
        [Display(Name = "Сумма (руб.)")]
        [Range(1, 10000000)]
        public decimal Amount { get; set; }

        [Required(ErrorMessage = "Категория обязательна")]
        [Display(Name = "Категория")]
        public string Category { get; set; }

        [Display(Name = "Дата")]
        [DataType(DataType.Date)]
        public DateTime Date { get; set; } = DateTime.Today;

        [Display(Name = "Чек/документ")]
        public IFormFile Receipt { get; set; }

        public string ReceiptUrl { get; set; }
    }
}
