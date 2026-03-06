class PrivacyAssistant {
    constructor() {
        this.synth = window.speechSynthesis;
        this.avatar = document.getElementById('assistantAvatar');
        this.status = document.getElementById('assistantStatus');
        this.isSpeaking = false;
        this.waveBars = document.querySelectorAll('.sound-wave span');
        
        this.init();
    }
    
    init() {
        if (!this.synth) {
            console.warn('Синтез речи не поддерживается');
            return;
        }
        
        // Приветствие через 1 секунду
        setTimeout(() => {
            this.speak('Здравствуйте! Я ваш помощник по конфиденциальности. Я расскажу всё о том, как мы защищаем ваши данные. Нажмите на любой раздел или задайте вопрос.');
        }, 1000);
    }
    
    speak(text) {
        if (!this.synth) return;
        
        // Останавливаем текущую речь
        this.stop();
        
        // Создаем сообщение
        const utterance = new SpeechSynthesisUtterance(text);
        utterance.lang = 'ru-RU';
        utterance.rate = 0.95;
        utterance.pitch = 1.1;
        
        // Выбираем русский голос
        const voices = this.synth.getVoices();
        const russianVoice = voices.find(v => v.lang.includes('ru'));
        if (russianVoice) utterance.voice = russianVoice;
        
        // События
        utterance.onstart = () => {
            this.isSpeaking = true;
            this.updateUI(true);
        };
        
        utterance.onend = () => {
            this.isSpeaking = false;
            this.updateUI(false);
        };
        
        utterance.onerror = () => {
            this.isSpeaking = false;
            this.updateUI(false);
        };
        
        this.synth.speak(utterance);
    }
    
    stop() {
        if (this.synth) {
            this.synth.cancel();
            this.isSpeaking = false;
            this.updateUI(false);
        }
    }
    
    updateUI(isSpeaking) {
        // Анимация аватара
        if (this.avatar) {
            if (isSpeaking) {
                this.avatar.classList.add('speaking');
            } else {
                this.avatar.classList.remove('speaking');
            }
        }
        
        // Статус
        if (this.status) {
            this.status.textContent = isSpeaking ? '🎤 Говорю...' : '💬 Готов';
            this.status.className = `assistant-status ${isSpeaking ? 'speaking' : ''}`;
        }
        
        // Звуковая волна
        if (this.waveBars) {
            this.waveBars.forEach(bar => {
                bar.style.animation = isSpeaking ? 'wave 1s ease-in-out infinite' : 'none';
            });
        }
    }
    
    // Ответы на вопросы
    answerQuestion(question) {
        const answers = {
            'collect': 'Мы собираем: ваше имя, email, телефон, данные о проектах ремонта, историю заказов. Это необходимо для работы сервиса.',
            'use': 'Данные используются для: подбора подрядчиков, расчета сметы, связи с вами, улучшения качества услуг.',
            'share': 'Мы передаем данные только подрядчикам с вашего согласия и по требованию закона. Никакой продажи третьим лицам.',
            'protect': 'Данные защищены: SSL шифрование, безопасное хранение, регулярные проверки, двухфакторная аутентификация.',
            'delete': 'Удалить данные можно: в личном кабинете, написав на email, через поддержку. Мы удаляем всё в течение 30 дней.'
        };
        
        this.speak(answers[question] || 'Задайте другой вопрос');
    }
    
    // Озвучить раздел
    speakSection(title, content) {
        this.speak(`${title}. ${content}`);
    }
    
    // Полный обзор
    fullOverview() {
        const text = `Политика конфиденциальности JonyBalls3. 
            
            Раздел 1. Сбор данных: Мы собираем ваше имя, email, телефон, данные о проектах.
            
            Раздел 2. Использование: Данные нужны для подбора подрядчиков, расчета сметы и связи.
            
            Раздел 3. Передача: Только подрядчикам с вашего согласия или по закону.
            
            Раздел 4. Защита: SSL шифрование, безопасное хранение, проверки безопасности.
            
            Раздел 5. Удаление: Вы можете удалить данные в любой момент через личный кабинет.
            
            Раздел 6. Контакты: Email для вопросов: privacy@jonyballs3.ru`;
        
        this.speak(text);
    }
}

// Инициализация
document.addEventListener('DOMContentLoaded', () => {
    window.assistant = new PrivacyAssistant();
    
    // Добавляем анимацию при загрузке
    setTimeout(() => {
        const avatar = document.getElementById('assistantAvatar');
        if (avatar) {
            avatar.style.animation = 'float 3s ease-in-out infinite';
        }
    }, 500);
});
