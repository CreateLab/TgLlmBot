using System;
using TgLlmBot.Configuration.Options;
using TgLlmBot.Configuration.TypedConfiguration.Llm;
using TgLlmBot.Configuration.TypedConfiguration.Telegram;

namespace TgLlmBot.Configuration.TypedConfiguration;

public class ApplicationConfiguration
{
    private ApplicationConfiguration(TelegramConfiguration telegram, LlmConfiguration llm)
    {
        ArgumentNullException.ThrowIfNull(telegram);
        ArgumentNullException.ThrowIfNull(llm);
        Telegram = telegram;
        Llm = llm;
    }

    public TelegramConfiguration Telegram { get; }
    public LlmConfiguration Llm { get; }

    public static ApplicationConfiguration Convert(ApplicationOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);
        var telegram = TelegramConfiguration.Convert(options.Telegram);
        var llm = LlmConfiguration.Convert(options.Llm);
        return new(
            telegram,
            llm);
    }
}
