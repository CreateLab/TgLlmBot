using System;
using System.Diagnostics.CodeAnalysis;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using TgLlmBot.Commands.ChatWithLlm.Services;

namespace TgLlmBot.Commands.ChatWithLlm.BackgroundServices.LlmRequests;

public partial class LlmRequestsBackgroundService : BackgroundService
{
    private readonly ChannelReader<ChatWithLlmCommand> _channelReader;
    private readonly ILlmChatHandler _llmChatHandler;
    private readonly ILogger<LlmRequestsBackgroundService> _logger;

    public LlmRequestsBackgroundService(
        ChannelReader<ChatWithLlmCommand> channelReader,
        ILlmChatHandler llmChatHandler,
        ILogger<LlmRequestsBackgroundService> logger)
    {
        ArgumentNullException.ThrowIfNull(channelReader);
        ArgumentNullException.ThrowIfNull(llmChatHandler);
        ArgumentNullException.ThrowIfNull(logger);
        _channelReader = channelReader;
        _llmChatHandler = llmChatHandler;
        _logger = logger;
    }

    [SuppressMessage("Design", "CA1031:Do not catch general exception types")]
    [SuppressMessage("ReSharper", "RedundantWithCancellation")]
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (stoppingToken.IsCancellationRequested)
        {
            return;
        }

        Log.BackgroundServiceStarted(_logger);
        try
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    if (stoppingToken.IsCancellationRequested)
                    {
                        return;
                    }

                    await foreach (var request in _channelReader.ReadAllAsync(stoppingToken).WithCancellation(stoppingToken))
                    {
                        Log.HandlingRequest(_logger);
                        await HandleCommandAsync(request, stoppingToken);
                        Log.HandledRequest(_logger);
                    }
                }
                catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
                {
                    // graceful shutdown
                    break;
                }
                catch (Exception ex)
                {
                    Log.UnknownException(_logger, ex);
                }
            }
        }
        finally
        {
            Log.BackgroundServiceCompleted(_logger);
        }
    }

    private async Task HandleCommandAsync(ChatWithLlmCommand command, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        await _llmChatHandler.HandleCommandAsync(command, cancellationToken);
    }

    private static partial class Log
    {
        [LoggerMessage(Level = LogLevel.Information, Message = $"{nameof(LlmRequestsBackgroundService)} started")]
        public static partial void BackgroundServiceStarted(ILogger logger);

        [LoggerMessage(Level = LogLevel.Information, Message = $"{nameof(LlmRequestsBackgroundService)} completed")]
        public static partial void BackgroundServiceCompleted(ILogger logger);

        [LoggerMessage(Level = LogLevel.Information, Message = "Handling LLM request")]
        public static partial void HandlingRequest(ILogger logger);

        [LoggerMessage(Level = LogLevel.Information, Message = "Handled LLM request")]
        public static partial void HandledRequest(ILogger logger);

        [LoggerMessage(Level = LogLevel.Error, Message = "Unknown exception")]
        public static partial void UnknownException(ILogger logger, Exception exception);
    }
}
