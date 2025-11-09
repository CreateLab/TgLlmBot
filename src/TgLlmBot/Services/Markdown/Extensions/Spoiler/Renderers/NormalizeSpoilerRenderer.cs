using System;
using Markdig.Renderers;
using Markdig.Renderers.Html;
using TgLlmBot.Services.Markdown.Extensions.Spoiler.Inlines;

namespace TgLlmBot.Services.Markdown.Extensions.Spoiler.Renderers;

public sealed class NormalizeSpoilerRenderer : HtmlObjectRenderer<SpoilerInline>
{
    protected override void Write(HtmlRenderer renderer, SpoilerInline obj)
    {
        ArgumentNullException.ThrowIfNull(renderer);
        ArgumentNullException.ThrowIfNull(obj);
        if (renderer.EnableHtmlForInline)
        {
            renderer.Write("<span").WriteAttributes(obj).Write(">");
        }

        renderer.WriteEscape(obj.Content);
        if (renderer.EnableHtmlForInline)
        {
            renderer.Write("</span>");
        }
    }
}
