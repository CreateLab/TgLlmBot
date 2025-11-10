using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;
using Markdig;
using Markdig.Extensions.AutoIdentifiers;
using Markdig.Extensions.EmphasisExtras;
using Markdig.Extensions.Tables;
using Markdig.Syntax;
using Markdig.Syntax.Inlines;

namespace TgLlmBot.Services.Telegram.Markdown;

/// <summary>
///     Converts Markdown to Telegram MarkdownV2 format with proper escaping.
/// </summary>
public class DefaultTelegramMarkdownConverter : ITelegramMarkdownConverter
{
    private const string InputToTest = """
                                       Поехали по делу. Цель реальная, но без херни в стиле “-50 кг за 2 месяца”.

                                       План в шагах:

                                       1. Медосмотр (обязательно)
                                       - Сходи к врачу/эндокринологу/кардиологу.
                                       - Минимум сдать:
                                         - Общий анализ крови, липидный профиль.
                                         - Глюкоза, инсулин, HbA1c.
                                         - TSH, Т3, Т4 (щитовидка).
                                         - Печёночные, почечные.
                                       - Цель: исключить диабет, проблемы со щитовидкой, сердцем. Если там жопа — план корректируется врачом.

                                       2. Базовые цифры
                                       - Рост, вес, обхват талии.
                                       - Посчитать примерный TDEE (суточный расход калорий).
                                       - Цель: дефицит примерно 500–900 ккал/день. Это ≈0.5–1 кг/неделю.
                                       - Не опускаться ниже 1800–2000 ккал без наблюдения врача (при таком весе).

                                       3. Питание (без экстремизма)
                                       Основная идея: не “диета”, а адекватный рацион.

                                       - Белок: 1.6–2.0 г/кг от целевого веса (то есть от 100 кг ≈ 160–200 г/день).
                                         - Курица, индейка, говядина, рыба, яйца, творог, греческий йогурт, бобовые.
                                       - Углеводы:
                                         - Оставить сложные: крупы, овощи, фрукты, цельнозерновой хлеб.
                                         - Выкинуть/урезать до минимума: сладкое, соки, газировки, фастфуд, выпечку.
                                       - Жиры:
                                         - Норм: орехи, оливковое масло, авокадо, жирная рыба.
                                         - Резать трансжиры, жареное в масле “до корочки”.
                                       - Структура:
                                         - 3–4 приёма пищи, без зажоров ночью.
                                         - Каждый приём: белок + овощи + (по ситуации) сложные угли.
                                       - Пить:
                                         - 2–3 л воды в день.
                                       - Алкоголь:
                                         - Максимально урезать, лучше в ноль.

                                       4. Движение (без геройства)
                                       Старт с тем, что не убивает суставы.

                                       - Неделя 1–2:
                                         - 5–10k шагов в день (сколько осилишь стабильно).
                                       - Неделя 3+:
                                         - 30–45 мин ходьбы в быстром темпе 5–6 раз в неделю.
                                       - Через 4–6 недель:
                                         - Подключить 2–3 лёгкие силовые тренировки в неделю:
                                           - Приседания с собственным весом, отжимания от стены/скамьи, резинки, машинки в зале.
                                       - Цель: сохранить мышцы, сжигать жир, не убить колени.

                                       5. Контроль прогресса
                                       - Взвешивание: 1–2 раза в неделю утром натощак.
                                       - Обхват талии: раз в 2 недели.
                                       - Фото раз в месяц.
                                       - Если вес не двигается 2–3 недели:
                                         - Чуть урезать ещё 150–200 ккал или добавить 2–3k шагов.

                                       6. Сон и стресс
                                       - Сон: 7–9 часов. Недосып = голод + срыв.
                                       - Стресс: убрать постоянную жвачку из работы/мессенджеров/новостей, не заедать проблемы.

                                       7. Ограничения и реальность
                                       - Нормальный темп: 0.5–1 кг/неделя.
                                         - -50 кг займёт примерно 9–15 месяцев. Это нормально.
                                       - Плато будут — это не повод срываться, а повод подкрутить калории/движение.
                                       - Не делать:
                                         - Сушиться на 800 ккал.
                                         - Жрать только гречку.
                                         - Кето/детокс/голодание “пока не упаду” — особенно с твоим стартовым весом.

                                       8. Пример простого дня (набросок)
                                       - Завтрак: яйца + творог + огурец/помидор.
                                       - Обед: курица/говядина + гречка/рис + салат.
                                       - Перекус: йогурт/творог/сыр + орехи.
                                       - Ужин: рыба/курица + овощи.
                                       Под себя по вкусу настраиваешь, главное — вписаться в калории и добрать белок.

                                       Если хочешь, в следующем сообщении могу под твой рост/возраст/активность посчитать пример конкретных калорий и расписать день по граммам.
                                       """;

