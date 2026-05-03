# Логіка ШІ-репетитора та Адмін-панелі

```mermaid
sequenceDiagram
    actor User as Студент / Адмін
    participant Bot as MessageHandler
    participant Dict as Словник waitingForAsk
    participant GS as GeminiService
    participant DB as База Даних

    Note over User,Bot: Сценарій 1: Запитання до ШІ (/ask)
    User->>Bot: Команда /ask (без тексту)
    Bot->>Dict: Встановлення true для chatId
    Bot-->>User: Запит: Напишіть ваше питання
    User->>Bot: Текст питання (наприклад: "Що таке клас?")
    Bot->>Dict: Перевірка стану (waiting == true)
    Bot->>GS: Запит до Gemini API
    GS-->>Bot: Відповідь від ШІ
    Bot-->>User: Вивід відповіді репетитора

    Note over User,Bot: Сценарій 2: Адміністрування (/gen)
    User->>Bot: Команда /gen [Предмет] [Кількість]
    Bot->>Bot: Читання ADMIN_ID з .env
    Bot->>Bot: Перевірка відповідності chatId
    alt Перевірка пройдена
        Bot->>GS: Запит на генерацію питань
        GS-->>Bot: JSON з питаннями
        Bot->>DB: Збереження в таблиці Subjects та Questions
        Bot-->>User: Повідомлення про успішне додавання
    else Перевірка провалена
        Bot-->>User: Повідомлення: Доступ заборонено
    end