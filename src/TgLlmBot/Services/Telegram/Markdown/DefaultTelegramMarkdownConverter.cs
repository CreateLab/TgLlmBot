using Markdig;
using Markdig.Extensions.EmphasisExtras;
using TgLlmBot.Services.Markdown.Extensions.Spoiler.Builder;

namespace TgLlmBot.Services.Telegram.Markdown;

public class DefaultTelegramMarkdownConverter : ITelegramMarkdownConverter
{
    private static readonly MarkdownPipeline MarkdownPipeline = new MarkdownPipelineBuilder()
        .UseSpoilers()
        .UseAlertBlocks()
        .UseAutoIdentifiers()
        .UseCustomContainers()
        .UseDefinitionLists()
        .UseEmphasisExtras(EmphasisExtraOptions.Strikethrough)
        .UseGridTables()
        .UseMediaLinks()
        .UsePipeTables()
        .UseListExtras()
        .UseTaskLists()
        .UseAutoLinks()
        .UseGenericAttributes()
        .Build();

    public string ConvertToTelegramMarkdown(string normalMarkdown)
    {
        var document = Markdig.Markdown.Parse(normalMarkdown, MarkdownPipeline);
        return new TelegramMarkdownRenderer().Render(document);
    }
}
