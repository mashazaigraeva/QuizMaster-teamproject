using System;
using System.Threading;
using System.Threading.Tasks;
using Telegram.Bot;
using Telegram.Bot.Polling;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using DotNetEnv;
using QuizMaster.Data;
using QuizMaster.Handlers;

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
                AllowedUpdates = Array.Empty<UpdateType>(),
                DropPendingUpdates = true
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
                await MessageHandler.HandleAsync(botClient, update.Message, cancellationToken);
                return;
            }

            if (update.Type == UpdateType.CallbackQuery && update.CallbackQuery != null)
            {
                await CallbackHandler.HandleAsync(botClient, update.CallbackQuery, cancellationToken);
                return;
            }   
        }

        static Task HandlePollingErrorAsync(ITelegramBotClient botClient, Exception exception, CancellationToken cancellationToken)
        {
            Console.WriteLine($"Помилка: {exception.Message}");
            return Task.CompletedTask;
        }
    }
}