# QuizMaster Bot 

```mermaid
sequenceDiagram
    title Студент проходить тестування (QuizMaster)

    actor S as Студент
    participant B as Telegram Bot (Handlers)
    participant L as TestLogicService
    participant DB as AppDbContext (SQLite)

    %% Авторизація та старт
    S->>B: /start
    B->>DB: Перевірка чи існує User
    DB-->>B: User знайдено
    B-->>S: Відображення Головного Меню (Inline Buttons)

    %% Вибір предмету
    S->>B: Натискає "Почати тест (ООП)"
    B->>L: Запит на генерацію квитка (SubjectId)
    L->>DB: Отримання списку питань з БД
    DB-->>L: List<Question>
    L->>L: Перемішування та вибір 10 питань
    L-->>B: Квиток згенеровано
    B-->>S: Відправка першого питання

    %% Проходження тесту
    loop Поки є питання у квитку
        S->>B: Натискає кнопку з відповіддю (A, B, C, D)
        B->>L: Збереження відповіді у UserSession
        L-->>B: Наступне питання
        B-->>S: Відправка наступного питання
    end

    %% Завершення
    S->>B: Відповідь на останнє питання
    B->>L: Розрахунок результату (CalculateScore)
    L->>DB: Збереження ExamResult у базу
    DB-->>L: Збережено успішно
    L-->>B: Відсоток правильних відповідей
    B-->>S: Відправка фінального результату (напр. 85%)