using System.Threading; 
using System.Threading.Tasks;
using Telegram.Bot;
using Telegram.Bot.Types;

namespace QuizMaster.Handlers
{
    public class MessageHandler
    {
        public static async Task HandleAsync(ITelegramBotClient botClient, Message message, CancellationToken cancellationToken)
        {
            if (message.Text == null) return;

            if (message.Text == "/start")
            {
                await MenuHandler.SendMainMenuAsync(botClient, message.Chat.Id, cancellationToken);
            }
        }
    }
}