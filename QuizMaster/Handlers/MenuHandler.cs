using System.Threading;
using System.Threading.Tasks;
using Telegram.Bot;
using Telegram.Bot.Types.ReplyMarkups;

namespace StudyBot.Handlers
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
                new[]
                {
                    InlineKeyboardButton.WithCallbackData("Почати тест", "menu_subjects")
                },
                new[]
                {
                    InlineKeyboardButton.WithCallbackData("Статистика", "menu_stats"),
                    InlineKeyboardButton.WithCallbackData("Налаштування", "menu_settings")
                }
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
                new[]
                {
                    InlineKeyboardButton.WithCallbackData("Математика", "subject_math"),
                    InlineKeyboardButton.WithCallbackData("C#", "subject_csharp")
                },
                new[]
                {
                    InlineKeyboardButton.WithCallbackData("Назад", "menu_main")
                }
            });

            await botClient.SendMessage(
                chatId,
                "Обери предмет:",
                replyMarkup: keyboard,
                cancellationToken: ct);
        }
    }
}