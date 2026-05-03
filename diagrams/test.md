# Процес проходження тесту

```mermaid
sequenceDiagram
    actor Student as Студент
    participant Bot as QuizMaster Bot
    participant TLS as TestLogicService
    participant DB as База Даних

    Student->>Bot: Обирає предмет (Callback)
    Bot->>TLS: StartNewTestAsync(chatId, subjectId)
    TLS->>DB: Отримання списку питань
    TLS-->>Bot: Питання знайдено
    Bot-->>Student: Відправка першого питання (кнопки A, B, C, D)

    Note over Student,Bot: Процес відповіді
    Student->>Bot: Натискає кнопку відповіді
    Bot->>TLS: ProcessAnswerAsync(chatId, option)
    TLS->>DB: Порівняння з CorrectOption
    TLS-->>Bot: Результат (True/False)
    
    alt Відповідь невірна
        Bot->>DB: Отримання поля Explanation
        DB-->>Bot: Текст пояснення
        Bot-->>Student: Повідомлення про помилку + Пояснення
    else Відповідь вірна
        Bot-->>Student: Повідомлення про успіх
    end

    Bot->>TLS: GetNextQuestion()
    TLS-->>Bot: Наступне питання / Завершення
    Bot-->>Student: Наступне питання АБО Фінальний бал