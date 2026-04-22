using Microsoft.EntityFrameworkCore;
using nexusarena.Domain;

namespace nexusarena.Data;

public sealed class NexusArenaDbContext(DbContextOptions<NexusArenaDbContext> options) : DbContext(options)
{
    public DbSet<User> Users => Set<User>();
    public DbSet<GameCatalogItem> GameCatalogItems => Set<GameCatalogItem>();
    public DbSet<Tournament> Tournaments => Set<Tournament>();
    public DbSet<TournamentRegistration> TournamentRegistrations => Set<TournamentRegistration>();
    public DbSet<TournamentGroup> TournamentGroups => Set<TournamentGroup>();
    public DbSet<TournamentGroupMember> TournamentGroupMembers => Set<TournamentGroupMember>();
    public DbSet<Match> Matches => Set<Match>();
    public DbSet<MatchResultDispute> MatchResultDisputes => Set<MatchResultDispute>();
    public DbSet<TournamentNotification> TournamentNotifications => Set<TournamentNotification>();
    public DbSet<XpLedgerEntry> XpLedgerEntries => Set<XpLedgerEntry>();
    public DbSet<UserAchievement> UserAchievements => Set<UserAchievement>();
    public DbSet<PlayerFeedback> PlayerFeedback => Set<PlayerFeedback>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<User>(entity =>
        {
            entity.HasIndex(x => x.Email).IsUnique();
            entity.HasIndex(x => x.Nickname).IsUnique();
        });

        modelBuilder.Entity<GameCatalogItem>(entity =>
        {
            entity.HasIndex(x => x.Title).IsUnique();
            entity.HasIndex(x => x.Slug).IsUnique();
        });

        modelBuilder.Entity<Tournament>(entity =>
        {
            entity.HasOne(x => x.Organizer)
                .WithMany(x => x.OrganizedTournaments)
                .HasForeignKey(x => x.OrganizerId)
                .OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(x => x.GameCatalogItem)
                .WithMany(x => x.Tournaments)
                .HasForeignKey(x => x.GameCatalogItemId)
                .OnDelete(DeleteBehavior.SetNull);
        });

        modelBuilder.Entity<TournamentRegistration>(entity =>
        {
            entity.HasIndex(x => new { x.TournamentId, x.UserId }).IsUnique();
            entity.HasOne(x => x.Tournament)
                .WithMany(x => x.Registrations)
                .HasForeignKey(x => x.TournamentId);
            entity.HasOne(x => x.User)
                .WithMany(x => x.Registrations)
                .HasForeignKey(x => x.UserId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<TournamentGroup>(entity =>
        {
            entity.HasIndex(x => new { x.TournamentId, x.Order }).IsUnique();
            entity.HasOne(x => x.Tournament)
                .WithMany(x => x.Groups)
                .HasForeignKey(x => x.TournamentId);
        });

        modelBuilder.Entity<TournamentGroupMember>(entity =>
        {
            entity.HasIndex(x => new { x.TournamentGroupId, x.TournamentRegistrationId }).IsUnique();
            entity.HasOne(x => x.TournamentGroup)
                .WithMany(x => x.Members)
                .HasForeignKey(x => x.TournamentGroupId);
            entity.HasOne(x => x.TournamentRegistration)
                .WithMany(x => x.GroupMemberships)
                .HasForeignKey(x => x.TournamentRegistrationId)
                .OnDelete(DeleteBehavior.NoAction);
        });

        modelBuilder.Entity<Match>(entity =>
        {
            entity.HasIndex(x => new { x.TournamentId, x.Stage, x.RoundNumber, x.Sequence }).IsUnique();
            entity.HasOne(x => x.Tournament)
                .WithMany(x => x.Matches)
                .HasForeignKey(x => x.TournamentId);
            entity.HasOne(x => x.TournamentGroup)
                .WithMany(x => x.Matches)
                .HasForeignKey(x => x.TournamentGroupId)
                .OnDelete(DeleteBehavior.NoAction);
            entity.HasOne(x => x.PlayerOneRegistration)
                .WithMany(x => x.PlayerOneMatches)
                .HasForeignKey(x => x.PlayerOneRegistrationId)
                .OnDelete(DeleteBehavior.NoAction);
            entity.HasOne(x => x.PlayerTwoRegistration)
                .WithMany(x => x.PlayerTwoMatches)
                .HasForeignKey(x => x.PlayerTwoRegistrationId)
                .OnDelete(DeleteBehavior.NoAction);
            entity.HasOne(x => x.WinnerRegistration)
                .WithMany(x => x.WonMatches)
                .HasForeignKey(x => x.WinnerRegistrationId)
                .OnDelete(DeleteBehavior.NoAction);
        });

        modelBuilder.Entity<MatchResultDispute>(entity =>
        {
            entity.HasOne(x => x.Match)
                .WithMany(x => x.Disputes)
                .HasForeignKey(x => x.MatchId);
            entity.HasOne(x => x.TournamentRegistration)
                .WithMany(x => x.ResultDisputes)
                .HasForeignKey(x => x.TournamentRegistrationId)
                .OnDelete(DeleteBehavior.NoAction);
        });

        modelBuilder.Entity<TournamentNotification>(entity =>
        {
            entity.HasOne(x => x.User)
                .WithMany(x => x.Notifications)
                .HasForeignKey(x => x.UserId);
            entity.HasOne(x => x.Tournament)
                .WithMany(x => x.Notifications)
                .HasForeignKey(x => x.TournamentId)
                .OnDelete(DeleteBehavior.SetNull);
        });

        modelBuilder.Entity<XpLedgerEntry>(entity =>
        {
            entity.HasOne(x => x.User)
                .WithMany(x => x.XpLedgerEntries)
                .HasForeignKey(x => x.UserId);
        });

        modelBuilder.Entity<UserAchievement>(entity =>
        {
            entity.HasIndex(x => new { x.UserId, x.Code }).IsUnique();
            entity.HasOne(x => x.User)
                .WithMany(x => x.Achievements)
                .HasForeignKey(x => x.UserId);
        });

        modelBuilder.Entity<PlayerFeedback>(entity =>
        {
            entity.HasIndex(x => new { x.TournamentId, x.FromUserId, x.ToUserId }).IsUnique();
            entity.HasOne(x => x.Tournament)
                .WithMany()
                .HasForeignKey(x => x.TournamentId);
            entity.HasOne(x => x.FromUser)
                .WithMany(x => x.SentFeedback)
                .HasForeignKey(x => x.FromUserId)
                .OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(x => x.ToUser)
                .WithMany(x => x.ReceivedFeedback)
                .HasForeignKey(x => x.ToUserId)
                .OnDelete(DeleteBehavior.Restrict);
        });
    }
}
