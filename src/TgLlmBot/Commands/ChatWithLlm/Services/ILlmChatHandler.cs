using System.Threading;
using System.Threading.Tasks;

namespace TgLlmBot.Commands.ChatWithLlm.Services;

public interface ILlmChatHandler
{
    Task HandleCommandAsync(ChatWithLlmCommand command, CancellationToken cancellationToken);
}
