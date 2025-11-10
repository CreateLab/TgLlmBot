using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace TgLlmBot.DataAccess.Design;

public class DesignTimeBotDbContextFactory : IDesignTimeDbContextFactory<BotDbContext>
{
    public BotDbContext CreateDbContext(string[] args)
    {
        var optionsBuilder = new DbContextOptionsBuilder<BotDbContext>();
        optionsBuilder.UseNpgsql(
            "Host=localhost;Port=5432;Database=tgbot;Username=postgres;Password=postgres;",
            options =>
            {
                options.SetPostgresVersion(18, 0);
                options.MigrationsAssembly(typeof(DesignTimeBotDbContextFactory).Assembly);
            });
        return new(optionsBuilder.Options);
    }
}
