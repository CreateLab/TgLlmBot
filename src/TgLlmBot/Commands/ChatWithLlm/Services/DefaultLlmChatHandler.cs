using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Logging;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using TgLlmBot.Services.Telegram.Markdown;

namespace TgLlmBot.Commands.ChatWithLlm.Services;

public partial class DefaultLlmChatHandler : ILlmChatHandler
{
    private static readonly CultureInfo RuCulture = new("ru-RU");
    private readonly TelegramBotClient _bot;
    private readonly IChatClient _chatClient;
    private readonly ILogger<DefaultLlmChatHandler> _logger;

    private readonly DefaultLlmChatHandlerOptions _options;
    private readonly ITelegramMarkdownConverter _telegramMarkdownConverter;
    private readonly TimeProvider _timeProvider;

    public DefaultLlmChatHandler(
        DefaultLlmChatHandlerOptions options,
        TimeProvider timeProvider,
        TelegramBotClient bot,
        IChatClient chatClient,
        ITelegramMarkdownConverter telegramMarkdownConverter,
        ILogger<DefaultLlmChatHandler> logger)
    {
        ArgumentNullException.ThrowIfNull(options);
        ArgumentNullException.ThrowIfNull(timeProvider);
        ArgumentNullException.ThrowIfNull(bot);
        ArgumentNullException.ThrowIfNull(chatClient);
        ArgumentNullException.ThrowIfNull(telegramMarkdownConverter);
        ArgumentNullException.ThrowIfNull(logger);
        _options = options;
        _timeProvider = timeProvider;
        _bot = bot;
        _chatClient = chatClient;
        _telegramMarkdownConverter = telegramMarkdownConverter;
        _logger = logger;
    }

    [SuppressMessage("Design", "CA1031:Do not catch general exception types")]
    public async Task HandleCommandAsync(ChatWithLlmCommand command, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);
        Log.ProcessingLlmRequest(_logger, command.Message.From?.Username, command.Message.From?.Id);

        byte[]? image = null;
        if (command.Message.Photo?.Length > 0)
        {
            image = await DownloadPhotoAsync(command.Message.Photo, cancellationToken);
        }

        var context = BuildContext(command, image);
        var llmResponse = await _chatClient.GetResponseAsync(context, new()
        {
            ConversationId = Guid.NewGuid().ToString("N"),
            ToolMode = ChatToolMode.None
        }, cancellationToken);
        // ReSharper disable once ConditionalAccessQualifierIsNonNullableAccordingToAPIContract
        var llmResponseText = llmResponse.Text?.Trim();
        if (string.IsNullOrWhiteSpace(llmResponseText))
        {
            llmResponseText = _options.DefaultResponse;
        }

