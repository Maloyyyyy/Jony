using System.ComponentModel.DataAnnotations;

namespace JonyBalls3.Models
{
    public class ProjectViewModel
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "Название проекта обязательно")]
        [Display(Name = "Название проекта")]
        public string Name { get; set; }

        [Display(Name = "Описание")]
        public string Description { get; set; }

        [Required(ErrorMessage = "Площадь обязательна")]
        [Display(Name = "Площадь (м²)")]
        [Range(1, 1000, ErrorMessage = "Площадь должна быть от 1 до 1000 м²")]
        public decimal Area { get; set; }

        [Required(ErrorMessage = "Тип ремонта обязателен")]
        [Display(Name = "Тип ремонта")]
        public string RepairType { get; set; }

        [Required(ErrorMessage = "Бюджет обязателен")]
        [Display(Name = "Бюджет (руб.)")]
        [Range(1000, 10000000, ErrorMessage = "Бюджет должен быть от 1 000 до 10 000 000 руб.")]
        public decimal Budget { get; set; }

        [Display(Name = "Дата начала")]
        [DataType(DataType.Date)]
        public DateTime? StartDate { get; set; }

        [Display(Name = "Дата окончания")]
        [DataType(DataType.Date)]
        public DateTime? EndDate { get; set; }

        [Display(Name = "Статус")]
        public string Status { get; set; } = "Планирование";

        // Для расчета из калькулятора
        public bool FromCalculator { get; set; }
        public decimal? CalculatedTotal { get; set; }
    }
}
