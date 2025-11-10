using System;
using TgLlmBot.Configuration.Options;
using TgLlmBot.Configuration.TypedConfiguration.DataAccess;
using TgLlmBot.Configuration.TypedConfiguration.Llm;
using TgLlmBot.Configuration.TypedConfiguration.Telegram;

namespace TgLlmBot.Configuration.TypedConfiguration;

public class ApplicationConfiguration
{
    private ApplicationConfiguration(
        TelegramConfiguration telegram,
        LlmConfiguration llm,
        DataAccessConfiguration dataAccess)
    {
        ArgumentNullException.ThrowIfNull(telegram);
        ArgumentNullException.ThrowIfNull(llm);
        ArgumentNullException.ThrowIfNull(dataAccess);
        Telegram = telegram;
        Llm = llm;
        DataAccess = dataAccess;
    }

    public TelegramConfiguration Telegram { get; }
    public LlmConfiguration Llm { get; }

    public DataAccessConfiguration DataAccess { get; }

    public static ApplicationConfiguration Convert(ApplicationOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);
        var telegram = TelegramConfiguration.Convert(options.Telegram);
        var llm = LlmConfiguration.Convert(options.Llm);
        var dataAccess = DataAccessConfiguration.Convert(options.DataAccess);
        return new(
            telegram,
            llm,
            dataAccess);
    }
}
