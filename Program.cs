using System.ComponentModel.DataAnnotations;
using System.Security.Claims;
using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi;
using nexusarena.Contracts;
using nexusarena.Data;
using nexusarena.Domain;
using nexusarena.Security;
using nexusarena.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddOpenApi();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "Nexus Arena API",
        Version = "v1",
        Description = "API para autenticacao, torneios, inscricoes, partidas, XP, conquistas e ranking."
    });

    var securityScheme = new OpenApiSecurityScheme
    {
        Name = "Authorization",
        Description = "Informe o token JWT no formato: Bearer {seu_token}",
        In = ParameterLocation.Header,
        Type = SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT"
    };

    options.AddSecurityDefinition(JwtBearerDefaults.AuthenticationScheme, securityScheme);
    options.AddSecurityRequirement(_ => new OpenApiSecurityRequirement
    {
        [new OpenApiSecuritySchemeReference(JwtBearerDefaults.AuthenticationScheme, null, null)] = new List<string>()
    });
});
builder.Services.AddAuthorization();
builder.Services.AddScoped<PasswordService>();
builder.Services.AddScoped<JwtTokenService>();
builder.Services.AddScoped<ProgressionService>();
builder.Services.AddScoped<BracketService>();
builder.Services.Configure<JwtOptions>(builder.Configuration.GetSection(JwtOptions.SectionName));

var jwtOptions = builder.Configuration.GetSection(JwtOptions.SectionName).Get<JwtOptions>() ?? new JwtOptions();
builder.Services
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateIssuerSigningKey = true,
            ValidateLifetime = true,
            ValidIssuer = jwtOptions.Issuer,
            ValidAudience = jwtOptions.Audience,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtOptions.SigningKey))
        };
    });

var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
                       ?? "Server=DESKTOP-51IULJL\\SQLEXPRESS;Database=NexusArena;User Id=nexusarena_app;Password=SuaSenhaForte123!;Encrypt=False;TrustServerCertificate=True;MultipleActiveResultSets=true";
builder.Services.AddDbContext<NexusArenaDbContext>(options =>
    options.UseSqlServer(connectionString, sqlOptions => sqlOptions.EnableRetryOnFailure()));

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.UseSwagger();
    app.UseSwaggerUI(options =>
    {
        options.SwaggerEndpoint("/swagger/v1/swagger.json", "Nexus Arena API v1");
        options.RoutePrefix = "swagger";
    });
}

app.UseAuthentication();
app.UseAuthorization();

var auth = app.MapGroup("/auth");
auth.MapPost("/register", async Task<IResult> (
    RegisterRequest request,
    NexusArenaDbContext dbContext,
    PasswordService passwordService,
    JwtTokenService jwtTokenService,
    CancellationToken cancellationToken) =>
{
    var validation = Validate(request);
    if (validation is not null)
    {
        return TypedResults.ValidationProblem(validation);
    }

    if (await dbContext.Users.AnyAsync(x => x.Email == request.Email || x.Nickname == request.Nickname, cancellationToken))
    {
        return TypedResults.ValidationProblem(new Dictionary<string, string[]>
        {
            ["identity"] = ["Email ou nickname ja estao em uso."]
        });
    }

    var user = new User
    {
        Nickname = request.Nickname.Trim(),
        Email = request.Email.Trim().ToLowerInvariant(),
        PasswordHash = passwordService.Hash(request.Password),
        Role = request.Role
    };

    dbContext.Users.Add(user);
    await dbContext.SaveChangesAsync(cancellationToken);

    return TypedResults.Ok(new
    {
        token = jwtTokenService.Create(user),
        user = ToUserResponse(user)
    });
});

auth.MapPost("/login", async Task<IResult> (
    LoginRequest request,
    NexusArenaDbContext dbContext,
    PasswordService passwordService,
    JwtTokenService jwtTokenService,
    CancellationToken cancellationToken) =>
{
    var validation = Validate(request);
    if (validation is not null)
    {
        return TypedResults.ValidationProblem(validation);
    }

    var email = request.Email.Trim().ToLowerInvariant();
    var user = await dbContext.Users.FirstOrDefaultAsync(x => x.Email == email, cancellationToken);
    if (user is null || !passwordService.Verify(request.Password, user.PasswordHash))
    {
        return TypedResults.Unauthorized();
    }

    return TypedResults.Ok(new
    {
        token = jwtTokenService.Create(user),
        user = ToUserResponse(user)
    });
});

