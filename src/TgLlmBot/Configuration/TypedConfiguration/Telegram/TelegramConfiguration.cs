using System;
using System.Collections.Generic;
using System.Linq;
using TgLlmBot.Configuration.Options.Telegram;

namespace TgLlmBot.Configuration.TypedConfiguration.Telegram;

public class TelegramConfiguration
{
    private TelegramConfiguration(
        string botToken,
        IReadOnlySet<long> allowedChatIds,
        string botName)
    {
        ArgumentNullException.ThrowIfNull(botToken);
        if (string.IsNullOrEmpty(botToken))
        {
            throw new ArgumentException("Value cannot be null or empty.", nameof(botToken));
        }

        ArgumentNullException.ThrowIfNull(allowedChatIds);
        if ((allowedChatIds.Count > 0) is not true)
        {
            throw new ArgumentException("Value should contain at least 1 element", nameof(allowedChatIds));
        }

        BotToken = botToken;
        AllowedChatIds = allowedChatIds;
        BotName = botName;
    }

    public string BotToken { get; }

    public IReadOnlySet<long> AllowedChatIds { get; }

    public string BotName { get; }

    public static TelegramConfiguration Convert(TelegramOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);
        return new(
            options.BotToken,
            options.AllowedChatIds.ToHashSet(),
            options.BotName);
    }
}
