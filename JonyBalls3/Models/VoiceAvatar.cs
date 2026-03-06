namespace JonyBalls3.Models
{
    public class VoiceAvatar
    {
        public int Id { get; set; }
        public string Name { get; set; } = "Ассистент JonyBalls";
        public string AvatarUrl { get; set; } = "https://api.dicebear.com/7.x/bottts/svg?seed=jonyballs";
        public string Voice { get; set; } = "ru-RU-DariyaNeural";
        public string Greeting { get; set; } = "Привет! Я расскажу вам о политике конфиденциальности";
    }
}
