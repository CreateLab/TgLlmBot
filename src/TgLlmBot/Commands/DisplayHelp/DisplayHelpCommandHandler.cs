using System;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Telegram.Bot;
using TgLlmBot.Services.Telegram.CommandDispatcher.Abstractions;

namespace TgLlmBot.Commands.DisplayHelp;

public class DisplayHelpCommandHandler : AbstractCommandHandler<DisplayHelpCommand>
{
    private readonly TelegramBotClient _bot;
    private readonly string _helpTemplate;

    public DisplayHelpCommandHandler(DisplayHelpCommandHandlerOptions options, TelegramBotClient bot)
    {
        ArgumentNullException.ThrowIfNull(options);
        ArgumentNullException.ThrowIfNull(bot);
        _bot = bot;
        _helpTemplate = BuildHelpTemplate(options.BotName);
    }

    public override async Task HandleAsync(DisplayHelpCommand command, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);
        await _bot.SendMessage(
            command.Message.Chat,
            _helpTemplate,
            replyParameters: new()
            {
                MessageId = command.Message.MessageId
            },
            cancellationToken: cancellationToken);
    }

    private static string BuildHelpTemplate(string botName)
    {
        var builder = new StringBuilder();
        builder.AppendLine("!help - display help");
        var botNameTemplate = $"{botName} - prefix to talk with LLM";
        builder.AppendLine(botNameTemplate);
        return builder.ToString();
    }
}
