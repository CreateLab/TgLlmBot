using System;
using Markdig;
using Markdig.Parsers.Inlines;
using Markdig.Renderers;
using Markdig.Renderers.Html.Inlines;
using TgLlmBot.Services.Markdown.Extensions.Spoiler.Parsers;
using TgLlmBot.Services.Markdown.Extensions.Spoiler.Renderers;

namespace TgLlmBot.Services.Markdown.Extensions.Spoiler;

public sealed class SpoilerExtension : IMarkdownExtension
{
    public void Setup(MarkdownPipelineBuilder pipeline)
    {
        ArgumentNullException.ThrowIfNull(pipeline);
        if (!pipeline.InlineParsers.Contains<SpoilerInlineParser>())
        {
            pipeline.InlineParsers.InsertBefore<LinkInlineParser>(new SpoilerInlineParser());
        }
    }

    public void Setup(MarkdownPipeline pipeline, IMarkdownRenderer renderer)
    {
        ArgumentNullException.ThrowIfNull(pipeline);
        ArgumentNullException.ThrowIfNull(renderer);
        if (!renderer.ObjectRenderers.Contains<NormalizeSpoilerRenderer>())
        {
            renderer.ObjectRenderers.InsertBefore<LinkInlineRenderer>(new NormalizeSpoilerRenderer());
        }
    }
}
