using System.ComponentModel.DataAnnotations;
using System.Diagnostics.CodeAnalysis;

namespace TgLlmBot.Configuration.Options.Telegram;

[SuppressMessage("ReSharper", "PreferConcreteValueOverDefault")]
public class TelegramOptions
{
    [Required]
    [MaxLength(10000)]
    public string BotToken { get; set; } = default!;

    [Required]
    [MinLength(1)]
    public long[] AllowedChatIds { get; set; } = default!;

    [Required]
    [MinLength(1)]
    [MaxLength(100)]
    public string BotName { get; set; } = default!;
}
