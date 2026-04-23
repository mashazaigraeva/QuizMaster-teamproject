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

            switch (callbackData)
            {
                case "menu_main":
                    await Handlers.MenuHandler.SendMainMenuAsync(botClient, chatId, cancellationToken);
                    break;

                case "menu_subjects":
                    await Handlers.MenuHandler.SendSubjectsMenuAsync(botClient, chatId, cancellationToken);
                    break;

                case "menu_statistics":
                    await botClient.SendMessage(chatId, "Тут буде виводитись твоя статистика успішності. (В розробці)", cancellationToken: cancellationToken);
                    break;
                
                case "subject_math":
                case "subject_csharp":
                case "subject_networks":
                    await botClient.SendMessage(chatId, $"Ти обрав предмет: {callbackData}. Запускаю алгоритм тестування...", cancellationToken: cancellationToken);
                    break;

                default:
                    await botClient.SendMessage(chatId, "Невідома команда.", cancellationToken: cancellationToken);
                    break;
            }
        }

        static Task HandlePollingErrorAsync(ITelegramBotClient botClient, Exception exception, CancellationToken cancellationToken)
        {
            Console.WriteLine($"Помилка: {exception.Message}");
            return Task.CompletedTask;
        }
    }
}