var tournaments = app.MapGroup("/tournaments").RequireAuthorization();

tournaments.MapPost("/", async Task<IResult> (
    CreateTournamentRequest request,
    ClaimsPrincipal principal,
    NexusArenaDbContext dbContext,
    CancellationToken cancellationToken) =>
{
    var validation = Validate(request);
    if (validation is not null)
    {
        return TypedResults.ValidationProblem(validation);
    }

    if (!CanOrganize(principal))
    {
        return TypedResults.Forbid();
    }

    var tournament = new Tournament
    {
        Name = request.Name.Trim(),
        GameTitle = request.GameTitle.Trim(),
        Format = request.Format,
        Visibility = request.Visibility,
        MaxParticipants = request.MaxParticipants,
        StartDateUtc = request.StartDateUtc,
        RegistrationClosesAtUtc = request.RegistrationClosesAtUtc,
        CheckInOpensAtUtc = request.CheckInOpensAtUtc,
        CheckInClosesAtUtc = request.CheckInClosesAtUtc,
        Rules = request.Rules.Trim(),
        OrganizerId = GetUserId(principal),
        Status = TournamentStatus.RegistrationOpen
    };

    dbContext.Tournaments.Add(tournament);
    await dbContext.SaveChangesAsync(cancellationToken);

    return TypedResults.Created($"/tournaments/{tournament.Id}", ToTournamentResponse(tournament, 0, 0));
});

tournaments.MapGet("/", async Task<IResult> (
    string? game,
    TournamentStatus? status,
    NexusArenaDbContext dbContext,
    CancellationToken cancellationToken) =>
{
    var query = dbContext.Tournaments.AsQueryable();
    if (!string.IsNullOrWhiteSpace(game))
    {
        query = query.Where(x => x.GameTitle == game);
    }

    if (status.HasValue)
    {
        query = query.Where(x => x.Status == status.Value);
    }

    var items = await query
        .OrderByDescending(x => x.CreatedAtUtc)
        .Select(x => new
        {
            tournament = x,
            registered = x.Registrations.Count(r => r.Status != RegistrationStatus.Waitlisted && r.Status != RegistrationStatus.Withdrawn),
            waitlist = x.Registrations.Count(r => r.Status == RegistrationStatus.Waitlisted)
        })
        .ToListAsync(cancellationToken);

    return TypedResults.Ok(items.Select(x => ToTournamentResponse(x.tournament, x.registered, x.waitlist)));
});

tournaments.MapGet("/{tournamentId:guid}", async Task<IResult> (
    Guid tournamentId,
    NexusArenaDbContext dbContext,
    CancellationToken cancellationToken) =>
{
    var tournament = await dbContext.Tournaments
        .Include(x => x.Registrations)
        .Include(x => x.Groups)
        .Include(x => x.Matches)
        .FirstOrDefaultAsync(x => x.Id == tournamentId, cancellationToken);

    if (tournament is null)
    {
        return TypedResults.NotFound();
    }

    return TypedResults.Ok(new
    {
        tournament = ToTournamentResponse(
            tournament,
            tournament.Registrations.Count(r => r.Status != RegistrationStatus.Waitlisted && r.Status != RegistrationStatus.Withdrawn),
            tournament.Registrations.Count(r => r.Status == RegistrationStatus.Waitlisted)),
        groups = tournament.Groups
            .OrderBy(x => x.Order)
            .Select(x => new { x.Id, x.Name, x.Order }),
        matches = tournament.Matches
            .OrderBy(x => x.Stage)
            .ThenBy(x => x.RoundNumber)
            .ThenBy(x => x.Sequence)
            .Select(ToMatchResponse)
    });
});

