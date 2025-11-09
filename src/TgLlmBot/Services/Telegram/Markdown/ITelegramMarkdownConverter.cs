namespace TgLlmBot.Services.Telegram.Markdown;

public interface ITelegramMarkdownConverter
{
    string ConvertToTelegramMarkdown(string normalMarkdown);
}
