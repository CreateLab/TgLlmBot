using System;
using Markdig;

namespace TgLlmBot.Services.Markdown.Extensions.Spoiler.Builder;

public static class MarkdownPipelineBuilderExtensions
{
    public static MarkdownPipelineBuilder UseSpoilers(this MarkdownPipelineBuilder pipeline)
    {
        ArgumentNullException.ThrowIfNull(pipeline);
        var extensions = pipeline.Extensions;

        if (!extensions.Contains<SpoilerExtension>())
        {
            extensions.Add(new SpoilerExtension());
        }

        return pipeline;
    }
}
