using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Telegram.Bot;
using Telegram.Bot.Types;
using QuizMaster.Data;
using QuizMaster.Services;

namespace QuizMaster.Handlers
{
    public class CallbackHandler
    {
        public static async Task HandleAsync(ITelegramBotClient botClient, CallbackQuery callbackQuery, CancellationToken cancellationToken)
        {
            string callbackData = callbackQuery.Data;
            long chatId = callbackQuery.Message.Chat.Id;

            Console.WriteLine($"Натиснуто кнопку: {callbackData}");
            await botClient.AnswerCallbackQuery(callbackQuery.Id, cancellationToken: cancellationToken);

            if (callbackData.StartsWith("ans_"))
            {
                string selectedOption = callbackData.Split('_')[1]; 
                using var db = new AppDbContext();
                var testLogic = new TestLogicService(db);
                
                try
                {
                    var currentQuestion = await testLogic.GetCurrentQuestionAsync(chatId);
                    bool isCorrect = await testLogic.ProcessAnswerAsync(chatId, selectedOption);
                    
                    string feedback = "✅ Правильно!";
                    if (!isCorrect)
                    {
                        string safeExplanation = currentQuestion?.Explanation?.Replace("&", "&amp;").Replace("<", "&lt;").Replace(">", "&gt;") ?? "Пояснення відсутнє.";
                        feedback = $"❌ Неправильно!\n\n💡 Пояснення: {safeExplanation}";
                    }
                    await botClient.SendMessage(chatId, feedback, cancellationToken: cancellationToken);
                    
                    var nextQuestion = await testLogic.GetCurrentQuestionAsync(chatId);
                    if (nextQuestion != null)
                    {
                        await MenuHandler.SendCurrentQuestionAsync(botClient, chatId, testLogic, cancellationToken);
                    }
                    else
                    {
                        string result = await testLogic.FinishTestAsync(chatId);
                        await botClient.SendMessage(chatId, result, cancellationToken: cancellationToken);
                        await MenuHandler.SendMainMenuAsync(botClient, chatId, cancellationToken);
                    }
                }
                catch (Exception ex)
                {
                    await botClient.SendMessage(chatId, $"🏁 Сесію завершено або сталася помилка:\n{ex.Message}", cancellationToken: cancellationToken);
                }
                return;
            }

            switch (callbackData)
            {
                case "menu_main":
                    await MenuHandler.SendMainMenuAsync(botClient, chatId, cancellationToken);
                    break;
                case "menu_subjects":
                    await MenuHandler.SendSubjectsMenuAsync(botClient, chatId, cancellationToken);
                    break;
                case "menu_stats":
                    using (var db = new AppDbContext())
                    {
                        var statsService = new StatisticsService(db);
                        string report = await statsService.GetUserStatisticsAsync(chatId);
                        await botClient.SendMessage(chatId, report, parseMode: Telegram.Bot.Types.Enums.ParseMode.Markdown, cancellationToken: cancellationToken);
                    }
                    break;

                case "subject_math":
                case "subject_prog_basics":
                case "subject_networks":
                case "subject_algorithms":
                    using (var db = new AppDbContext())
                    {
                        var testLogic = new TestLogicService(db);
                        string subjectName = callbackData switch {
                            "subject_math" => "Математичний аналіз",
                            "subject_prog_basics" => "Основи програмування",
                            "subject_networks" => "Комп'ютерні мережі",
                            "subject_algorithms" => "Алгоритми та структури даних",
                            _ => ""
                        };

                        var subject = db.Subjects.ToList().FirstOrDefault(s => s.Name.ToLower() == subjectName.ToLower());
                        if (subject == null)
                        {
                            await botClient.SendMessage(chatId, $"⚠️ Предмет '{subjectName}' не знайдено. Згенеруйте питання!", cancellationToken: cancellationToken);
                            break;
                        }

                        try
                        {
                            await botClient.SendMessage(chatId, $"🚀 Запускаю тест з предмету: {subjectName}...", cancellationToken: cancellationToken);
                            await testLogic.StartNewTestAsync(chatId, subject.Id, 10, false);
                            await MenuHandler.SendCurrentQuestionAsync(botClient, chatId, testLogic, cancellationToken);
                        }
                        catch (Exception ex)
                        {
                            await botClient.SendMessage(chatId, $"❌ Не вдалося запустити тест: {ex.Message}", cancellationToken: cancellationToken);
                        }
                    }
                    break;

                case "menu_settings":
                    string helpText = "⚙️ <b>Довідка та Налаштування</b>\n\n" +
                                      "Я — твій розумний ШІ-помічник для підготовки до іспитів. 🎓\n\n" +
                                      "📌 Основні команди:\n" +
                                      "🔹 /start — Головне меню\n" +
                                      "🔹 /ask [питання] — Запитати ШІ-репетитора\n" +
                                      "🔹 /quit — Завершити поточний тест\n\n" +
                                      "🛠 Для адміна:\n" +
                                      "🔹 /gen [Предмет] [К-сть] — Згенерувати питання.";

                    var backKeyboard = new Telegram.Bot.Types.ReplyMarkups.InlineKeyboardMarkup(
                        Telegram.Bot.Types.ReplyMarkups.InlineKeyboardButton.WithCallbackData("🔙 Назад у меню", "menu_main")
                    );

                    await botClient.SendMessage(chatId, helpText, parseMode: Telegram.Bot.Types.Enums.ParseMode.Html, replyMarkup: backKeyboard, cancellationToken: cancellationToken);
                    break;
            }
        }
    }
}
