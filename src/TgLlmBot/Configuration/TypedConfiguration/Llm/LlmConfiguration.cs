using System;
using TgLlmBot.Configuration.Options.Llm;

namespace TgLlmBot.Configuration.TypedConfiguration.Llm;

public class LlmConfiguration
{
    private LlmConfiguration(
        Uri endpoint,
        string apiKey,
        string model)
    {
        ArgumentNullException.ThrowIfNull(endpoint);
        ArgumentNullException.ThrowIfNull(apiKey);
        ArgumentNullException.ThrowIfNull(model);

        if (string.IsNullOrEmpty(apiKey))
        {
            throw new ArgumentException("Value cannot be null or empty.", nameof(apiKey));
        }

        if (string.IsNullOrEmpty(model))
        {
            throw new ArgumentException("Value cannot be null or empty.", nameof(model));
        }


        Endpoint = endpoint;
        ApiKey = apiKey;
        Model = model;
    }

    public Uri Endpoint { get; }
    public string ApiKey { get; }
    public string Model { get; }

    public static LlmConfiguration Convert(LlmOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);
        if (!Uri.TryCreate(options.Endpoint, UriKind.Absolute, out var typedEndpoint))
        {
            throw new ArgumentException("Invalid endpoint.", nameof(options));
        }

        return new(
            typedEndpoint,
            options.ApiKey,
            options.Model);
    }
}
