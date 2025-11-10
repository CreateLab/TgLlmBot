using System.ComponentModel.DataAnnotations;

namespace TgLlmBot.Configuration.Options.DataAccess;

public class DataAccessOptions
{
    [Required]
    [MaxLength(1000)]
    public string PostgresConnectionString { get; set; } = default!;
}
