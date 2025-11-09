using System.Threading;
using System.Threading.Tasks;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;

namespace TgLlmBot.Services.Telegram.CommandDispatcher;

public interface ITelegramCommandDispatcher
{
    Task HandleMessageAsync(
        Message message,
        UpdateType type,
        CancellationToken cancellationToken);
}