    private static readonly MarkdownPipeline Pipeline = new MarkdownPipelineBuilder()
        .UseAlertBlocks()
        .UseAutoIdentifiers(AutoIdentifierOptions.GitHub)
        .UseMathematics()
        .UseCustomContainers()
        .UseDefinitionLists()
        .UseEmphasisExtras(EmphasisExtraOptions.Strikethrough)
        .UseGridTables()
        .UseMediaLinks()
        .UsePipeTables()
        .UseListExtras()
        .UseTaskLists()
        .UseAutoLinks()
        .UseGenericAttributes() // Must be last as it is one parser that is modifying other parsers
        .Build();

    public string ConvertToTelegramMarkdown(string normalMarkdown)
    {
        // Parse the markdown using Markdig
        var document = Markdig.Markdown.Parse(normalMarkdown, Pipeline);
        // Convert to Telegram MarkdownV2
        var result = new StringBuilder();
        ConvertDocument(document, result);
        return result.ToString();
    }

    public static string Test()
    {
        return new DefaultTelegramMarkdownConverter().ConvertToTelegramMarkdown(InputToTest);
    }

    private static void ConvertDocument(MarkdownDocument document, StringBuilder output)
    {
        var isFirst = true;
        foreach (var block in document)
        {
            if (!isFirst && output.Length > 0 && output[^1] != '\n')
            {
                output.AppendLine();
            }

            ConvertBlock(block, output);
            isFirst = false;
        }
    }

    private static void ConvertBlock(Block block, StringBuilder output)
    {
        switch (block)
        {
            case HeadingBlock heading:
                ConvertHeading(heading, output);
                break;
            case ParagraphBlock paragraph:
                ConvertParagraph(paragraph, output);
                break;
            case ListBlock list:
                ConvertListBlock(list, output);
                break;
            case QuoteBlock quote:
                ConvertQuoteBlock(quote, output);
                break;
            case CodeBlock codeBlock:
                ConvertCodeBlock(codeBlock, output);
                break;
            case Table table:
                ConvertTableBlock(table, output);
                break;
            case ThematicBreakBlock:
                output.AppendLine(EscapeText("---"));
                break;
            case HtmlBlock html:
                ConvertHtmlBlock(html, output);
                break;
            case ContainerBlock container:
                foreach (var childBlock in container)
                {
                    ConvertBlock(childBlock, output);
                }

                break;
            default:
                // For other block types, try to extract inline content
                if (block is LeafBlock { Inline: not null } leaf)
                {
                    ConvertInline(leaf.Inline, output, false);
                    output.AppendLine();
                }

                break;
        }
    }

    private static void ConvertHtmlBlock(HtmlBlock html, StringBuilder output)
    {
        // Convert simple HTML tags to Telegram markdown
        var content = html.Lines.ToString() ?? string.Empty;
        content = Regex.Replace(content, "<b>(.*?)</b>", "*$1*", RegexOptions.IgnoreCase);
        content = Regex.Replace(content, "<i>(.*?)</i>", "_$1_", RegexOptions.IgnoreCase);
        content = Regex.Replace(content, "<u>(.*?)</u>", "__$1__", RegexOptions.IgnoreCase);
        content = Regex.Replace(content, "<s>(.*?)</s>", "~$1~", RegexOptions.IgnoreCase);
        content = Regex.Replace(content, "<code>(.*?)</code>", "`$1`", RegexOptions.IgnoreCase);
        content = Regex.Replace(content, "<[^>]+>", string.Empty, RegexOptions.IgnoreCase); // Remove other HTML tags

        output.AppendLine(EscapeText(content) + "\n");
    }

