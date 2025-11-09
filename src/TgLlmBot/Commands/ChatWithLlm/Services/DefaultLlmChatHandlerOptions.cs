using System;

namespace TgLlmBot.Commands.ChatWithLlm.Services;

public class DefaultLlmChatHandlerOptions
{
    public DefaultLlmChatHandlerOptions(string botName, string defaultResponse)
    {
        if (string.IsNullOrWhiteSpace(botName))
        {
            throw new ArgumentException("Value cannot be null or whitespace.", nameof(botName));
        }

        if (string.IsNullOrWhiteSpace(defaultResponse))
        {
            throw new ArgumentException("Value cannot be null or whitespace.", nameof(defaultResponse));
        }

        BotName = botName;
        DefaultResponse = defaultResponse;
    }

    public string BotName { get; }

    public string DefaultResponse { get; }
}
