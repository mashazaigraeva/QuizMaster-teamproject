# Функціонал для різних ролей

```mermaid
flowchart LR
    Student[Студент]
    Admin[Адміністратор]
    Gemini[Gemini API]
    
    subgraph QuizMaster_Bot[QuizMaster Bot]
        UC1(Реєстрація /start)
        UC2(Вибір дисципліни)
        UC3(Проходження тесту)
        UC4(ШІ-Репетитор /ask)
        UC5(Перегляд статистики)
        UC6(Генерація питань /gen)
    end
    
    Student --- UC1
    Student --- UC2
    Student --- UC3
    Student --- UC4
    Student --- UC5
    
    Admin --- UC1
    Admin --- UC2
    Admin --- UC3
    Admin --- UC4
    Admin --- UC5
    Admin --- UC6
    
    UC4 --- Gemini
    UC6 --- Gemini