    private static void ConvertTableBlock(Table table, StringBuilder output)
    {
        // Telegram doesn't support tables, so we'll render as preformatted text
        output.AppendLine("```");

        foreach (var row in table)
        {
            if (row is TableRow tableRow)
            {
                var cells = new List<string>();
                foreach (var cell in tableRow)
                {
                    if (cell is TableCell tableCell)
                    {
                        var cellContent = new StringBuilder();
                        foreach (var block in tableCell)
                        {
                            if (block is ParagraphBlock { Inline: not null } para)
                            {
                                foreach (var inline in para.Inline)
                                {
                                    ConvertInline(inline, cellContent, false);
                                }
                            }
                        }

                        cells.Add(cellContent.ToString());
                    }
                }

                output.AppendLine(EscapeCodeBlock(string.Join(" | ", cells)));
            }
        }

        output.AppendLine("```\n");
    }

    private static void ConvertHeading(HeadingBlock heading, StringBuilder output)
    {
        // Telegram doesn't have native heading support, use bold with escaped '#' prefix
        var level = heading.Level;
        output.Append(EscapeText(new string('#', level) + " "));
        output.Append('*');
        ConvertInline(heading.Inline, output, true);
        output.Append('*');
        output.AppendLine();
    }

    private static void ConvertParagraph(ParagraphBlock paragraph, StringBuilder output)
    {
        if (paragraph.Inline != null)
        {
            ConvertInline(paragraph.Inline, output, false);
            output.AppendLine();
        }
    }

    private static void ConvertCodeBlock(CodeBlock codeBlock, StringBuilder output)
    {
        var fencedCodeBlock = codeBlock as FencedCodeBlock;
        var language = fencedCodeBlock?.Info ?? string.Empty;

        output.Append("```");
        if (!string.IsNullOrEmpty(language))
        {
            output.Append(language);
        }

        output.AppendLine();

        // Extract code content
        var code = codeBlock.Lines.ToString();
        output.Append(EscapeCodeBlock(code));

        if (!code.EndsWith('\n'))
        {
            output.AppendLine();
        }

        output.AppendLine("```");
    }

    private static void ConvertQuoteBlock(QuoteBlock quote, StringBuilder output)
    {
        foreach (var childBlock in quote)
        {
            if (childBlock is ParagraphBlock paragraph)
            {
                output.Append('>');
                ConvertInline(paragraph.Inline, output, false);
                output.AppendLine();
            }
            else
            {
                output.Append('>');
                ConvertBlock(childBlock, output);
            }
        }
    }

    private static void ConvertListBlock(ListBlock list, StringBuilder output)
    {
        foreach (var item in list)
        {
            if (item is ListItemBlock listItem)
            {
                var prefix = list.IsOrdered
                    ? EscapeText($"{listItem.Order}. ")
                    : EscapeText("• ");

                output.Append(prefix);

                var isFirst = true;
                foreach (var childBlock in listItem)
                {
                    if (!isFirst)
                    {
                        output.Append("  "); // Indent continuation
                    }

                    ConvertBlock(childBlock, output);
                    isFirst = false;
                }
            }
        }
    }

    private static void ConvertInline(Inline? inline, StringBuilder output, bool inBold)
    {
        var current = inline;
        while (current != null)
        {
            switch (current)
            {
                case LiteralInline literal:
                    output.Append(EscapeText(literal.Content.ToString()));
                    break;

                case EmphasisInline emphasis:
                    ConvertEmphasis(emphasis, output, inBold);
                    break;

                case CodeInline code:
                    output.Append('`');
                    output.Append(EscapeInlineCode(code.Content));
                    output.Append('`');
                    break;

                case LinkInline link:
                    ConvertLink(link, output);
                    break;

                case LineBreakInline:
                    output.AppendLine();
                    break;

                case ContainerInline container:
                    ConvertInline(container.FirstChild, output, inBold);
                    break;

                default:
                    // Handle unknown inline elements by extracting text
                    if (current is LeafInline leaf)
                    {
                        output.Append(EscapeText(leaf.ToString()));
                    }

                    break;
            }

            current = current.NextSibling;
        }
    }

