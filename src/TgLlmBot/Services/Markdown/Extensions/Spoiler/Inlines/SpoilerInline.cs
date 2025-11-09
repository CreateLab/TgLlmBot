using System.Diagnostics;
using Markdig.Helpers;
using Markdig.Syntax.Inlines;

namespace TgLlmBot.Services.Markdown.Extensions.Spoiler.Inlines;

[DebuggerDisplay("{" + nameof(Content) + "}")]
public sealed class SpoilerInline : LeafInline
{
    public StringSlice Content { get; init; }
}
