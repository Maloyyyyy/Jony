using System.ComponentModel.DataAnnotations;

namespace JonyBalls3.Models.Calculator
{
    public class CalculationResult
    {
        [Display(Name = "Общая стоимость")]
        public decimal TotalCost { get; set; }

        [Display(Name = "Стены")]
        public decimal WallsCost { get; set; }

        [Display(Name = "Пол")]
        public decimal FloorCost { get; set; }

        [Display(Name = "Потолок")]
        public decimal CeilingCost { get; set; }

        [Display(Name = "Электрика")]
        public decimal ElectricCost { get; set; }

        [Display(Name = "Сантехника")]
        public decimal PlumbingCost { get; set; }

        [Display(Name = "Материалы")]
        public decimal MaterialsCost { get; set; }

        [Display(Name = "Работы")]
        public decimal WorkCost { get; set; }

        [Display(Name = "Доставка")]
        public decimal DeliveryCost { get; set; }

        [Display(Name = "Инструменты")]
        public decimal ToolsCost { get; set; }

        [Display(Name = "Дополнительные расходы")]
        public decimal AdditionalCosts { get; set; }

        [Display(Name = "Тип ремонта")]
        public string RepairType { get; set; } = "";

        [Display(Name = "Площадь")]
        public decimal Area { get; set; }

        [Display(Name = "Качество материалов")]
        public string MaterialQuality { get; set; } = "";

        [Display(Name = "Детализация")]
        public Dictionary<string, decimal> Breakdown { get; set; } = new();

        [Display(Name = "Рекомендации")]
        public string[] Recommendations { get; set; } = Array.Empty<string>();
    }
}
