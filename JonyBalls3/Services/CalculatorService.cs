using JonyBalls3.Models.Calculator;

namespace JonyBalls3.Services
{
    public class CalculatorService
    {
        public CalculationResult Calculate(CalculationRequest request)
        {
            // Базовые цены за м² для разных типов работ
            decimal wallsBasePrice = request.RepairType switch
            {
                "Косметический" => 800,
                "Капитальный" => 1500,
                "Дизайнерский" => 2500,
                _ => 800
            };

            decimal floorBasePrice = request.RepairType switch
            {
                "Косметический" => 1000,
                "Капитальный" => 2000,
                "Дизайнерский" => 3500,
                _ => 1000
            };

            decimal ceilingBasePrice = request.RepairType switch
            {
                "Косметический" => 600,
                "Капитальный" => 1200,
                "Дизайнерский" => 2000,
                _ => 600
            };

            decimal electricBasePrice = request.RepairType switch
            {
                "Косметический" => 500,
                "Капитальный" => 1000,
                "Дизайнерский" => 1500,
                _ => 500
            };

            decimal plumbingBasePrice = request.RepairType switch
            {
                "Косметический" => 800,
                "Капитальный" => 1500,
                "Дизайнерский" => 2200,
                _ => 800
            };

            // Коэффициент качества материалов
            decimal materialMultiplier = request.MaterialQuality switch
            {
                "Эконом" => 0.7m,
                "Среднее" => 1.0m,
                "Премиум" => 1.8m,
                _ => 1.0m
            };

            // Коэффициент сложности (зависит от количества комнат)
            decimal complexityMultiplier = 1 + (request.RoomCount - 1) * 0.15m;

            // Расчет стоимости работ
            decimal wallsCost = request.IncludeWalls ? wallsBasePrice * request.Area * complexityMultiplier : 0;
            decimal floorCost = request.IncludeFloor ? floorBasePrice * request.Area * complexityMultiplier : 0;
            decimal ceilingCost = request.IncludeCeiling ? ceilingBasePrice * request.Area * complexityMultiplier : 0;
            decimal electricCost = request.IncludeElectric ? electricBasePrice * request.Area * complexityMultiplier : 0;
            decimal plumbingCost = request.IncludePlumbing ? plumbingBasePrice * request.Area * complexityMultiplier : 0;

            // Стоимость работ (без материалов)
            decimal workCost = wallsCost + floorCost + ceilingCost + electricCost + plumbingCost;

            // Стоимость материалов (обычно 60-80% от стоимости работ)
            decimal materialsCost = workCost * materialMultiplier * 0.7m;

            // Доставка
            decimal deliveryCost = request.IncludeDelivery ? workCost * 0.05m : 0;

            // Инструменты
            decimal toolsCost = request.IncludeTools ? workCost * 0.03m : 0;

            // Дополнительные расходы (10% на непредвиденное)
            decimal additionalCosts = (workCost + materialsCost + deliveryCost + toolsCost) * 0.1m;

            // Итог
            decimal totalCost = workCost + materialsCost + deliveryCost + toolsCost + additionalCosts;

            // Детализация
            var breakdown = new Dictionary<string, decimal>
            {
                ["Стены"] = wallsCost,
                ["Пол"] = floorCost,
                ["Потолок"] = ceilingCost,
                ["Электрика"] = electricCost,
                ["Сантехника"] = plumbingCost,
                ["Материалы"] = materialsCost,
                ["Доставка"] = deliveryCost,
                ["Инструменты"] = toolsCost,
                ["Доп. расходы"] = additionalCosts
            }.Where(kv => kv.Value > 0).ToDictionary(kv => kv.Key, kv => kv.Value);

            // Рекомендации
            var recommendations = new List<string>();
            
            if (request.RepairType == "Косметический" && request.MaterialQuality == "Премиум")
                recommendations.Add("Для косметического ремонта материалы премиум-класса могут быть избыточны. Рассмотрите вариант среднего качества.");
            
            if (request.IncludeElectric && !request.IncludeWalls)
                recommendations.Add("Электропроводка обычно требует штробления стен. Убедитесь, что это учтено в смете.");
            
            if (request.RoomCount > 3)
                recommendations.Add("Для большого количества комнат рекомендуется поэтапная оплата и контроль качества.");
            
            recommendations.Add("Добавьте 10-15% на непредвиденные расходы");
            recommendations.Add("Получите минимум 3 предложения от подрядчиков");
            recommendations.Add("Проверяйте наличие лицензий и страховки у подрядчиков");

            return new CalculationResult
            {
                TotalCost = totalCost,
                WallsCost = wallsCost,
                FloorCost = floorCost,
                CeilingCost = ceilingCost,
                ElectricCost = electricCost,
                PlumbingCost = plumbingCost,
                WorkCost = workCost,
                MaterialsCost = materialsCost,
                DeliveryCost = deliveryCost,
                ToolsCost = toolsCost,
                AdditionalCosts = additionalCosts,
                RepairType = request.RepairType,
                Area = request.Area,
                MaterialQuality = request.MaterialQuality,
                Breakdown = breakdown,
                Recommendations = recommendations.ToArray()
            };
        }
    }
}