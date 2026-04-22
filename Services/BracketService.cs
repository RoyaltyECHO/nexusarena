using Microsoft.EntityFrameworkCore;
using nexusarena.Data;
using nexusarena.Domain;

namespace nexusarena.Services;

public sealed class BracketService(
    NexusArenaDbContext dbContext,
    ProgressionService progressionService,
    NotificationService notificationService)
{
    public async Task GenerateSingleEliminationAsync(Tournament tournament, CancellationToken cancellationToken)
    {
        var registrations = await LoadBracketEligibleRegistrationsAsync(tournament.Id, cancellationToken);
        await GenerateEliminationBracketAsync(tournament.Id, registrations, cancellationToken);
    }

    public async Task GenerateGroupStageAsync(Tournament tournament, CancellationToken cancellationToken)
    {
        var registrations = await LoadBracketEligibleRegistrationsAsync(tournament.Id, cancellationToken);
        if (registrations.Count < 2)
        {
            throw new InvalidOperationException("Sao necessarios pelo menos 2 participantes confirmados para gerar grupos.");
        }

        var shuffled = registrations.OrderBy(_ => Guid.NewGuid()).ToList();
        for (var i = 0; i < shuffled.Count; i++)
        {
            shuffled[i].Seed = i + 1;
        }

        var groupCount = Math.Max(1, (int)Math.Ceiling(shuffled.Count / 4m));
        var groups = new List<TournamentGroup>();
        for (var i = 0; i < groupCount; i++)
        {
            var group = new TournamentGroup
            {
                TournamentId = tournament.Id,
                Order = i + 1,
                Name = $"Grupo {(char)('A' + i)}"
            };
            dbContext.TournamentGroups.Add(group);
            groups.Add(group);
        }

        await dbContext.SaveChangesAsync(cancellationToken);

        for (var i = 0; i < shuffled.Count; i++)
        {
            var group = groups[i % groups.Count];
            dbContext.TournamentGroupMembers.Add(new TournamentGroupMember
            {
                TournamentGroupId = group.Id,
                TournamentRegistrationId = shuffled[i].Id
            });
        }

        await dbContext.SaveChangesAsync(cancellationToken);

        foreach (var group in groups)
        {
            var members = await dbContext.TournamentGroupMembers
                .Where(x => x.TournamentGroupId == group.Id)
                .OrderBy(x => x.Id)
                .ToListAsync(cancellationToken);

            var sequence = 1;
            for (var i = 0; i < members.Count; i++)
            {
                for (var j = i + 1; j < members.Count; j++)
                {
                    dbContext.Matches.Add(new Match
                    {
                        TournamentId = tournament.Id,
                        TournamentGroupId = group.Id,
                        Stage = MatchStage.Group,
                        RoundNumber = i + 1,
                        Sequence = sequence++,
                        PlayerOneRegistrationId = members[i].TournamentRegistrationId,
                        PlayerTwoRegistrationId = members[j].TournamentRegistrationId
                    });
                }
            }
        }

        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task FinalizeConfirmedResultAsync(Match match, CancellationToken cancellationToken)
    {
        if (match.WinnerRegistrationId is null)
        {
            throw new InvalidOperationException("A partida precisa ter um vencedor para ser confirmada.");
        }

        var tournament = await dbContext.Tournaments.FirstAsync(x => x.Id == match.TournamentId, cancellationToken);
        var winnerRegistration = await dbContext.TournamentRegistrations
            .Include(x => x.User)
            .FirstAsync(x => x.Id == match.WinnerRegistrationId.Value, cancellationToken);

        winnerRegistration.Wins += 1;
        winnerRegistration.User.TotalWins += 1;
        await progressionService.AwardMatchWinXpAsync(winnerRegistration.User, tournament, match, cancellationToken);

        var loserId = match.PlayerOneRegistrationId == match.WinnerRegistrationId
            ? match.PlayerTwoRegistrationId
            : match.PlayerOneRegistrationId;

        if (loserId.HasValue)
        {
            var loserRegistration = await dbContext.TournamentRegistrations
                .Include(x => x.User)
                .FirstAsync(x => x.Id == loserId.Value, cancellationToken);

            loserRegistration.Losses += 1;
            loserRegistration.User.TotalLosses += 1;

            if (match.Stage == MatchStage.Bracket)
            {
                loserRegistration.Status = RegistrationStatus.Eliminated;
            }
        }

        if (match.Stage == MatchStage.Group && match.TournamentGroupId.HasValue)
        {
            await UpdateGroupStandingsAsync(match, cancellationToken);
        }

        if (match.NextMatchId.HasValue)
        {
            var nextMatch = await dbContext.Matches.FirstAsync(x => x.Id == match.NextMatchId, cancellationToken);
            if (match.NextMatchSlot == 1)
            {
                nextMatch.PlayerOneRegistrationId = match.WinnerRegistrationId;
            }
            else
            {
                nextMatch.PlayerTwoRegistrationId = match.WinnerRegistrationId;
            }
        }

        await dbContext.SaveChangesAsync(cancellationToken);
        await AutoAdvanceByesAsync(match.TournamentId, cancellationToken);
        await EnsureGroupKnockoutIfReadyAsync(match.TournamentId, cancellationToken);
        await TryCompleteTournamentAsync(match.TournamentId, cancellationToken);
    }

    public async Task EnsureGroupKnockoutIfReadyAsync(Guid tournamentId, CancellationToken cancellationToken)
    {
        var tournament = await dbContext.Tournaments.FirstAsync(x => x.Id == tournamentId, cancellationToken);
        if (tournament.Format != TournamentFormat.GroupStage)
        {
            return;
        }

        var hasBracketMatches = await dbContext.Matches.AnyAsync(x => x.TournamentId == tournamentId && x.Stage == MatchStage.Bracket, cancellationToken);
        if (hasBracketMatches)
        {
            return;
        }

        var pendingGroupMatches = await dbContext.Matches
            .AnyAsync(x => x.TournamentId == tournamentId && x.Stage == MatchStage.Group && x.Status != MatchStatus.Confirmed, cancellationToken);

        if (pendingGroupMatches)
        {
            return;
        }

        var groups = await dbContext.TournamentGroups
            .Include(x => x.Members)
            .Where(x => x.TournamentId == tournamentId)
            .OrderBy(x => x.Order)
            .ToListAsync(cancellationToken);

        var qualified = new List<TournamentRegistration>();
        foreach (var group in groups)
        {
            var topMembers = await dbContext.TournamentGroupMembers
                .Include(x => x.TournamentRegistration)
                .ThenInclude(x => x.User)
                .Where(x => x.TournamentGroupId == group.Id)
                .OrderByDescending(x => x.Points)
                .ThenByDescending(x => x.Wins)
                .ThenByDescending(x => x.ScoreFor - x.ScoreAgainst)
                .ThenByDescending(x => x.ScoreFor)
                .Take(group.Members.Count >= 4 ? 2 : 1)
                .ToListAsync(cancellationToken);

            qualified.AddRange(topMembers.Select(x => x.TournamentRegistration));
        }

        var distinctQualified = qualified
            .GroupBy(x => x.Id)
            .Select(x => x.First())
            .ToList();

        if (distinctQualified.Count < 2)
        {
            return;
        }

        await GenerateEliminationBracketAsync(tournamentId, distinctQualified, cancellationToken);
    }

    public async Task PromoteWaitlistAsync(Guid tournamentId, CancellationToken cancellationToken)
    {
        var tournament = await dbContext.Tournaments.FirstAsync(x => x.Id == tournamentId, cancellationToken);
        var currentActive = await dbContext.TournamentRegistrations
            .CountAsync(x => x.TournamentId == tournamentId &&
                             (x.Status == RegistrationStatus.Registered || x.Status == RegistrationStatus.CheckedIn), cancellationToken);

        if (currentActive >= tournament.MaxParticipants)
        {
            return;
        }

        var nextWaitlisted = await dbContext.TournamentRegistrations
            .Include(x => x.User)
            .Where(x => x.TournamentId == tournamentId && x.Status == RegistrationStatus.Waitlisted)
            .OrderBy(x => x.RegisteredAtUtc)
            .FirstOrDefaultAsync(cancellationToken);

        if (nextWaitlisted is null)
        {
            return;
        }

        nextWaitlisted.Status = RegistrationStatus.Registered;
        notificationService.Add(
            nextWaitlisted.UserId,
            tournamentId,
            NotificationType.WaitlistPromotion,
            $"Uma vaga foi liberada no torneio {tournament.Name}. Sua inscricao foi confirmada.");
    }

    public async Task CloseCheckInAsync(Guid tournamentId, CancellationToken cancellationToken)
    {
        var tournament = await dbContext.Tournaments
            .Include(x => x.Registrations)
            .FirstAsync(x => x.Id == tournamentId, cancellationToken);

        foreach (var registration in tournament.Registrations.Where(x => x.Status == RegistrationStatus.Registered))
        {
            registration.Status = RegistrationStatus.Withdrawn;
        }

        tournament.Status = TournamentStatus.CheckInClosed;
        await dbContext.SaveChangesAsync(cancellationToken);
        await PromoteWaitlistAsync(tournamentId, cancellationToken);
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    private async Task<List<TournamentRegistration>> LoadBracketEligibleRegistrationsAsync(Guid tournamentId, CancellationToken cancellationToken)
    {
        var registrations = await dbContext.TournamentRegistrations
            .Where(x => x.TournamentId == tournamentId &&
                        (x.Status == RegistrationStatus.CheckedIn || x.Status == RegistrationStatus.Registered))
            .Include(x => x.User)
            .OrderBy(x => x.RegisteredAtUtc)
            .ToListAsync(cancellationToken);

        if (registrations.Count < 2)
        {
            throw new InvalidOperationException("Sao necessarios pelo menos 2 participantes ativos para gerar a chave.");
        }

        return registrations;
    }

    private async Task GenerateEliminationBracketAsync(Guid tournamentId, IReadOnlyCollection<TournamentRegistration> registrations, CancellationToken cancellationToken)
    {
        if (registrations.Count < 2)
        {
            throw new InvalidOperationException("Sao necessarios pelo menos 2 participantes para gerar eliminacao.");
        }

        var shuffled = registrations.OrderBy(_ => Guid.NewGuid()).ToList();
        for (var i = 0; i < shuffled.Count; i++)
        {
            shuffled[i].Seed = i + 1;
        }

        var bracketSize = 1;
        while (bracketSize < shuffled.Count)
        {
            bracketSize *= 2;
        }

        var rounds = (int)Math.Log2(bracketSize);
        var roundsByMatches = new List<List<Match>>();

        for (var round = 1; round <= rounds; round++)
        {
            var matchesInRound = bracketSize / (int)Math.Pow(2, round);
            var roundMatches = new List<Match>();
            for (var sequence = 1; sequence <= matchesInRound; sequence++)
            {
                var match = new Match
                {
                    TournamentId = tournamentId,
                    Stage = MatchStage.Bracket,
                    RoundNumber = round,
                    Sequence = sequence
                };

                dbContext.Matches.Add(match);
                roundMatches.Add(match);
            }

            roundsByMatches.Add(roundMatches);
        }

        for (var round = 0; round < roundsByMatches.Count - 1; round++)
        {
            for (var index = 0; index < roundsByMatches[round].Count; index++)
            {
                var nextMatch = roundsByMatches[round + 1][index / 2];
                roundsByMatches[round][index].NextMatchId = nextMatch.Id;
                roundsByMatches[round][index].NextMatchSlot = (index % 2) + 1;
            }
        }

        var padded = shuffled.Cast<TournamentRegistration?>().ToList();
        while (padded.Count < bracketSize)
        {
            padded.Add(null);
        }

        for (var index = 0; index < roundsByMatches[0].Count; index++)
        {
            var match = roundsByMatches[0][index];
            match.PlayerOneRegistrationId = padded[index * 2]?.Id;
            match.PlayerTwoRegistrationId = padded[index * 2 + 1]?.Id;
        }

        await dbContext.SaveChangesAsync(cancellationToken);
        await AutoAdvanceByesAsync(tournamentId, cancellationToken);
    }

    private async Task AutoAdvanceByesAsync(Guid tournamentId, CancellationToken cancellationToken)
    {
        while (true)
        {
            var byeMatch = await dbContext.Matches
                .Where(x => x.TournamentId == tournamentId &&
                            x.Stage == MatchStage.Bracket &&
                            x.Status == MatchStatus.Pending &&
                            ((x.PlayerOneRegistrationId == null) != (x.PlayerTwoRegistrationId == null)))
                .OrderBy(x => x.RoundNumber)
                .ThenBy(x => x.Sequence)
                .FirstOrDefaultAsync(cancellationToken);

            if (byeMatch is null)
            {
                break;
            }

            byeMatch.WinnerRegistrationId = byeMatch.PlayerOneRegistrationId ?? byeMatch.PlayerTwoRegistrationId;
            byeMatch.PlayerOneScore = byeMatch.PlayerOneRegistrationId.HasValue ? 1 : 0;
            byeMatch.PlayerTwoScore = byeMatch.PlayerTwoRegistrationId.HasValue ? 1 : 0;
            byeMatch.Status = MatchStatus.Confirmed;
            byeMatch.ConfirmedAtUtc = DateTime.UtcNow;

            if (byeMatch.NextMatchId.HasValue)
            {
                var nextMatch = await dbContext.Matches.FirstAsync(x => x.Id == byeMatch.NextMatchId.Value, cancellationToken);
                if (byeMatch.NextMatchSlot == 1)
                {
                    nextMatch.PlayerOneRegistrationId = byeMatch.WinnerRegistrationId;
                }
                else
                {
                    nextMatch.PlayerTwoRegistrationId = byeMatch.WinnerRegistrationId;
                }
            }

            await dbContext.SaveChangesAsync(cancellationToken);
        }
    }

    private async Task UpdateGroupStandingsAsync(Match match, CancellationToken cancellationToken)
    {
        var members = await dbContext.TournamentGroupMembers
            .Where(x => x.TournamentGroupId == match.TournamentGroupId &&
                        (x.TournamentRegistrationId == match.PlayerOneRegistrationId || x.TournamentRegistrationId == match.PlayerTwoRegistrationId))
            .ToListAsync(cancellationToken);

        var playerOne = members.First(x => x.TournamentRegistrationId == match.PlayerOneRegistrationId);
        var playerTwo = members.First(x => x.TournamentRegistrationId == match.PlayerTwoRegistrationId);

        playerOne.ScoreFor += match.PlayerOneScore ?? 0;
        playerOne.ScoreAgainst += match.PlayerTwoScore ?? 0;
        playerTwo.ScoreFor += match.PlayerTwoScore ?? 0;
        playerTwo.ScoreAgainst += match.PlayerOneScore ?? 0;

        if (match.WinnerRegistrationId == match.PlayerOneRegistrationId)
        {
            playerOne.Wins += 1;
            playerOne.Points += 3;
            playerTwo.Losses += 1;
        }
        else
        {
            playerTwo.Wins += 1;
            playerTwo.Points += 3;
            playerOne.Losses += 1;
        }
    }

    private async Task TryCompleteTournamentAsync(Guid tournamentId, CancellationToken cancellationToken)
    {
        var tournament = await dbContext.Tournaments.FirstAsync(x => x.Id == tournamentId, cancellationToken);
        if (tournament.Status != TournamentStatus.InProgress)
        {
            return;
        }

        var pendingMatches = await dbContext.Matches
            .AnyAsync(x => x.TournamentId == tournamentId && x.Status != MatchStatus.Confirmed, cancellationToken);

        if (pendingMatches)
        {
            return;
        }

        TournamentRegistration championRegistration;
        var hasBracketMatches = await dbContext.Matches.AnyAsync(x => x.TournamentId == tournamentId && x.Stage == MatchStage.Bracket, cancellationToken);

        if (hasBracketMatches)
        {
            var finalMatch = await dbContext.Matches
                .Where(x => x.TournamentId == tournamentId && x.Stage == MatchStage.Bracket)
                .OrderByDescending(x => x.RoundNumber)
                .ThenByDescending(x => x.Sequence)
                .FirstAsync(cancellationToken);

            championRegistration = await dbContext.TournamentRegistrations
                .Include(x => x.User)
                .FirstAsync(x => x.Id == finalMatch.WinnerRegistrationId, cancellationToken);
        }
        else
        {
            var bestGroupMember = await dbContext.TournamentGroupMembers
                .Include(x => x.TournamentRegistration)
                .ThenInclude(x => x.User)
                .Where(x => x.TournamentRegistration.TournamentId == tournamentId)
                .OrderByDescending(x => x.Points)
                .ThenByDescending(x => x.Wins)
                .ThenByDescending(x => x.ScoreFor - x.ScoreAgainst)
                .ThenByDescending(x => x.ScoreFor)
                .FirstAsync(cancellationToken);

            championRegistration = bestGroupMember.TournamentRegistration;
        }

        var registrations = await dbContext.TournamentRegistrations
            .Include(x => x.User)
            .Where(x => x.TournamentId == tournamentId)
            .ToListAsync(cancellationToken);

        foreach (var registration in registrations)
        {
            if (registration.Status != RegistrationStatus.Withdrawn)
            {
                await progressionService.AwardCompletionXpAsync(registration.User, tournament, cancellationToken);
                registration.User.TournamentsParticipated += 1;
            }

            if (registration.Id == championRegistration.Id)
            {
                registration.Status = RegistrationStatus.Champion;
                registration.FinalPlacement = 1;
                registration.User.TotalTitles += 1;
                registration.User.ConsecutiveTitles += 1;
                await progressionService.AwardChampionshipXpAsync(registration.User, tournament, cancellationToken);
            }
            else if (registration.Status != RegistrationStatus.Withdrawn)
            {
                registration.User.ConsecutiveTitles = 0;
            }
        }

        tournament.Status = TournamentStatus.Completed;
        tournament.CompletedAtUtc = DateTime.UtcNow;

        foreach (var registration in registrations)
        {
            await progressionService.UnlockAchievementsAsync(registration.UserId, cancellationToken);
        }

        await dbContext.SaveChangesAsync(cancellationToken);
    }
}