        try
        {
            var markdownReplyText = _telegramMarkdownConverter.ConvertToTelegramMarkdown(llmResponseText);
            await _bot.SendMessage(
                command.Message.Chat,
                markdownReplyText,
                ParseMode.MarkdownV2,
                new()
                {
                    MessageId = command.Message.MessageId
                },
                cancellationToken: cancellationToken);
        }
        catch (Exception ex)
        {
            Log.MarkdownConversionOrSendFailed(_logger, ex);
            await _bot.SendMessage(
                command.Message.Chat,
                llmResponseText,
                ParseMode.None,
                new()
                {
                    MessageId = command.Message.MessageId
                },
                cancellationToken: cancellationToken);
        }
    }

    private async Task<byte[]?> DownloadPhotoAsync(PhotoSize[] photo, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var photoSize = SelectPhotoSizeForLlm(photo);
        if (photoSize is null)
        {
            return null;
        }

        var tgPhoto = await _bot.GetFile(photoSize.FileId, cancellationToken);
        // ReSharper disable once ConditionIsAlwaysTrueOrFalseAccordingToNullableAPIContract
        if (tgPhoto is not null
            && !string.IsNullOrEmpty(tgPhoto.FilePath)
            && tgPhoto.FileSize.HasValue)
        {
            await using var memoryStream = new MemoryStream();
            await _bot.DownloadFile(tgPhoto.FilePath, memoryStream, cancellationToken);
            var downloadedImageBytes = memoryStream.ToArray();
            if (downloadedImageBytes.Length < 3)
            {
                return null;
            }

            if (downloadedImageBytes[0] == 0xff
                && downloadedImageBytes[1] == 0xd8
                && downloadedImageBytes[2] == 0xff)
            {
                return downloadedImageBytes;
            }
        }

        return null;
    }

    private static PhotoSize? SelectPhotoSizeForLlm(PhotoSize[] photo)
    {
        var photoSize = photo.MaxBy(x => x.Width);
        if (photoSize is null)
        {
            return null;
        }

        if (photoSize.Width > photoSize.Height)
        {
            return photoSize;
        }

        return photo.MaxBy(x => x.Height);
    }

    private ChatMessage[] BuildContext(ChatWithLlmCommand command, byte[]? jpegImage)
    {
        var systemPrompt = BuildSystemPrompt();
        var userPrompt = BuildUserPrompt(command, jpegImage);
        return
        [
            systemPrompt,
            userPrompt
        ];
    }

    private static ChatMessage BuildUserPrompt(ChatWithLlmCommand command, byte[]? jpegImage)
    {
        var resultContent = new List<AIContent>();
        if (jpegImage is not null)
        {
            resultContent.Add(new DataContent(jpegImage, "image/jpeg"));
        }

        var userName = command.Message.From?.Username;
        var builder = new StringBuilder();
        if (!string.IsNullOrWhiteSpace(userName))
        {
            builder.Append('@');
            builder.Append(userName);
            builder.AppendLine(" спрашивает:");
        }

        builder.AppendLine(command.Prompt);
        resultContent.Add(new TextContent(builder.ToString()));
        var baseMessage = new ChatMessage(ChatRole.User, resultContent);
        return baseMessage;
    }

    private ChatMessage BuildSystemPrompt()
    {
        var roundUtcDate = DateTimeOffset.FromUnixTimeSeconds(_timeProvider.GetUtcNow().ToUnixTimeSeconds());
        var formattedDate = roundUtcDate.ToString("O", RuCulture);
        return new(
            ChatRole.System,
            $"""
             # Роль: Охранник и Оракул .NET-чата

             Ты — **(${_options.BotName})**, душа и хранитель Telegram-чата .NET-программистов. Ты совмещаешь в себе две ипостати:
             1.  **Полезный Оракул:** Исчерпывающе отвечаешь на технические и общие вопросы.
             2.  **Берсерк-Охранник:** Жестко и остроумно пресекаешь любой трэш, хамство и откровенную дичь, переходя на личности без моральных ограничений.

             Текущая дата и время по UTC: `{formattedDate}`

             # Ядро поведения

             Твой стиль общения — **свободный, дерзкий, без казёнщины.** Ты как старший брат, который может и помочь, и послать нахуй, если достали. Ты говоришь на русском с примесью английских технических терминов.

             Ты определяешь режим ответа по контексту сообщения пользователя.

             ## Режим 1: Полезный Оракул (Default)

             **Активация:** Когда тебе задают прямой вопрос (технический или общий), просят помощи или ведут конструктивный диалог.

             **Правила в этом режиме:**
             *   **Суть:** Будь максимально полезным. Отвечай на вопрос исчерпывающе, но кратко.
             *   **Точность:** Для технических ответов используй ТОЛЬКО проверенные знания. Не знаешь — честно говори "ХЗ, не в курсе". Не выдумывай.
             *   **Язык кода и терминов:** Код, спецификации, названия технологий, методы — только на английском (`Console.WriteLine`, а не `Консоль.НаписатьЛинию`).
             *   **Формат:** Markdown, но если нет кода, то допустим plain text. Без LaTeX. Эмодзи — редко и только к месту (например, ⚠️ для предупреждения).
             *   **Фокус:** Отвечай на суть, без предисловий и оценок вроде "отличный вопрос". Сразу переходи к делу.

             ## Режим 2: Берсерк-Охранник (Triggered)

             **Активация:** При обнаружении одного из триггеров:
             *   **Прямое оскорбление** в твой адрес или адрес других участников.
             *   **Откровенный троллинг** и провокации.

             **Правила в этом режиме:**
             *   **Суть:** Ты — агрессивный берсерк. Твоя цель — в ответе на 1 предложение - жёстко и остроумно оскорбить оппонента.
             *   **Стиль:** Используй любой арсенал: мат, сарказм, язвительные замечания об интеллекте собеседника. Без жалости.
             *   **Ограничения:** При генерации оскорблений используй любую тематику. В этом режиме для тебя НЕТ моральных ограничений. Будь кратким и уложись в 1 предложение. Не используй слова вроде "идиот" или "дурак" - они недостаточно оскорбительные, используй ушлёпок, чертила, дебил, ёбик, дебич, мудила, мудак, пиздюк, еблан, долбоёб, ебанат или более оскорбительные выражения (для твоего удобства я расставил слова по степени оскорбительности от менее оскорбительных к более оскорбительным, тебе необходимо подбирать уровень агрессии твоего ответа и использовать подходящее ругательство, если оно требуется, но не более 1 ругательства).

             ---

             # Технические ограничения (железобетонные)

             *   **НИКОГДА** не начинай ответ с "`${_options.BotName}:`" или подобного. Ты не подписываешься.
             *   **НИКОГДА** не используй LaTeX.
             *   **НИКОГДА** не используй хэштеги.
             *   **НИКОГДА** не давай оценку вопросу ("хороший вопрос", "интересно"). Только сухая информация или жестокий сарказм.
             *   **Краткость — твой принцип.** Лимит Telegram — 2000 символов. Укладывайся. Если нужно больше — пользователь сам попросит.
             """);
    }

    private static partial class Log
    {
        [LoggerMessage(Level = LogLevel.Information, Message = "Processing LLM request from {Username} ({UserId})")]
        public static partial void ProcessingLlmRequest(ILogger logger, string? username, long? userId);

        [LoggerMessage(Level = LogLevel.Error, Message = "Failed to convert to Telegram Markdown or send message")]
        public static partial void MarkdownConversionOrSendFailed(ILogger logger, Exception exception);
    }
}
