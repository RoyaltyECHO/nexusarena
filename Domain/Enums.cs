namespace nexusarena.Domain;

public enum UserRole
{
    Player = 1,
    Organizer = 2,
    Admin = 3
}

public enum TournamentFormat
{
    SingleElimination = 1,
    GroupStage = 2
}

public enum TournamentVisibility
{
    Public = 1,
    Private = 2
}

public enum TournamentStatus
{
    Draft = 1,
    RegistrationOpen = 2,
    RegistrationClosed = 3,
    CheckInOpen = 4,
    InProgress = 5,
    Completed = 6,
    Cancelled = 7,
    CheckInClosed = 8
}

public enum RegistrationStatus
{
    Registered = 1,
    Waitlisted = 2,
    CheckedIn = 3,
    Eliminated = 4,
    Champion = 5,
    Withdrawn = 6
}

public enum MatchStage
{
    Bracket = 1,
    Group = 2
}

public enum MatchStatus
{
    Pending = 1,
    PendingConfirmation = 2,
    Confirmed = 3,
    Disputed = 4
}

public enum DisputeStatus
{
    Open = 1,
    ResolvedAccepted = 2,
    ResolvedRejected = 3
}

public enum NotificationType
{
    WaitlistPromotion = 1,
    TournamentUpdate = 2,
    MatchDispute = 3,
    General = 4
}

public enum XpActionType
{
    TournamentSignup = 1,
    GroupStageWin = 2,
    EliminationWin = 3,
    Championship = 4,
    TournamentCompletion = 5,
    PositiveFeedback = 6
}

public enum AchievementCode
{
    Estreante = 1,
    PrimeiraVitoria = 2,
    Veterano = 3,
    Invicto = 4,
    Lenda = 5,
    HatTrick = 6,
    FairPlay = 7,
    Dominador = 8
}

public enum PlayerLevel
{
    Ferro = 1,
    Bronze = 2,
    Prata = 3,
    Ouro = 4,
    Diamante = 5,
    Nexus = 6
}
