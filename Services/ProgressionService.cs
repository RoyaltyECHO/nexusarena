using Microsoft.EntityFrameworkCore;
using nexusarena.Data;
using nexusarena.Domain;

namespace nexusarena.Services;

public sealed class ProgressionService(NexusArenaDbContext dbContext)
{
    public static PlayerLevel GetLevel(int totalXp) => totalXp switch
    {
        < 500 => PlayerLevel.Ferro,
        < 1500 => PlayerLevel.Bronze,
        < 3000 => PlayerLevel.Prata,
        < 6000 => PlayerLevel.Ouro,
        < 10000 => PlayerLevel.Diamante,
        _ => PlayerLevel.Nexus
    };

    public async Task AwardSignupXpAsync(User user, Tournament tournament, CancellationToken cancellationToken)
        => await AddXpAsync(user, 10, XpActionType.TournamentSignup, "Inscricao em torneio", tournament, null, cancellationToken);

    public async Task AwardMatchWinXpAsync(User user, Tournament tournament, Match match, CancellationToken cancellationToken)
    {
        var isGroup = match.Stage == MatchStage.Group;
        await AddXpAsync(
            user,
            isGroup ? 25 : 40,
            isGroup ? XpActionType.GroupStageWin : XpActionType.EliminationWin,
            isGroup ? "Vitoria na fase de grupos" : "Vitoria em partida eliminatoria",
            tournament,
            match,
            cancellationToken);
    }

    public async Task AwardChampionshipXpAsync(User user, Tournament tournament, CancellationToken cancellationToken)
        => await AddXpAsync(user, 150, XpActionType.Championship, "Conquista de campeonato", tournament, null, cancellationToken);

    public async Task AwardCompletionXpAsync(User user, Tournament tournament, CancellationToken cancellationToken)
        => await AddXpAsync(user, 15, XpActionType.TournamentCompletion, "Participacao sem desistir", tournament, null, cancellationToken);

    public async Task AwardPositiveFeedbackXpAsync(User user, Tournament tournament, CancellationToken cancellationToken)
        => await AddXpAsync(user, 5, XpActionType.PositiveFeedback, "Avaliacao positiva recebida", tournament, null, cancellationToken);

    public async Task UnlockAchievementsAsync(Guid userId, CancellationToken cancellationToken)
    {
        var user = await dbContext.Users
            .Include(x => x.Achievements)
            .Include(x => x.Registrations)
            .FirstAsync(x => x.Id == userId, cancellationToken);

        var maxRatedTournaments = await dbContext.PlayerFeedback
            .Where(x => x.ToUserId == userId && x.Rating == 5)
            .Select(x => x.TournamentId)
            .Distinct()
            .CountAsync(cancellationToken);

        var achievements = new List<(AchievementCode Code, bool Condition, string Name, string Description)>
        {
            (AchievementCode.Estreante, user.TournamentsParticipated >= 1, "Estreante", "Participe do seu primeiro torneio"),
            (AchievementCode.PrimeiraVitoria, user.TotalWins >= 1, "Primeira Vitoria", "Venca sua primeira partida"),
            (AchievementCode.Veterano, user.TournamentsParticipated >= 10, "Veterano", "Participe de 10 torneios"),
            (AchievementCode.Lenda, GetLevel(user.TotalXp) == PlayerLevel.Nexus, "Lenda", "Alcance o nivel NEXUS"),
            (AchievementCode.HatTrick, user.ConsecutiveTitles >= 3, "Hat-Trick", "Venca 3 campeonatos seguidos"),
            (AchievementCode.FairPlay, maxRatedTournaments >= 5, "Fair Play", "Avaliacao maxima em 5 torneios"),
            (AchievementCode.Dominador, user.TotalWins >= 50, "Dominador", "Venca 50 partidas no total")
        };

        var hasInvicto = await dbContext.TournamentRegistrations
            .AnyAsync(x => x.UserId == userId && x.Status == RegistrationStatus.Champion && x.Losses == 0, cancellationToken);

        achievements.Add((AchievementCode.Invicto, hasInvicto, "Invicto", "Conquiste um campeonato sem perder"));

        foreach (var achievement in achievements.Where(x => x.Condition))
        {
            if (user.Achievements.All(existing => existing.Code != achievement.Code))
            {
                dbContext.UserAchievements.Add(new UserAchievement
                {
                    UserId = userId,
                    Code = achievement.Code,
                    Name = achievement.Name,
                    Description = achievement.Description
                });
            }
        }
    }

    private async Task AddXpAsync(
        User user,
        int amount,
        XpActionType actionType,
        string description,
        Tournament tournament,
        Match? match,
        CancellationToken cancellationToken)
    {
        user.TotalXp += amount;

        dbContext.XpLedgerEntries.Add(new XpLedgerEntry
        {
            UserId = user.Id,
            TournamentId = tournament.Id,
            MatchId = match?.Id,
            Amount = amount,
            ActionType = actionType,
            Description = description,
            GameTitle = tournament.GameTitle
        });

        await UnlockAchievementsAsync(user.Id, cancellationToken);
    }
}
