using System;
using Telegram.Bot.Types;

namespace TgLlmBot.Services.Telegram.SelfInformation;

public class DefaultTelegramSelfInformation : ITelegramSelfInformation
{
    private volatile User _user = null!;

    public User GetSelf()
    {
        return _user;
    }

    public void SetSelf(User user)
    {
        ArgumentNullException.ThrowIfNull(user);
        _user = user;
    }
}
