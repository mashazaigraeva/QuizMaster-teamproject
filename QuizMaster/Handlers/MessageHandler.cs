using System;
using System.Linq;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Telegram.Bot;
using Telegram.Bot.Types;
using QuizMaster.Data;
using QuizMaster.Services;

namespace QuizMaster.Handlers
{
    public class MessageHandler
    {
        public static Dictionary<long, bool> waitingForAsk = new Dictionary<long, bool>();

        public static async Task HandleAsync(ITelegramBotClient botClient, Message message, CancellationToken cancellationToken)
        {
            Console.WriteLine($"Отримано повідомлення: '{message.Text}' від {message.Chat.Id}");
            string messageText = message.Text;
            long chatId = message.Chat.Id;

            if (messageText == "/start")
            {
                using (var db = new AppDbContext())
                {
                    var user = db.Users.FirstOrDefault(u => u.TelegramId == chatId);
                    if (user == null)
                    {
                        user = new Models.User { TelegramId = chatId, Username = message.Chat.Username };
                        db.Users.Add(user);
                        db.SaveChanges();
                        
                        await botClient.SendMessage(chatId, "👋 Привіт! Я успішно зареєстрував тебе в базі. Готовий до тестів?", cancellationToken: cancellationToken);
                    }
                    else
                    {
                        await botClient.SendMessage(chatId, $"👋 З поверненням, {user.Username ?? "студенте"}! Продовжимо навчання?", cancellationToken: cancellationToken);
                    }
                }
                await MenuHandler.SendMainMenuAsync(botClient, chatId, cancellationToken);
                return;
            }

            if (waitingForAsk.GetValueOrDefault(chatId) == true)
            {
                if (!messageText.StartsWith("/")) messageText = "/ask " + messageText; 
                waitingForAsk[chatId] = false;
            }

            if (messageText.StartsWith("/ask", StringComparison.OrdinalIgnoreCase))
            {
                string userQuestion = messageText.Length > 4 ? messageText.Substring(4).Trim() : "";

                if (string.IsNullOrEmpty(userQuestion))
                {
                    waitingForAsk[chatId] = true; 
                    await botClient.SendMessage(chatId, "✍️ Що саме тебе цікавить?\nНапиши своє запитання прямо сюди (наступним повідомленням), і я передам його ШІ!", parseMode: Telegram.Bot.Types.Enums.ParseMode.Html, cancellationToken: cancellationToken);
                    return;
                }

                await botClient.SendMessage(chatId, "🧠 Gemini аналізує питання...", parseMode: Telegram.Bot.Types.Enums.ParseMode.Html, cancellationToken: cancellationToken);

                try
                {
                    var gemini = new GeminiService(); 
                    string answer = await gemini.AskTutorAsync(userQuestion);
                    await botClient.SendMessage(chatId, $"🎓 Відповідь ШІ:\n\n{answer}", parseMode: Telegram.Bot.Types.Enums.ParseMode.Html, cancellationToken: cancellationToken);
                }
                catch (Exception ex)
                {
                    await botClient.SendMessage(chatId, $"⚠️ ШІ тимчасово недоступний. Спробуй пізніше.\n({ex.Message})", cancellationToken: cancellationToken);
                }
                return;
            }

            if (messageText.Equals("/quit", StringComparison.OrdinalIgnoreCase))
            {
                using (var db = new AppDbContext())
                {
                    var testLogic = new TestLogicService(db);
                    testLogic.StopSession(chatId);
                }
                await botClient.SendMessage(chatId, "👋 На сьогодні з підготовкою все. Пиши /start коли будеш готовий повернутися!", cancellationToken: cancellationToken);
                return;
            }

            if (messageText.StartsWith("/gen "))
            {
                string adminIdString = Environment.GetEnvironmentVariable("ADMIN_ID");
                if (string.IsNullOrEmpty(adminIdString)) return;

                var adminIds = adminIdString.Split(',').Select(id => id.Trim()).ToList();
                if (!adminIds.Contains(chatId.ToString()))
                {
                    await botClient.SendMessage(chatId, "⛔ У вас немає прав для генерації питань.", cancellationToken: cancellationToken);
                    return;
                }

                var parts = messageText.Split(' ', StringSplitOptions.RemoveEmptyEntries); 
                if (parts.Length >= 3 && int.TryParse(parts.Last(), out int count))
                {
                    string subject = string.Join(" ", parts.Skip(1).Take(parts.Length - 2));
                    await botClient.SendMessage(chatId, $"⏳ Gemini думає... Генерую {count} питань з '{subject}'. Це займе кілька секунд.", cancellationToken: cancellationToken);
                    
                    try
                    {
                        var gemini = new GeminiService();
                        string jsonResult = await gemini.GenerateQuestionsAsync(subject, count);
                        if (string.IsNullOrEmpty(jsonResult)) return;

                        if (jsonResult.StartsWith("```json")) jsonResult = jsonResult.Substring(7, jsonResult.Length - 10).Trim();
                        else if (jsonResult.StartsWith("```")) jsonResult = jsonResult.Substring(3, jsonResult.Length - 6).Trim();

                        using var db = new AppDbContext();
                        var subjectEntity = db.Subjects.ToList().FirstOrDefault(s => s.Name.ToLower() == subject.ToLower());
                        if (subjectEntity == null)
                        {
                            subjectEntity = new Models.Subject { Name = subject };
                            db.Subjects.Add(subjectEntity);
                            db.SaveChanges();
                        }

                        var options = new System.Text.Json.JsonSerializerOptions { PropertyNameCaseInsensitive = true };
                        var questions = System.Text.Json.JsonSerializer.Deserialize<List<Models.Question>>(jsonResult, options);

                        if (questions != null)
                        {
                            foreach (var q in questions)
                            {
                                q.SubjectId = subjectEntity.Id;
                                db.Questions.Add(q);
                            }
                            db.SaveChanges();
                            await botClient.SendMessage(chatId, $"✨ Магія відбулася! {questions.Count} нових питань з '{subject}' успішно додано до бази даних. 🎉", cancellationToken: cancellationToken);
                        }
                    }
                    catch (Exception ex)
                    {
                        await botClient.SendMessage(chatId, $"❌ Помилка: {ex.Message}", cancellationToken: cancellationToken);
                    }
                }
                else
                {
                    await botClient.SendMessage(chatId, "⚠️ Неправильний формат. Використовуй: /gen Назва_предмета Кількість (наприклад: /gen C# 5)", cancellationToken: cancellationToken);
                return;
                }
            }

            if (messageText.StartsWith("/"))
            {
                await botClient.SendMessage(chatId, "⚠️ На жаль, я не розумію такої команди.\n\nНапиши /start, щоб відкрити головне меню, або скористайся кнопкою «Налаштування»!", cancellationToken: cancellationToken);
            }
        }
    }
}