using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Telegram.Bot;
using Telegram.Bot.Polling;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using DotNetEnv;
using QuizMaster.Models;
using QuizMaster.Data;

namespace QuizMaster
{
    class Program
    {
        static async Task Main(string[] args)
        {
            Env.Load();
            string botToken = Environment.GetEnvironmentVariable("BOT_TOKEN");

            if (string.IsNullOrEmpty(botToken))
            {
                Console.WriteLine("Помилка: Токен бота не знайдено!");
                return;
            }

            using (var context = new AppDbContext())
            {
                Console.WriteLine("База даних підключена.");
            }

            var botClient = new TelegramBotClient(botToken);
            using var cts = new CancellationTokenSource();

            var receiverOptions = new ReceiverOptions
            {
                AllowedUpdates = Array.Empty<UpdateType>()
            };

            botClient.StartReceiving(
                updateHandler: HandleUpdateAsync,
                errorHandler: HandlePollingErrorAsync,
                receiverOptions: receiverOptions,
                cancellationToken: cts.Token
            );

            var me = await botClient.GetMe();
            Console.WriteLine($"Бот @{me.Username} успішно запущений! Натисніть Enter для зупинки.");
            Console.ReadLine();
            cts.Cancel();
        }

        static async Task HandleUpdateAsync(ITelegramBotClient botClient, Update update, CancellationToken cancellationToken)
        {
            if (update.Type == UpdateType.Message && update.Message?.Text != null)
            {
                var message = update.Message;
                Console.WriteLine($"Отримано повідомлення: '{message.Text}' від {message.Chat.Id}");

                if (message.Text == "/start")
                {
                    using (var db = new AppDbContext())
                    {
                        var user = db.Users.FirstOrDefault(u => u.TelegramId == message.Chat.Id);

                        if (user == null)
                        {
                            user = new Models.User
                            {
                                TelegramId = message.Chat.Id,
                                Username = message.Chat.Username
                            };
                            db.Users.Add(user);
                            db.SaveChanges();
                            
                            await botClient.SendMessage(
                                chatId: message.Chat.Id,
                                text: "Привіт! Я успішно зареєстрував тебе в базі. Готовий до тестів?",
                                cancellationToken: cancellationToken);
                        }
                        else
                        {
                            await botClient.SendMessage(
                                chatId: message.Chat.Id,
                                text: $"З поверненням, {user.Username ?? "студенте"}! Продовжимо навчання?",
                                cancellationToken: cancellationToken);
                        }
                    }
                    await Handlers.MenuHandler.SendMainMenuAsync(botClient, message.Chat.Id, cancellationToken);
                }


                string messageText = update.Message.Text;
                long chatId = update.Message.Chat.Id;

                if (messageText.StartsWith("/gen "))
                {
                    string adminIdString = Environment.GetEnvironmentVariable("ADMIN_ID");
                    long adminId = 0;

                    if (long.TryParse(adminIdString, out adminId))
                    {
                        if (message.Chat.Id == adminId)
                        {
                            await botClient.SendMessage(message.Chat.Id, "Доступ дозволено! Починаю генерацію...", cancellationToken: cancellationToken);
                        }
                        else
                        {
                            await botClient.SendMessage(message.Chat.Id, "У вас немає прав для генерації питань.", cancellationToken: cancellationToken);
                        }
                    }
                    else
                    {
                        Console.WriteLine("Помилка: ADMIN_ID у файлі .env не є числом або порожній!");
                    }

                    var parts = messageText.Split(' ', StringSplitOptions.RemoveEmptyEntries); 

                    if (parts.Length >= 3 && int.TryParse(parts.Last(), out int count))
                    {
                        string subject = string.Join(" ", parts.Skip(1).Take(parts.Length - 2));

                        await botClient.SendMessage(chatId, $"Gemini думає... Генерую {count} питань з '{subject}'. Це займе кілька секунд.", cancellationToken: cancellationToken);
                        string jsonResult = null;

                        try
                        {
                            var gemini = new Services.GeminiService();
                            
                            jsonResult = await gemini.GenerateQuestionsAsync(subject, count);

                            if (string.IsNullOrEmpty(jsonResult))
                            {
                                await botClient.SendMessage(chatId, "Помилка: Gemini не повернув дані або виникла помилка з'єднання.", cancellationToken: cancellationToken);
                                return;
                            }

                            if (jsonResult.StartsWith("```json"))
                            {
                                jsonResult = jsonResult.Substring(7, jsonResult.Length - 10).Trim();
                            }
                            else if (jsonResult.StartsWith("```"))
                            {
                                jsonResult = jsonResult.Substring(3, jsonResult.Length - 6).Trim();
                            }

                            using var db = new Data.AppDbContext();
                            
                            var subjectEntity = db.Subjects.ToList().FirstOrDefault(s => s.Name.ToLower() == subject.ToLower());
                            if (subjectEntity == null)
                            {
                                subjectEntity = new Models.Subject { Name = subject };
                                db.Subjects.Add(subjectEntity);
                                db.SaveChanges();
                            }

                            var options = new System.Text.Json.JsonSerializerOptions { PropertyNameCaseInsensitive = true };
                            var questions = System.Text.Json.JsonSerializer.Deserialize<System.Collections.Generic.List<Models.Question>>(jsonResult, options);

                            if (questions == null || questions.Count == 0)
                            {
                                await botClient.SendMessage(chatId, "ШІ повернув порожній масив.", cancellationToken: cancellationToken);
                                return;
                            }

                            foreach (var q in questions)
                            {
                                q.SubjectId = subjectEntity.Id;
                                db.Questions.Add(q);
                            }
                            
                            db.SaveChanges();

                            await botClient.SendMessage(chatId, $"Магія відбулася! {questions.Count} нових питань з '{subject}' успішно додано до бази даних.", cancellationToken: cancellationToken);
                        }
                        catch (Exception ex)
                        {
                            await botClient.SendMessage(chatId, $"Помилка парсингу або збереження: {ex.Message}\n\nОсь що повернув ШІ:\n{jsonResult ?? "null"}", cancellationToken: cancellationToken);
                        }
                    }
                    else
                    {
                        await botClient.SendMessage(chatId, "Неправильний формат. Використовуй: /gen Назва_предмета Кількість (наприклад: /gen C# 5)", cancellationToken: cancellationToken);
                    }
                    return;
                }
                return;
            }

            if (update.Type == UpdateType.CallbackQuery && update.CallbackQuery != null)
            {
                await HandleCallbackQueryAsync(botClient, update.CallbackQuery, cancellationToken);
                return;
            }   
        }

