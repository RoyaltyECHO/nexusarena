using nexusarena.Data;
using nexusarena.Domain;

namespace nexusarena.Services;

public sealed class NotificationService(NexusArenaDbContext dbContext)
{
    public void Add(Guid userId, Guid? tournamentId, NotificationType type, string message)
    {
        dbContext.TournamentNotifications.Add(new TournamentNotification
        {
            UserId = userId,
            TournamentId = tournamentId,
            Type = type,
            Message = message
        });
    }
}
