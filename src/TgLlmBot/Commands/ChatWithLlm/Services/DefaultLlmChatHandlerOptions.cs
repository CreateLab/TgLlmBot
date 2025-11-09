using System;

namespace TgLlmBot.Commands.ChatWithLlm.Services;

public class DefaultLlmChatHandlerOptions
{
    public DefaultLlmChatHandlerOptions(string botName)
    {
        if (string.IsNullOrWhiteSpace(botName))
        {
            throw new ArgumentException("Value cannot be null or whitespace.", nameof(botName));
        }

        BotName = botName;
    }

    public string BotName { get; }
}
