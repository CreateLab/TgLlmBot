using System;
using Microsoft.EntityFrameworkCore;
using TgLlmBot.DataAccess.EntityTypeConfigurations;
using TgLlmBot.DataAccess.Models;

namespace TgLlmBot.DataAccess;

public class BotDbContext : DbContext
{
    public BotDbContext(DbContextOptions<BotDbContext> options)
        : base(options)
    {
    }

    public DbSet<DbChatMessage> ChatHistory { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        ArgumentNullException.ThrowIfNull(modelBuilder);
        modelBuilder.ApplyConfiguration(new DbChatMessageEntityTypeConfiguration());
        base.OnModelCreating(modelBuilder);
    }
}
