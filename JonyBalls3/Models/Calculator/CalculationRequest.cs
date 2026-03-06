using System.ComponentModel.DataAnnotations;

namespace JonyBalls3.Models.Calculator
{
    public class CalculationRequest
    {
        [Display(Name = "Площадь (м²)")]
        [Range(1, 1000, ErrorMessage = "Площадь должна быть от 1 до 1000 м²")]
        public decimal Area { get; set; } = 50;

        [Display(Name = "Количество комнат")]
        [Range(1, 20, ErrorMessage = "Количество комнат должно быть от 1 до 20")]
        public int RoomCount { get; set; } = 2;

        [Display(Name = "Тип ремонта")]
        public string RepairType { get; set; } = "Косметический";

        [Display(Name = "Стены")]
        public bool IncludeWalls { get; set; } = true;

        [Display(Name = "Пол")]
        public bool IncludeFloor { get; set; } = true;

        [Display(Name = "Потолок")]
        public bool IncludeCeiling { get; set; } = true;

        [Display(Name = "Электрика")]
        public bool IncludeElectric { get; set; } = false;

        [Display(Name = "Сантехника")]
        public bool IncludePlumbing { get; set; } = false;

        [Display(Name = "Качество материалов")]
        public string MaterialQuality { get; set; } = "Среднее";

        [Display(Name = "Доставка")]
        public bool IncludeDelivery { get; set; } = true;

        [Display(Name = "Инструменты")]
        public bool IncludeTools { get; set; } = false;
    }
}
