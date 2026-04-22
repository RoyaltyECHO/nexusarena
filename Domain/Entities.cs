using System.ComponentModel.DataAnnotations;

namespace nexusarena.Domain;

public sealed class User
{
    public Guid Id { get; set; } = Guid.NewGuid();
    [MaxLength(40)]
    public string Nickname { get; set; } = string.Empty;
    [MaxLength(120)]
    public string Email { get; set; } = string.Empty;
    [MaxLength(512)]
    public string PasswordHash { get; set; } = string.Empty;
    public UserRole Role { get; set; } = UserRole.Player;
    public int TotalXp { get; set; }
    public int TotalWins { get; set; }
    public int TotalLosses { get; set; }
    public int TotalTitles { get; set; }
    public int TournamentsParticipated { get; set; }
    public int PositiveRatingsReceived { get; set; }
    public int ConsecutiveTitles { get; set; }
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;

    public ICollection<Tournament> OrganizedTournaments { get; set; } = new List<Tournament>();
    public ICollection<TournamentRegistration> Registrations { get; set; } = new List<TournamentRegistration>();
    public ICollection<XpLedgerEntry> XpLedgerEntries { get; set; } = new List<XpLedgerEntry>();
    public ICollection<UserAchievement> Achievements { get; set; } = new List<UserAchievement>();
    public ICollection<PlayerFeedback> SentFeedback { get; set; } = new List<PlayerFeedback>();
    public ICollection<PlayerFeedback> ReceivedFeedback { get; set; } = new List<PlayerFeedback>();
}

public sealed class Tournament
{
    public Guid Id { get; set; } = Guid.NewGuid();
    [MaxLength(120)]
    public string Name { get; set; } = string.Empty;
    [MaxLength(80)]
    public string GameTitle { get; set; } = string.Empty;
    public TournamentFormat Format { get; set; }
    public TournamentVisibility Visibility { get; set; } = TournamentVisibility.Public;
    public TournamentStatus Status { get; set; } = TournamentStatus.RegistrationOpen;
    public int MaxParticipants { get; set; }
    public DateTime StartDateUtc { get; set; }
    public DateTime? RegistrationClosesAtUtc { get; set; }
    public DateTime? CheckInOpensAtUtc { get; set; }
    public DateTime? CheckInClosesAtUtc { get; set; }
    [MaxLength(4000)]
    public string Rules { get; set; } = string.Empty;
    public Guid OrganizerId { get; set; }
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    public DateTime? CompletedAtUtc { get; set; }

    public User Organizer { get; set; } = null!;
    public ICollection<TournamentRegistration> Registrations { get; set; } = new List<TournamentRegistration>();
    public ICollection<TournamentGroup> Groups { get; set; } = new List<TournamentGroup>();
    public ICollection<Match> Matches { get; set; } = new List<Match>();
}

public sealed class TournamentRegistration
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid TournamentId { get; set; }
    public Guid UserId { get; set; }
    public RegistrationStatus Status { get; set; } = RegistrationStatus.Registered;
    public DateTime RegisteredAtUtc { get; set; } = DateTime.UtcNow;
    public DateTime? CheckedInAtUtc { get; set; }
    public int? Seed { get; set; }
    public int Wins { get; set; }
    public int Losses { get; set; }
    public int? FinalPlacement { get; set; }

    public Tournament Tournament { get; set; } = null!;
    public User User { get; set; } = null!;
    public ICollection<TournamentGroupMember> GroupMemberships { get; set; } = new List<TournamentGroupMember>();
}

public sealed class TournamentGroup
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid TournamentId { get; set; }
    [MaxLength(40)]
    public string Name { get; set; } = string.Empty;
    public int Order { get; set; }

    public Tournament Tournament { get; set; } = null!;
    public ICollection<TournamentGroupMember> Members { get; set; } = new List<TournamentGroupMember>();
    public ICollection<Match> Matches { get; set; } = new List<Match>();
}

public sealed class TournamentGroupMember
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid TournamentGroupId { get; set; }
    public Guid TournamentRegistrationId { get; set; }
    public int Points { get; set; }
    public int Wins { get; set; }
    public int Losses { get; set; }
    public int ScoreFor { get; set; }
    public int ScoreAgainst { get; set; }

    public TournamentGroup TournamentGroup { get; set; } = null!;
    public TournamentRegistration TournamentRegistration { get; set; } = null!;
}

public sealed class Match
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid TournamentId { get; set; }
    public Guid? TournamentGroupId { get; set; }
    public MatchStage Stage { get; set; }
    public MatchStatus Status { get; set; } = MatchStatus.Pending;
    public int RoundNumber { get; set; }
    public int Sequence { get; set; }
    public Guid? PlayerOneRegistrationId { get; set; }
    public Guid? PlayerTwoRegistrationId { get; set; }
    public int? PlayerOneScore { get; set; }
    public int? PlayerTwoScore { get; set; }
    public Guid? WinnerRegistrationId { get; set; }
    public Guid? NextMatchId { get; set; }
    public int? NextMatchSlot { get; set; }
    public DateTime? ResultReportedAtUtc { get; set; }
    public DateTime? PlayerOneConfirmedAtUtc { get; set; }
    public DateTime? PlayerTwoConfirmedAtUtc { get; set; }
    public DateTime? ConfirmedAtUtc { get; set; }

    public Tournament Tournament { get; set; } = null!;
    public TournamentGroup? TournamentGroup { get; set; }
}

public sealed class XpLedgerEntry
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid UserId { get; set; }
    public Guid? TournamentId { get; set; }
    public Guid? MatchId { get; set; }
    public XpActionType ActionType { get; set; }
    public int Amount { get; set; }
    [MaxLength(240)]
    public string Description { get; set; } = string.Empty;
    [MaxLength(80)]
    public string? GameTitle { get; set; }
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;

    public User User { get; set; } = null!;
}

public sealed class UserAchievement
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid UserId { get; set; }
    public AchievementCode Code { get; set; }
    [MaxLength(80)]
    public string Name { get; set; } = string.Empty;
    [MaxLength(240)]
    public string Description { get; set; } = string.Empty;
    public DateTime UnlockedAtUtc { get; set; } = DateTime.UtcNow;

    public User User { get; set; } = null!;
}

public sealed class PlayerFeedback
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid TournamentId { get; set; }
    public Guid FromUserId { get; set; }
    public Guid ToUserId { get; set; }
    [Range(1, 5)]
    public int Rating { get; set; }
    [MaxLength(400)]
    public string? Comment { get; set; }
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;

    public Tournament Tournament { get; set; } = null!;
    public User FromUser { get; set; } = null!;
    public User ToUser { get; set; } = null!;
}
