using System;
using Npgsql;
using TgLlmBot.Configuration.Options.DataAccess;

namespace TgLlmBot.Configuration.TypedConfiguration.DataAccess;

public class DataAccessConfiguration
{
    private DataAccessConfiguration(string postgresConnectionString)
    {
        if (string.IsNullOrEmpty(postgresConnectionString))
        {
            throw new ArgumentException("Value cannot be null or empty.", nameof(postgresConnectionString));
        }

        PostgresConnectionString = postgresConnectionString;
    }

    public string PostgresConnectionString { get; }

    public static DataAccessConfiguration Convert(DataAccessOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);
        var builder = new NpgsqlConnectionStringBuilder(options.PostgresConnectionString);
        return new(builder.ConnectionString);
    }
}
