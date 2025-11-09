using System;
using System.Collections.Generic;

namespace TgLlmBot.Services.Telegram.RequestHandler;

public class DefaultTelegramRequestHandlerOptions
{
    public DefaultTelegramRequestHandlerOptions(
        DateTimeOffset skipMessagesOlderThan,
        IReadOnlySet<long> allowedChatIds)
    {
        ArgumentNullException.ThrowIfNull(allowedChatIds);
        SkipMessagesOlderThan = skipMessagesOlderThan;
        AllowedChatIds = allowedChatIds;
    }

    public DateTimeOffset SkipMessagesOlderThan { get; }
    public IReadOnlySet<long> AllowedChatIds { get; }
}
