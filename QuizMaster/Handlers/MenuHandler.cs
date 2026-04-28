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
                new[] { InlineKeyboardButton.WithCallbackData("Вибрати дисципліну", "menu_subjects") },
                new[]{ InlineKeyboardButton.WithCallbackData("Моя статистика", "menu_stats") }
            });

            await botClient.SendMessage(
                chatId,
                "Привіт! Я твій помічник для підготовки до іспитів.\n\nОбери дію:",
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
                new[] { InlineKeyboardButton.WithCallbackData("Математичний аналіз", "subject_math") },
                new[] { InlineKeyboardButton.WithCallbackData("Основи програмування", "subject_prog_basics") },
                new[] { InlineKeyboardButton.WithCallbackData("Комп'ютерні мережі", "subject_networks") },
                new[] { InlineKeyboardButton.WithCallbackData("Алгоритми та структури даних", "subject_algorithms") },
                new[] { InlineKeyboardButton.WithCallbackData("Назад", "menu_main") }
            });

            await botClient.SendMessage(
                chatId,
                "Обери предмет:",
                replyMarkup: keyboard,
                cancellationToken: ct);
        }
    }
}