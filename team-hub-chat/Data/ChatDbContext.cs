using Microsoft.EntityFrameworkCore;
using team_hub_chat.Models;

namespace team_hub_chat.Data;

public class ChatDbContext(DbContextOptions<ChatDbContext> options) : DbContext(options)
{
    public DbSet<Conversation> Conversations => Set<Conversation>();
    public DbSet<ConversationMember> ConversationMembers => Set<ConversationMember>();
    public DbSet<Message> Messages => Set<Message>();
    public DbSet<MessageAttachment> MessageAttachments => Set<MessageAttachment>();
    public DbSet<MessageReaction> MessageReactions => Set<MessageReaction>();
    public DbSet<MessageMention> MessageMentions => Set<MessageMention>();
    public DbSet<MessageRead> MessageReads => Set<MessageRead>();
    public DbSet<ConversationPin> ConversationPins => Set<ConversationPin>();
    public DbSet<ConversationSetting> ConversationSettings => Set<ConversationSetting>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Conversation>(e =>
        {
            e.ToTable("conversations");
            e.HasIndex(c => c.OrganizationId);
            e.HasIndex(c => new { c.OrganizationId, c.LastMessageAt });
            e.Property(c => c.Type).HasMaxLength(30).IsRequired();
            e.Property(c => c.Name).HasMaxLength(255);
        });

        modelBuilder.Entity<ConversationMember>(e =>
        {
            e.ToTable("conversation_members");
            e.HasKey(m => new { m.ConversationId, m.UserId });
            e.HasIndex(m => m.UserId);
            e.Property(m => m.Role).HasMaxLength(30).IsRequired();
            e.HasOne(m => m.Conversation)
                .WithMany(c => c.Members)
                .HasForeignKey(m => m.ConversationId)
                .OnDelete(DeleteBehavior.Cascade);
            e.HasOne(m => m.LastReadMessage)
                .WithMany()
                .HasForeignKey(m => m.LastReadMessageId)
                .OnDelete(DeleteBehavior.SetNull);
        });

        modelBuilder.Entity<Message>(e =>
        {
            e.ToTable("messages");
            e.HasIndex(m => new { m.ConversationId, m.CreatedAt });
            e.Property(m => m.Content).HasColumnType("text");
            e.Property(m => m.MessageType).HasMaxLength(30).IsRequired();
            e.HasOne(m => m.Conversation)
                .WithMany(c => c.Messages)
                .HasForeignKey(m => m.ConversationId)
                .OnDelete(DeleteBehavior.Cascade);
            e.HasOne(m => m.ReplyTo)
                .WithMany(m => m.Replies)
                .HasForeignKey(m => m.ReplyToMessageId)
                .OnDelete(DeleteBehavior.SetNull);
        });

        modelBuilder.Entity<MessageAttachment>(e =>
        {
            e.ToTable("message_attachments");
            e.Property(a => a.FileName).HasMaxLength(255).IsRequired();
            e.Property(a => a.ContentType).HasMaxLength(100).IsRequired();
            e.Property(a => a.StorageKey).HasMaxLength(1000).IsRequired();
            e.HasOne(a => a.Message)
                .WithMany(m => m.Attachments)
                .HasForeignKey(a => a.MessageId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<MessageReaction>(e =>
        {
            e.ToTable("message_reactions");
            e.HasKey(r => new { r.MessageId, r.UserId });
            e.Property(r => r.Reaction).HasMaxLength(50).IsRequired();
            e.HasOne(r => r.Message)
                .WithMany(m => m.Reactions)
                .HasForeignKey(r => r.MessageId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<MessageMention>(e =>
        {
            e.ToTable("message_mentions");
            e.HasKey(m => new { m.MessageId, m.UserId });
            e.HasOne(m => m.Message)
                .WithMany(x => x.Mentions)
                .HasForeignKey(m => m.MessageId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<MessageRead>(e =>
        {
            e.ToTable("message_reads");
            e.HasKey(r => new { r.MessageId, r.UserId });
            e.HasOne(r => r.Message)
                .WithMany(m => m.Reads)
                .HasForeignKey(r => r.MessageId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<ConversationPin>(e =>
        {
            e.ToTable("conversation_pins");
            e.HasKey(p => new { p.ConversationId, p.UserId });
            e.HasOne(p => p.Conversation)
                .WithMany(c => c.Pins)
                .HasForeignKey(p => p.ConversationId)
                .OnDelete(DeleteBehavior.Cascade);
            e.HasOne(p => p.Message)
                .WithMany(m => m.Pins)
                .HasForeignKey(p => p.MessageId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<ConversationSetting>(e =>
        {
            e.ToTable("conversation_settings");
            e.HasKey(s => new { s.ConversationId, s.UserId });
            e.HasOne(s => s.Conversation)
                .WithMany(c => c.Settings)
                .HasForeignKey(s => s.ConversationId)
                .OnDelete(DeleteBehavior.Cascade);
        });
    }
}