tournaments.MapPut("/{tournamentId:guid}", async Task<IResult> (
    Guid tournamentId,
    UpdateTournamentRequest request,
    ClaimsPrincipal principal,
    NexusArenaDbContext dbContext,
    CancellationToken cancellationToken) =>
{
    var validation = Validate(request);
    if (validation is not null)
    {
        return TypedResults.ValidationProblem(validation);
    }

    var tournament = await dbContext.Tournaments.FirstOrDefaultAsync(x => x.Id == tournamentId, cancellationToken);
    if (tournament is null)
    {
        return TypedResults.NotFound();
    }

    if (!CanManageTournament(principal, tournament))
    {
        return TypedResults.Forbid();
    }

    if (tournament.Status is TournamentStatus.InProgress or TournamentStatus.Completed or TournamentStatus.Cancelled)
    {
        return TypedResults.ValidationProblem(new Dictionary<string, string[]>
        {
            ["status"] = ["Nao e possivel editar torneios em andamento, finalizados ou cancelados."]
        });
    }

    tournament.Name = request.Name.Trim();
    tournament.GameTitle = request.GameTitle.Trim();
    tournament.Visibility = request.Visibility;
    tournament.MaxParticipants = request.MaxParticipants;
    tournament.StartDateUtc = request.StartDateUtc;
    tournament.RegistrationClosesAtUtc = request.RegistrationClosesAtUtc;
    tournament.CheckInOpensAtUtc = request.CheckInOpensAtUtc;
    tournament.CheckInClosesAtUtc = request.CheckInClosesAtUtc;
    tournament.Rules = request.Rules.Trim();

    await dbContext.SaveChangesAsync(cancellationToken);
    return TypedResults.Ok(ToTournamentResponse(tournament, 0, 0));
});

tournaments.MapDelete("/{tournamentId:guid}", async Task<IResult> (
    Guid tournamentId,
    ClaimsPrincipal principal,
    NexusArenaDbContext dbContext,
    CancellationToken cancellationToken) =>
{
    var tournament = await dbContext.Tournaments.FirstOrDefaultAsync(x => x.Id == tournamentId, cancellationToken);
    if (tournament is null)
    {
        return TypedResults.NotFound();
    }

    if (!CanManageTournament(principal, tournament))
    {
        return TypedResults.Forbid();
    }

    if (tournament.Status == TournamentStatus.InProgress)
    {
        return TypedResults.ValidationProblem(new Dictionary<string, string[]>
        {
            ["status"] = ["Nao e possivel remover um torneio em andamento."]
        });
    }

    dbContext.Tournaments.Remove(tournament);
    await dbContext.SaveChangesAsync(cancellationToken);
    return TypedResults.NoContent();
});

tournaments.MapPost("/{tournamentId:guid}/register", async Task<IResult> (
    Guid tournamentId,
    ClaimsPrincipal principal,
    NexusArenaDbContext dbContext,
    ProgressionService progressionService,
    CancellationToken cancellationToken) =>
{
    var tournament = await dbContext.Tournaments.FirstOrDefaultAsync(x => x.Id == tournamentId, cancellationToken);
    if (tournament is null)
    {
        return TypedResults.NotFound();
    }

    if (tournament.Status != TournamentStatus.RegistrationOpen)
    {
        return TypedResults.ValidationProblem(new Dictionary<string, string[]>
        {
            ["status"] = ["O torneio nao esta com inscricoes abertas."]
        });
    }

    var userId = GetUserId(principal);
    var alreadyRegistered = await dbContext.TournamentRegistrations
        .AnyAsync(x => x.TournamentId == tournamentId && x.UserId == userId, cancellationToken);
    if (alreadyRegistered)
    {
        return TypedResults.ValidationProblem(new Dictionary<string, string[]>
        {
            ["registration"] = ["Usuario ja inscrito neste torneio."]
        });
    }

    var confirmedCount = await dbContext.TournamentRegistrations
        .CountAsync(x => x.TournamentId == tournamentId &&
                         x.Status != RegistrationStatus.Waitlisted &&
                         x.Status != RegistrationStatus.Withdrawn, cancellationToken);

    var registration = new TournamentRegistration
    {
        TournamentId = tournamentId,
        UserId = userId,
        Status = confirmedCount >= tournament.MaxParticipants ? RegistrationStatus.Waitlisted : RegistrationStatus.Registered
    };

    dbContext.TournamentRegistrations.Add(registration);

    var user = await dbContext.Users.FirstAsync(x => x.Id == userId, cancellationToken);
    await progressionService.AwardSignupXpAsync(user, tournament, cancellationToken);

    await dbContext.SaveChangesAsync(cancellationToken);

    return TypedResults.Ok(new
    {
        registration.Id,
        registration.Status,
        registration.RegisteredAtUtc
    });
});

