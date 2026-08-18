using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace team_hub_chat.Data;

public sealed class ChatDbContextFactory : IDesignTimeDbContextFactory<ChatDbContext>
{
    public ChatDbContext CreateDbContext(string[] args)
    {
        var options = new DbContextOptionsBuilder<ChatDbContext>()
            .UseNpgsql("Host=localhost;Port=5433;Database=chat_db;Username=postgres;Password=postgres")
            .Options;

        return new ChatDbContext(options);
    }
}
