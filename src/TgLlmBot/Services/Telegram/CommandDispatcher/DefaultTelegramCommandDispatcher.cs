using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using TgLlmBot.Commands.ChatWithLlm;
using TgLlmBot.Commands.DisplayHelp;
using TgLlmBot.Services.DataAccess;
using TgLlmBot.Services.Telegram.SelfInformation;

namespace TgLlmBot.Services.Telegram.CommandDispatcher;

public class DefaultTelegramCommandDispatcher : ITelegramCommandDispatcher
{
    private static readonly HashSet<MessageType> AllowedMessageTypes =
    [
        MessageType.Text,
        MessageType.Photo
    ];

    private readonly ChatWithLlmCommandHandler _chatWithLlmCommandHandler;
    private readonly DisplayHelpCommandHandler _displayHelpCommandHandler;
    private readonly ITelegramMessageStorage _messageStorage;
    private readonly DefaultTelegramCommandDispatcherOptions _options;
    private readonly ITelegramSelfInformation _selfInformation;

    public DefaultTelegramCommandDispatcher(
        DefaultTelegramCommandDispatcherOptions options,
        ITelegramSelfInformation selfInformation,
        DisplayHelpCommandHandler displayHelpCommandHandler,
        ChatWithLlmCommandHandler chatWithLlmCommandHandler,
        ITelegramMessageStorage messageStorage)
    {
        ArgumentNullException.ThrowIfNull(options);
        ArgumentNullException.ThrowIfNull(selfInformation);
        ArgumentNullException.ThrowIfNull(displayHelpCommandHandler);
        ArgumentNullException.ThrowIfNull(chatWithLlmCommandHandler);
        ArgumentNullException.ThrowIfNull(messageStorage);
        _options = options;
        _selfInformation = selfInformation;
        _displayHelpCommandHandler = displayHelpCommandHandler;
        _chatWithLlmCommandHandler = chatWithLlmCommandHandler;
        _messageStorage = messageStorage;
    }

    public async Task HandleMessageAsync(Message? message, UpdateType type, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        if (message is null)
        {
            return;
        }

        if (!AllowedMessageTypes.Contains(message.Type))
        {
            return;
        }

        var self = _selfInformation.GetSelf();
        await _messageStorage.StoreMessageAsync(message, self, cancellationToken);
        switch (message.Text)
        {
            case "!help":
                {
                    var command = new DisplayHelpCommand(message, type);
                    await _displayHelpCommandHandler.HandleAsync(command, cancellationToken);
                    return;
                }
        }

        var prompt = message.Text ?? message.Caption;
        if (message.Chat.Type == ChatType.Private)
        {
            var command = new ChatWithLlmCommand(message, type, self, prompt);
            await _chatWithLlmCommandHandler.HandleAsync(command, cancellationToken);
        }
        else if (message.Chat.Type is ChatType.Group or ChatType.Supergroup)
        {
            if (prompt?.StartsWith(_options.BotName, StringComparison.InvariantCultureIgnoreCase) is true
                || (message.ReplyToMessage?.From is not null && message.ReplyToMessage.From.Id == self.Id))
            {
                var command = new ChatWithLlmCommand(message, type, self, prompt);
                await _chatWithLlmCommandHandler.HandleAsync(command, cancellationToken);
            }
        }
    }
}
