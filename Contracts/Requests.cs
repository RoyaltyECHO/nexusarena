using System.ComponentModel.DataAnnotations;
using nexusarena.Domain;

namespace nexusarena.Contracts;

public sealed record RegisterRequest(
    [property: Required, MaxLength(40)] string Nickname,
    [property: Required, EmailAddress, MaxLength(120)] string Email,
    [property: Required, MinLength(8)] string Password,
    UserRole Role = UserRole.Player);

public sealed record LoginRequest(
    [property: Required, EmailAddress] string Email,
    [property: Required] string Password);

public sealed record CreateGameCatalogItemRequest(
    [property: Required, MaxLength(80)] string Title);

public sealed record CreateTournamentRequest(
    [property: Required, MaxLength(120)] string Name,
    [property: Required, MaxLength(80)] string GameTitle,
    Guid? GameCatalogItemId,
    TournamentFormat Format,
    TournamentVisibility Visibility,
    [property: Range(2, 512)] int MaxParticipants,
    DateTime StartDateUtc,
    DateTime? RegistrationClosesAtUtc,
    DateTime? CheckInOpensAtUtc,
    DateTime? CheckInClosesAtUtc,
    [property: MaxLength(4000)] string Rules);

public sealed record UpdateTournamentRequest(
    [property: Required, MaxLength(120)] string Name,
    [property: Required, MaxLength(80)] string GameTitle,
    Guid? GameCatalogItemId,
    TournamentVisibility Visibility,
    [property: Range(2, 512)] int MaxParticipants,
    DateTime StartDateUtc,
    DateTime? RegistrationClosesAtUtc,
    DateTime? CheckInOpensAtUtc,
    DateTime? CheckInClosesAtUtc,
    [property: MaxLength(4000)] string Rules);

public sealed record ReportMatchResultRequest(
    [property: Range(0, 999)] int PlayerOneScore,
    [property: Range(0, 999)] int PlayerTwoScore);

public sealed record RejectMatchResultRequest(
    [property: Required, MaxLength(400)] string Reason);

public sealed record ResolveMatchDisputeRequest(
    bool AcceptResult,
    [property: MaxLength(400)] string? ResolutionNote);

public sealed record SubmitFeedbackRequest(
    Guid ToUserId,
    [property: Range(1, 5)] int Rating,
    string? Comment);