        static async Task HandleCallbackQueryAsync(ITelegramBotClient botClient, CallbackQuery callbackQuery, CancellationToken cancellationToken)
        {
            string callbackData = callbackQuery.Data;
            long chatId = callbackQuery.Message.Chat.Id;

            Console.WriteLine($"Натиснуто кнопку: {callbackData}");

            await botClient.AnswerCallbackQuery(callbackQuery.Id, cancellationToken: cancellationToken);

            if (callbackData.StartsWith("ans_"))
            {
                string selectedOption = callbackData.Split('_')[1]; 
                
                using var db = new AppDbContext();
                var testLogic = new Services.TestLogicService(db);
                
                try
                {
                    var currentQuestion = await testLogic.GetCurrentQuestionAsync(chatId);
                    bool isCorrect = await testLogic.ProcessAnswerAsync(chatId, selectedOption);
                    
                    string feedback;
                    if (isCorrect)
                    {
                        feedback = "Правильно!";
                    }
                    else
                    {
                        string safeExplanation = currentQuestion?.Explanation?.Replace("&", "&amp;").Replace("<", "&lt;").Replace(">", "&gt;") ?? "Пояснення відсутнє.";
                        feedback = $"Неправильно!\n\nПояснення: {safeExplanation}";
                    }
                    await botClient.SendMessage(chatId, feedback, cancellationToken: cancellationToken);
                    
                    var nextQuestion = await testLogic.GetCurrentQuestionAsync(chatId);
                    if (nextQuestion != null)
                    {
                        await SendCurrentQuestionAsync(botClient, chatId, testLogic, cancellationToken);
                    }
                    else
                    {
                        string result = await testLogic.FinishTestAsync(chatId);
                        await botClient.SendMessage(chatId, result, cancellationToken: cancellationToken);
                        await Handlers.MenuHandler.SendMainMenuAsync(botClient, chatId, cancellationToken);
                    }
                }
                catch (Exception ex)
                {
                    await botClient.SendMessage(chatId, $"Сесію завершено або сталася помилка:\n{ex.Message}", cancellationToken: cancellationToken);
                }
                return;
            }

            switch (callbackData)
            {
                case "menu_main":
                    await Handlers.MenuHandler.SendMainMenuAsync(botClient, chatId, cancellationToken);
                    break;

                case "menu_subjects":
                    await Handlers.MenuHandler.SendSubjectsMenuAsync(botClient, chatId, cancellationToken);
                    break;

                case "menu_stats":
                    using (var db = new AppDbContext())
                    {
                        var statsService = new Services.StatisticsService(db);
                        string report = await statsService.GetUserStatisticsAsync(chatId);
                        
                        await botClient.SendMessage(
                            chatId: chatId, 
                            text: report, 
                            parseMode: Telegram.Bot.Types.Enums.ParseMode.Markdown,
                            cancellationToken: cancellationToken);
                    }
                    break;
                
                case "subject_math":
                case "subject_prog_basics":
                case "subject_networks":
                case "subject_algorithms":
                    using (var db = new AppDbContext())
                    {
                        var testLogic = new Services.TestLogicService(db);
                        
                        string subjectName = ""; 
                        if (callbackData == "subject_math")
                        {
                            subjectName = "Математичний аналіз";
                        }
                        if (callbackData == "subject_prog_basics")
                        {
                            subjectName = "Основи програмування";
                        }
                        if (callbackData == "subject_networks")
                        {
                            subjectName = "Комп'ютерні мережі";
                        }
                        if (callbackData == "subject_algorithms")
                        {
                            subjectName = "Алгоритми та структури даних";
                        }

                        var subject = db.Subjects.ToList().FirstOrDefault(s => s.Name.ToLower() == subjectName.ToLower());
                        
                        if (subject == null)
                        {
                            await botClient.SendMessage(chatId, $"Предмет '{subjectName}' не знайдено. Спочатку згенеруй питання командою /gen!", cancellationToken: cancellationToken);
                            break;
                        }

                        try
                        {
                            await botClient.SendMessage(chatId, $"Запускаю тест з предмету: {subjectName}...", cancellationToken: cancellationToken);
                            
                            await testLogic.StartNewTestAsync(chatId, subject.Id, 10, false);
                            
                            await SendCurrentQuestionAsync(botClient, chatId, testLogic, cancellationToken);
                        }
                        catch (Exception ex)
                        {
                            await botClient.SendMessage(chatId, $"Не вдалося запустити тест: {ex.Message}", cancellationToken: cancellationToken);
                        }
                    }
                    break;

                default:
                    await botClient.SendMessage(chatId, "Невідома команда.", cancellationToken: cancellationToken);
                    break;
            }
        }

