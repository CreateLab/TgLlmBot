using Telegram.Bot.Types;

namespace TgLlmBot.Services.Telegram.SelfInformation;

public interface ITelegramSelfInformation
{
    User GetSelf();
}