tournaments.MapPost("/{tournamentId:guid}/check-in", async Task<IResult> (
    Guid tournamentId,
    ClaimsPrincipal principal,
    NexusArenaDbContext dbContext,
    CancellationToken cancellationToken) =>
{
    var currentUserId = GetUserId(principal);
    var registration = await dbContext.TournamentRegistrations
        .Include(x => x.Tournament)
        .FirstOrDefaultAsync(x => x.TournamentId == tournamentId && x.UserId == currentUserId, cancellationToken);

    if (registration is null)
    {
        return TypedResults.NotFound();
    }

    if (registration.Tournament.Status != TournamentStatus.CheckInOpen)
    {
        return TypedResults.ValidationProblem(new Dictionary<string, string[]>
        {
            ["status"] = ["O check-in nao esta aberto para este torneio."]
        });
    }

    if (registration.Status == RegistrationStatus.Waitlisted)
    {
        return TypedResults.ValidationProblem(new Dictionary<string, string[]>
        {
            ["registration"] = ["Participantes em fila de espera nao podem fazer check-in."]
        });
    }

    registration.Status = RegistrationStatus.CheckedIn;
    registration.CheckedInAtUtc = DateTime.UtcNow;
    await dbContext.SaveChangesAsync(cancellationToken);

    return TypedResults.Ok(new
    {
        registration.Id,
        registration.Status,
        registration.CheckedInAtUtc
    });
});

tournaments.MapPost("/{tournamentId:guid}/close-registration", async Task<IResult> (
    Guid tournamentId,
    ClaimsPrincipal principal,
    NexusArenaDbContext dbContext,
    CancellationToken cancellationToken) =>
{
    var tournament = await dbContext.Tournaments.FirstOrDefaultAsync(x => x.Id == tournamentId, cancellationToken);
    if (tournament is null)
    {
        return TypedResults.NotFound();
    }

    if (!CanManageTournament(principal, tournament))
    {
        return TypedResults.Forbid();
    }

    tournament.Status = TournamentStatus.RegistrationClosed;
    await dbContext.SaveChangesAsync(cancellationToken);
    return TypedResults.Ok(new { tournament.Id, tournament.Status });
});

tournaments.MapPost("/{tournamentId:guid}/open-check-in", async Task<IResult> (
    Guid tournamentId,
    ClaimsPrincipal principal,
    NexusArenaDbContext dbContext,
    CancellationToken cancellationToken) =>
{
    var tournament = await dbContext.Tournaments.FirstOrDefaultAsync(x => x.Id == tournamentId, cancellationToken);
    if (tournament is null)
    {
        return TypedResults.NotFound();
    }

    if (!CanManageTournament(principal, tournament))
    {
        return TypedResults.Forbid();
    }

    tournament.Status = TournamentStatus.CheckInOpen;
    await dbContext.SaveChangesAsync(cancellationToken);
    return TypedResults.Ok(new { tournament.Id, tournament.Status });
});

tournaments.MapPost("/{tournamentId:guid}/start", async Task<IResult> (
    Guid tournamentId,
    ClaimsPrincipal principal,
    NexusArenaDbContext dbContext,
    BracketService bracketService,
    CancellationToken cancellationToken) =>
{
    var tournament = await dbContext.Tournaments.FirstOrDefaultAsync(x => x.Id == tournamentId, cancellationToken);
    if (tournament is null)
    {
        return TypedResults.NotFound();
    }

    if (!CanManageTournament(principal, tournament))
    {
        return TypedResults.Forbid();
    }

    var hasMatches = await dbContext.Matches.AnyAsync(x => x.TournamentId == tournamentId, cancellationToken);
    if (hasMatches)
    {
        return TypedResults.ValidationProblem(new Dictionary<string, string[]>
        {
            ["matches"] = ["As chaves desse torneio ja foram geradas."]
        });
    }

    tournament.Status = TournamentStatus.InProgress;
    await dbContext.SaveChangesAsync(cancellationToken);

    if (tournament.Format == TournamentFormat.SingleElimination)
    {
        await bracketService.GenerateSingleEliminationAsync(tournament, cancellationToken);
    }
    else
    {
        await bracketService.GenerateGroupStageAsync(tournament, cancellationToken);
    }

    return TypedResults.Ok(new { tournament.Id, tournament.Status });
});