    private static void ConvertEmphasis(EmphasisInline emphasis, StringBuilder output, bool inBold)
    {
        string marker;
        var childInBold = inBold;

        // Determine the type of emphasis
        if (emphasis.DelimiterChar is '*' or '_')
        {
            if (emphasis.DelimiterCount == 1)
            {
                // Italic
                marker = "_";
                childInBold = false;
            }
            else if (emphasis.DelimiterCount == 2)
            {
                // Bold
                marker = "*";
                childInBold = true;
            }
            else
            {
                // Both (bold + italic) - use bold marker, nest italic inside
                marker = "*";
                childInBold = true;
            }
        }
        else if (emphasis.DelimiterChar == '~')
        {
            // Strikethrough
            marker = "~";
            childInBold = false;
        }
        else
        {
            // Unknown, just process children
            ConvertInline(emphasis.FirstChild, output, inBold);
            return;
        }

        output.Append(marker);
        ConvertInline(emphasis.FirstChild, output, childInBold);
        output.Append(marker);
    }

    private static void ConvertLink(LinkInline link, StringBuilder output)
    {
        if (link.IsImage)
        {
            // Telegram doesn't support image markdown, just output the alt text
            output.Append(EscapeText("[Image: "));
            ConvertInline(link.FirstChild, output, false);
            output.Append(EscapeText("]"));
        }
        else
        {
            output.Append('[');
            ConvertInline(link.FirstChild, output, false);
            output.Append("](");
            output.Append(EscapeLinkUrl(link.Url ?? string.Empty));
            output.Append(')');
        }
    }

    /// <summary>
    ///     Escapes regular text according to Telegram MarkdownV2 rules
    /// </summary>
    private static string EscapeText(string? text)
    {
        if (string.IsNullOrEmpty(text))
        {
            return string.Empty;
        }

        var sb = new StringBuilder(text.Length * 2);
        foreach (var c in text)
        {
            // Characters that must be escaped in regular text:
            // '_', '*', '[', ']', '(', ')', '~', '`', '>', '#', '+', '-', '=', '|', '{', '}', '.', '!'
            if (c is '_' or '*' or '[' or ']' or '(' or ')' or '~' or '`' or '>' or '#' or '+' or '-' or '=' or '|' or '{' or '}' or '.' or '!' or '\\')
            {
                sb.Append('\\');
            }

            sb.Append(c);
        }

        return sb.ToString();
    }

    /// <summary>
    ///     Escapes text inside inline code (backticks)
    /// </summary>
    private static string EscapeInlineCode(string code)
    {
        if (string.IsNullOrEmpty(code))
        {
            return code;
        }

        var sb = new StringBuilder(code.Length * 2);
        foreach (var c in code)
        {
            // Inside code entities, only '`' and '\' need escaping
            if (c is '`' or '\\')
            {
                sb.Append('\\');
            }

            sb.Append(c);
        }

        return sb.ToString();
    }

    /// <summary>
    ///     Escapes text inside code blocks
    /// </summary>
    private static string EscapeCodeBlock(string code)
    {
        if (string.IsNullOrEmpty(code))
        {
            return code;
        }

        var sb = new StringBuilder(code.Length * 2);
        foreach (var c in code)
        {
            // Inside pre/code entities, only '`' and '\' need escaping
            if (c is '`' or '\\')
            {
                sb.Append('\\');
            }

            sb.Append(c);
        }

        return sb.ToString();
    }

    /// <summary>
    ///     Escapes URLs in link definitions
    /// </summary>
    private static string EscapeLinkUrl(string url)
    {
        if (string.IsNullOrEmpty(url))
        {
            return url;
        }

        var sb = new StringBuilder(url.Length * 2);
        foreach (var c in url)
        {
            // Inside link URL part, ')' and '\' must be escaped
            if (c is ')' or '\\')
            {
                sb.Append('\\');
            }

            sb.Append(c);
        }

        return sb.ToString();
    }
}
