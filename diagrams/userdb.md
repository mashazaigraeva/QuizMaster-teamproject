# Отримання та реєстрація студента

```mermaid
sequenceDiagram
    actor Student as Студент
    participant Bot as MessageHandler
    participant DB as AppDbContext
    participant Model as User (Entity)

    Student->>Bot: Команда /start
    Bot->>DB: Пошук за TelegramId (FirstOrDefault)
    
    alt Студент вже є в базі
        DB-->>Bot: Повертає об'єкт User
        Bot-->>Student: Привітання ("З поверненням, [Username]")
    else Студента немає (Перший вхід)
        Bot->>Model: Створення нового екземпляра User
        Model-->>Bot: Об'єкт з TelegramId та Username
        Bot->>DB: Додавання (db.Users.Add)
        Bot->>DB: Збереження (db.SaveChanges)
        Bot-->>Student: Повідомлення про реєстрацію
    end

    Bot->>Bot: Виклик MenuHandler.SendMainMenuAsync