var matches = app.MapGroup("/matches").RequireAuthorization();

matches.MapPost("/{matchId:guid}/report-result", async Task<IResult> (
    Guid matchId,
    ReportMatchResultRequest request,
    ClaimsPrincipal principal,
    NexusArenaDbContext dbContext,
    CancellationToken cancellationToken) =>
{
    var validation = Validate(request);
    if (validation is not null)
    {
        return TypedResults.ValidationProblem(validation);
    }

    var match = await dbContext.Matches
        .Include(x => x.Tournament)
        .FirstOrDefaultAsync(x => x.Id == matchId, cancellationToken);

    if (match is null)
    {
        return TypedResults.NotFound();
    }

    if (!CanManageTournament(principal, match.Tournament))
    {
        return TypedResults.Forbid();
    }

    if (request.PlayerOneScore == request.PlayerTwoScore)
    {
        return TypedResults.ValidationProblem(new Dictionary<string, string[]>
        {
            ["score"] = ["Empates nao sao suportados nesse fluxo atual."]
        });
    }

    if (match.PlayerOneRegistrationId is null || match.PlayerTwoRegistrationId is null)
    {
        return TypedResults.ValidationProblem(new Dictionary<string, string[]>
        {
            ["participants"] = ["A partida ainda nao possui dois participantes definidos."]
        });
    }

    match.PlayerOneScore = request.PlayerOneScore;
    match.PlayerTwoScore = request.PlayerTwoScore;
    match.WinnerRegistrationId = request.PlayerOneScore > request.PlayerTwoScore
        ? match.PlayerOneRegistrationId
        : match.PlayerTwoRegistrationId;
    match.ResultReportedAtUtc = DateTime.UtcNow;
    match.Status = MatchStatus.PendingConfirmation;
    match.PlayerOneConfirmedAtUtc = null;
    match.PlayerTwoConfirmedAtUtc = null;

    await dbContext.SaveChangesAsync(cancellationToken);
    return TypedResults.Ok(ToMatchResponse(match));
});

