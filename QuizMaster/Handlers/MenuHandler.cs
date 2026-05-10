using System.Threading;
using System.Threading.Tasks;
using Telegram.Bot;
using Telegram.Bot.Types.ReplyMarkups;
using Telegram.Bot.Types.Enums;

namespace QuizMaster.Handlers
{
    public class MenuHandler
    {
        public static async Task SendMainMenuAsync(
            ITelegramBotClient botClient,
            long chatId,
            CancellationToken ct)
        {
            var keyboard = new InlineKeyboardMarkup(new[]
            {
                new[] { InlineKeyboardButton.WithCallbackData("📚 Вибрати дисципліну", "menu_subjects") },
                new[]{ InlineKeyboardButton.WithCallbackData("📊 Моя статистика", "menu_stats") }
            });

            await botClient.SendMessage(
                chatId,
                "👋 Привіт! Я твій помічник для підготовки до іспитів.\n\nОбери дію:",
                replyMarkup: keyboard,
                cancellationToken: ct);
        }

        public static async Task SendSubjectsMenuAsync(
            ITelegramBotClient botClient,
            long chatId,
            CancellationToken ct)
        {
            var keyboard = new InlineKeyboardMarkup(new[]
            {
                new[] { InlineKeyboardButton.WithCallbackData("🧮 Математичний аналіз", "subject_math") },
                new[] { InlineKeyboardButton.WithCallbackData("💻 Основи програмування", "subject_prog_basics") },
                new[] { InlineKeyboardButton.WithCallbackData("🌐 Комп'ютерні мережі", "subject_networks") },
                new[] { InlineKeyboardButton.WithCallbackData("🧩 Алгоритми та структури даних", "subject_algorithms") },
                new[] { InlineKeyboardButton.WithCallbackData("🔙 Назад", "menu_main") }
            });

            await botClient.SendMessage(
                chatId,
                "🎯 Обери предмет:",
                replyMarkup: keyboard,
                cancellationToken: ct);
        }

        public static async Task SendCurrentQuestionAsync(
            ITelegramBotClient botClient, 
            long chatId, 
            Services.TestLogicService testLogic, 
            CancellationToken cancellationToken)
        {
            var question = await testLogic.GetCurrentQuestionAsync(chatId);
            if (question == null) return;

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

            var inlineKeyboard = new InlineKeyboardMarkup(new[]
            {
                new[]
                {
                    InlineKeyboardButton.WithCallbackData("A", "ans_A"),
                    InlineKeyboardButton.WithCallbackData("B", "ans_B"),
                    InlineKeyboardButton.WithCallbackData("C", "ans_C"),
                    InlineKeyboardButton.WithCallbackData("D", "ans_D")
                }
            });

            await botClient.SendMessage(chatId, text, ParseMode.Html, replyMarkup: inlineKeyboard, cancellationToken: cancellationToken);
        }
    }
}