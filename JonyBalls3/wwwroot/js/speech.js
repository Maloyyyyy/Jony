// Web Speech API - работает в браузере бесплатно
class VoiceAssistant {
    constructor() {
        this.synth = window.speechSynthesis;
        this.utterance = null;
        this.isPlaying = false;
        this.avatarElement = document.getElementById('assistant-avatar');
        this.statusElement = document.getElementById('voice-status');
    }

    // Инициализация
    init() {
        if (!this.synth) {
            alert('Ваш браузер не поддерживает синтез речи');
            return false;
        }
        return true;
    }

    // Воспроизведение текста
    speak(text, voice = 'ru-RU') {
        if (!this.init()) return;

        // Останавливаем текущее воспроизведение
        this.stop();

        // Создаем новое сообщение
        this.utterance = new SpeechSynthesisUtterance(text);
        this.utterance.lang = 'ru-RU';
        this.utterance.rate = 0.9; // Скорость речи
        this.utterance.pitch = 1.0; // Тон голоса
        this.utterance.volume = 1.0; // Громкость

        // Выбираем русский голос
        const voices = this.synth.getVoices();
        const russianVoice = voices.find(v => v.lang.includes('ru'));
        if (russianVoice) {
            this.utterance.voice = russianVoice;
        }

        // События
        this.utterance.onstart = () => {
            this.isPlaying = true;
            this.updateStatus('Говорю...');
            this.animateAvatar(true);
        };

        this.utterance.onend = () => {
            this.isPlaying = false;
            this.updateStatus('Готов');
            this.animateAvatar(false);
        };

        this.utterance.onerror = (event) => {
            console.error('Ошибка синтеза речи:', event);
            this.isPlaying = false;
            this.updateStatus('Ошибка');
            this.animateAvatar(false);
        };

        // Воспроизводим
        this.synth.speak(this.utterance);
    }

    // Остановка воспроизведения
    stop() {
        if (this.synth && this.isPlaying) {
            this.synth.cancel();
            this.isPlaying = false;
            this.updateStatus('Остановлено');
            this.animateAvatar(false);
        }
    }

    // Пауза
    pause() {
        if (this.synth && this.isPlaying) {
            this.synth.pause();
            this.updateStatus('Пауза');
        }
    }

    // Возобновление
    resume() {
        if (this.synth && this.isPlaying) {
            this.synth.resume();
            this.updateStatus('Говорю...');
        }
    }

    // Обновление статуса
    updateStatus(status) {
        if (this.statusElement) {
            this.statusElement.textContent = status;
            this.statusElement.className = `status-${status.toLowerCase()}`;
        }
    }

    // Анимация аватара
    animateAvatar(isSpeaking) {
        if (this.avatarElement) {
            if (isSpeaking) {
                this.avatarElement.classList.add('speaking');
                // Добавляем пульсацию
                this.avatarElement.style.animation = 'pulse 1s infinite';
            } else {
                this.avatarElement.classList.remove('speaking');
                this.avatarElement.style.animation = 'none';
            }
        }
    }

    // Получение текста политики
    getPrivacyPolicyText() {
        return `Здравствуйте! Я расскажу вам о политике конфиденциальности платформы JonyBalls3.
                
                Мы собираем следующую информацию: ваше имя, email, телефон, данные о проектах ремонта.
                
                Ваши данные используются для: предоставления услуг ремонта, связи с подрядчиками, улучшения качества сервиса.
                
                Мы не передаем ваши данные третьим лицам без вашего согласия.
                
                Вы можете в любой момент запросить удаление ваших данных.
                
                Для защиты информации мы используем современные методы шифрования.
                
                Спасибо за использование нашего сервиса!`;
    }

    // Разбивка текста на части для лучшего восприятия
    speakPrivacyPolicy() {
        const fullText = this.getPrivacyPolicyText();
        const parts = fullText.split('\n\n'); // Разбиваем по абзацам
        
        // Функция для последовательного воспроизведения
        const playNext = (index) => {
            if (index < parts.length) {
                this.speak(parts[index]);
                
                // После окончания текущей части играем следующую
                this.utterance.onend = () => {
                    setTimeout(() => playNext(index + 1), 500);
                };
            }
        };
        
        playNext(0);
    }
}

// Инициализация при загрузке страницы
document.addEventListener('DOMContentLoaded', function() {
    window.assistant = new VoiceAssistant();
    
    // Создаем кнопки управления
    createControlButtons();
});

function createControlButtons() {
    const container = document.querySelector('.voice-controls');
    if (!container) return;

    const buttons = `
        <button onclick="window.assistant.speakPrivacyPolicy()" class="btn btn-primary">
            <i class="fas fa-play"></i> Озвучить политику
        </button>
        <button onclick="window.assistant.stop()" class="btn btn-warning">
            <i class="fas fa-stop"></i> Стоп
        </button>
        <button onclick="window.assistant.pause()" class="btn btn-info">
            <i class="fas fa-pause"></i> Пауза
        </button>
        <button onclick="window.assistant.resume()" class="btn btn-success">
            <i class="fas fa-play-circle"></i> Продолжить
        </button>
    `;
    
    container.innerHTML = buttons;
}