matches.MapPost("/{matchId:guid}/confirm", async Task<IResult> (
    Guid matchId,
    ClaimsPrincipal principal,
    NexusArenaDbContext dbContext,
    BracketService bracketService,
    CancellationToken cancellationToken) =>
{
    var match = await dbContext.Matches.FirstOrDefaultAsync(x => x.Id == matchId, cancellationToken);
    if (match is null)
    {
        return TypedResults.NotFound();
    }

    if (match.Status != MatchStatus.PendingConfirmation)
    {
        return TypedResults.ValidationProblem(new Dictionary<string, string[]>
        {
            ["status"] = ["A partida nao esta aguardando confirmacao."]
        });
    }

    var currentUserId = GetUserId(principal);
    var registration = await dbContext.TournamentRegistrations
        .FirstOrDefaultAsync(x => x.TournamentId == match.TournamentId && x.UserId == currentUserId, cancellationToken);

    if (registration is null || (registration.Id != match.PlayerOneRegistrationId && registration.Id != match.PlayerTwoRegistrationId))
    {
        return TypedResults.ValidationProblem(new Dictionary<string, string[]>
        {
            ["player"] = ["Somente os jogadores da partida podem confirmar o resultado."]
        });
    }

    if (registration.Id == match.PlayerOneRegistrationId)
    {
        match.PlayerOneConfirmedAtUtc = DateTime.UtcNow;
    }
    else
    {
        match.PlayerTwoConfirmedAtUtc = DateTime.UtcNow;
    }

    var bothConfirmed = match.PlayerOneConfirmedAtUtc.HasValue && match.PlayerTwoConfirmedAtUtc.HasValue;
    if (bothConfirmed)
    {
        match.Status = MatchStatus.Confirmed;
        match.ConfirmedAtUtc = DateTime.UtcNow;
        await dbContext.SaveChangesAsync(cancellationToken);
        await bracketService.FinalizeConfirmedResultAsync(match, cancellationToken);
    }
    else
    {
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    return TypedResults.Ok(ToMatchResponse(match));
});

var feedback = app.MapGroup("/feedback").RequireAuthorization();
feedback.MapPost("/tournaments/{tournamentId:guid}", async Task<IResult> (
    Guid tournamentId,
    SubmitFeedbackRequest request,
    ClaimsPrincipal principal,
    NexusArenaDbContext dbContext,
    ProgressionService progressionService,
    CancellationToken cancellationToken) =>
{
    var validation = Validate(request);
    if (validation is not null)
    {
        return TypedResults.ValidationProblem(validation);
    }

    var fromUserId = GetUserId(principal);
    var tournament = await dbContext.Tournaments.FirstOrDefaultAsync(x => x.Id == tournamentId, cancellationToken);
    if (tournament is null)
    {
        return TypedResults.NotFound();
    }

    if (fromUserId == request.ToUserId)
    {
        return TypedResults.ValidationProblem(new Dictionary<string, string[]>
        {
            ["feedback"] = ["Nao e possivel avaliar a si mesmo."]
        });
    }

    var fromRegistered = await dbContext.TournamentRegistrations.AnyAsync(x => x.TournamentId == tournamentId && x.UserId == fromUserId, cancellationToken);
    var toRegistered = await dbContext.TournamentRegistrations.AnyAsync(x => x.TournamentId == tournamentId && x.UserId == request.ToUserId, cancellationToken);
    if (!fromRegistered || !toRegistered)
    {
        return TypedResults.ValidationProblem(new Dictionary<string, string[]>
        {
            ["feedback"] = ["Ambos os jogadores precisam ter participado do torneio."]
        });
    }

    var entity = new PlayerFeedback
    {
        TournamentId = tournamentId,
        FromUserId = fromUserId,
        ToUserId = request.ToUserId,
        Rating = request.Rating,
        Comment = request.Comment?.Trim()
    };

    dbContext.PlayerFeedback.Add(entity);

    if (request.Rating >= 4)
    {
        var user = await dbContext.Users.FirstAsync(x => x.Id == request.ToUserId, cancellationToken);
        user.PositiveRatingsReceived += 1;
        await progressionService.AwardPositiveFeedbackXpAsync(user, tournament, cancellationToken);
    }

    await dbContext.SaveChangesAsync(cancellationToken);
    return TypedResults.Ok(new { entity.Id, entity.Rating, entity.CreatedAtUtc });
});

var profile = app.MapGroup("/profile").RequireAuthorization();
profile.MapGet("/me", async Task<IResult> (
    ClaimsPrincipal principal,
    NexusArenaDbContext dbContext,
    CancellationToken cancellationToken) =>
{
    var currentUserId = GetUserId(principal);
    var user = await dbContext.Users
        .Include(x => x.Achievements)
        .FirstOrDefaultAsync(x => x.Id == currentUserId, cancellationToken);

    if (user is null)
    {
        return TypedResults.NotFound();
    }

    return TypedResults.Ok(new
    {
        user = ToUserResponse(user),
        achievements = user.Achievements.OrderBy(x => x.UnlockedAtUtc).Select(x => new
        {
            x.Code,
            x.Name,
            x.Description,
            x.UnlockedAtUtc
        })
    });
});

var ranking = app.MapGroup("/ranking").RequireAuthorization();
ranking.MapGet("/global", async Task<IResult> (
    string? game,
    NexusArenaDbContext dbContext,
    CancellationToken cancellationToken) =>
{
    if (string.IsNullOrWhiteSpace(game))
    {
        var users = await dbContext.Users
            .OrderByDescending(x => x.TotalXp)
            .ThenByDescending(x => x.TotalTitles)
            .ThenBy(x => x.Nickname)
            .ToListAsync(cancellationToken);

        return TypedResults.Ok(users.Select((user, index) => new
        {
            position = index + 1,
            user.Nickname,
            level = ProgressionService.GetLevel(user.TotalXp).ToString(),
            xp = user.TotalXp,
            titles = user.TotalTitles
        }));
    }

    var filtered = await dbContext.XpLedgerEntries
        .Where(x => x.GameTitle == game)
        .GroupBy(x => x.UserId)
        .Select(group => new
        {
            UserId = group.Key,
            Xp = group.Sum(x => x.Amount)
        })
        .Join(dbContext.Users, x => x.UserId, x => x.Id, (xp, user) => new
        {
            user.Nickname,
            xp.Xp,
            Titles = user.Registrations.Count(r => r.Tournament.GameTitle == game && r.Status == RegistrationStatus.Champion)
        })
        .OrderByDescending(x => x.Xp)
        .ThenByDescending(x => x.Titles)
        .ThenBy(x => x.Nickname)
        .ToListAsync(cancellationToken);

    return TypedResults.Ok(filtered.Select((user, index) => new
    {
        position = index + 1,
        user.Nickname,
        level = ProgressionService.GetLevel(user.Xp).ToString(),
        xp = user.Xp,
        titles = user.Titles
    }));
});

app.Run();

static Dictionary<string, string[]>? Validate<T>(T request)
{
    var context = new ValidationContext(request!);
    var results = new List<ValidationResult>();
    var valid = Validator.TryValidateObject(request!, context, results, true);
    if (valid)
    {
        return null;
    }

    return results
        .GroupBy(x => x.MemberNames.FirstOrDefault() ?? string.Empty)
        .ToDictionary(x => x.Key, x => x.Select(y => y.ErrorMessage ?? "Invalid value.").ToArray());
}

static Guid GetUserId(ClaimsPrincipal principal)
    => Guid.Parse(principal.FindFirstValue(ClaimTypes.NameIdentifier) ?? principal.FindFirstValue("sub")!);

static bool CanOrganize(ClaimsPrincipal principal)
{
    var role = principal.FindFirstValue(ClaimTypes.Role);
    return role is nameof(UserRole.Organizer) or nameof(UserRole.Admin);
}

static bool CanManageTournament(ClaimsPrincipal principal, Tournament tournament)
    => CanOrganize(principal) && (GetUserId(principal) == tournament.OrganizerId || principal.FindFirstValue(ClaimTypes.Role) == nameof(UserRole.Admin));

static object ToUserResponse(User user) => new
{
    user.Id,
    user.Nickname,
    user.Email,
    user.Role,
    user.TotalXp,
    Level = ProgressionService.GetLevel(user.TotalXp).ToString(),
    user.TotalWins,
    user.TotalLosses,
    user.TotalTitles,
    user.TournamentsParticipated,
    user.PositiveRatingsReceived,
    user.ConsecutiveTitles,
    user.CreatedAtUtc
};

static object ToTournamentResponse(Tournament tournament, int registeredCount, int waitlistCount) => new
{
    tournament.Id,
    tournament.Name,
    tournament.GameTitle,
    tournament.Format,
    tournament.Visibility,
    tournament.Status,
    tournament.MaxParticipants,
    registeredCount,
    waitlistCount,
    tournament.StartDateUtc,
    tournament.RegistrationClosesAtUtc,
    tournament.CheckInOpensAtUtc,
    tournament.CheckInClosesAtUtc,
    tournament.Rules,
    tournament.OrganizerId,
    tournament.CreatedAtUtc,
    tournament.CompletedAtUtc
};

static object ToMatchResponse(Match match) => new
{
    match.Id,
    match.TournamentId,
    match.TournamentGroupId,
    match.Stage,
    match.Status,
    match.RoundNumber,
    match.Sequence,
    match.PlayerOneRegistrationId,
    match.PlayerTwoRegistrationId,
    match.PlayerOneScore,
    match.PlayerTwoScore,
    match.WinnerRegistrationId,
    match.NextMatchId,
    match.NextMatchSlot,
    match.ResultReportedAtUtc,
    match.PlayerOneConfirmedAtUtc,
    match.PlayerTwoConfirmedAtUtc,
    match.ConfirmedAtUtc
};
