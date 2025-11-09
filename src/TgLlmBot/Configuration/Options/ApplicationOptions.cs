using System.ComponentModel.DataAnnotations;
using System.Diagnostics.CodeAnalysis;
using TgLlmBot.Configuration.Options.Llm;
using TgLlmBot.Configuration.Options.Telegram;

namespace TgLlmBot.Configuration.Options;

[SuppressMessage("ReSharper", "PreferConcreteValueOverDefault")]
public class ApplicationOptions
{
    [Required]
    public TelegramOptions Telegram { get; set; } = default!;

    [Required]
    public LlmOptions Llm { get; set; } = default!;
}