        static async Task SendCurrentQuestionAsync(ITelegramBotClient botClient, long chatId, Services.TestLogicService testLogic, CancellationToken cancellationToken)
        {
            var question = await testLogic.GetCurrentQuestionAsync(chatId);
            if (question == null)
            {
                return;
            }

            string safeText = question.Text?.Replace("&", "&amp;").Replace("<", "&lt;").Replace(">", "&gt;") ?? "";
            string safeA = question.OptionA?.Replace("&", "&amp;").Replace("<", "&lt;").Replace(">", "&gt;") ?? "";
            string safeB = question.OptionB?.Replace("&", "&amp;").Replace("<", "&lt;").Replace(">", "&gt;") ?? "";
            string safeC = question.OptionC?.Replace("&", "&amp;").Replace("<", "&lt;").Replace(">", "&gt;") ?? "";
            string safeD = question.OptionD?.Replace("&", "&amp;").Replace("<", "&lt;").Replace(">", "&gt;") ?? "";

            string text = $"<b>{safeText}</b>\n\n" +
                        $"A) {safeA}\n" +
                        $"B) {safeB}\n" +
                        $"C) {safeC}\n" +
                        $"D) {safeD}";

            var inlineKeyboard = new Telegram.Bot.Types.ReplyMarkups.InlineKeyboardMarkup(new[]
            {
                new[]
                {
                    Telegram.Bot.Types.ReplyMarkups.InlineKeyboardButton.WithCallbackData("A", "ans_A"),
                    Telegram.Bot.Types.ReplyMarkups.InlineKeyboardButton.WithCallbackData("B", "ans_B"),
                    Telegram.Bot.Types.ReplyMarkups.InlineKeyboardButton.WithCallbackData("C", "ans_C"),
                    Telegram.Bot.Types.ReplyMarkups.InlineKeyboardButton.WithCallbackData("D", "ans_D")
                }
            });

            await botClient.SendMessage(chatId, text, Telegram.Bot.Types.Enums.ParseMode.Html, replyMarkup: inlineKeyboard, cancellationToken: cancellationToken);
        }

        static Task HandlePollingErrorAsync(ITelegramBotClient botClient, Exception exception, CancellationToken cancellationToken)
        {
            Console.WriteLine($"Помилка: {exception.Message}");
            return Task.CompletedTask;
        }
    }
}