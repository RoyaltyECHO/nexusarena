using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace nexusarena.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Users",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Nickname = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false),
                    Email = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: false),
                    PasswordHash = table.Column<string>(type: "nvarchar(512)", maxLength: 512, nullable: false),
                    Role = table.Column<int>(type: "int", nullable: false),
                    TotalXp = table.Column<int>(type: "int", nullable: false),
                    TotalWins = table.Column<int>(type: "int", nullable: false),
                    TotalLosses = table.Column<int>(type: "int", nullable: false),
                    TotalTitles = table.Column<int>(type: "int", nullable: false),
                    TournamentsParticipated = table.Column<int>(type: "int", nullable: false),
                    PositiveRatingsReceived = table.Column<int>(type: "int", nullable: false),
                    ConsecutiveTitles = table.Column<int>(type: "int", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Users", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Tournaments",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: false),
                    GameTitle = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: false),
                    Format = table.Column<int>(type: "int", nullable: false),
                    Visibility = table.Column<int>(type: "int", nullable: false),
                    Status = table.Column<int>(type: "int", nullable: false),
                    MaxParticipants = table.Column<int>(type: "int", nullable: false),
                    StartDateUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    RegistrationClosesAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CheckInOpensAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CheckInClosesAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    Rules = table.Column<string>(type: "nvarchar(4000)", maxLength: 4000, nullable: false),
                    OrganizerId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CompletedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Tournaments", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Tournaments_Users_OrganizerId",
                        column: x => x.OrganizerId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "UserAchievements",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    UserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Code = table.Column<int>(type: "int", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(240)", maxLength: 240, nullable: false),
                    UnlockedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserAchievements", x => x.Id);
                    table.ForeignKey(
                        name: "FK_UserAchievements_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "XpLedgerEntries",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    UserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TournamentId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    MatchId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    ActionType = table.Column<int>(type: "int", nullable: false),
                    Amount = table.Column<int>(type: "int", nullable: false),
                    Description = table.Column<string>(type: "nvarchar(240)", maxLength: 240, nullable: false),
                    GameTitle = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: true),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_XpLedgerEntries", x => x.Id);
                    table.ForeignKey(
                        name: "FK_XpLedgerEntries_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "PlayerFeedback",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TournamentId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    FromUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ToUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Rating = table.Column<int>(type: "int", nullable: false),
                    Comment = table.Column<string>(type: "nvarchar(400)", maxLength: 400, nullable: true),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PlayerFeedback", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PlayerFeedback_Tournaments_TournamentId",
                        column: x => x.TournamentId,
                        principalTable: "Tournaments",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_PlayerFeedback_Users_FromUserId",
                        column: x => x.FromUserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_PlayerFeedback_Users_ToUserId",
                        column: x => x.ToUserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "TournamentGroups",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TournamentId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false),
                    Order = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TournamentGroups", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TournamentGroups_Tournaments_TournamentId",
                        column: x => x.TournamentId,
                        principalTable: "Tournaments",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "TournamentRegistrations",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TournamentId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    UserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Status = table.Column<int>(type: "int", nullable: false),
                    RegisteredAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CheckedInAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    Seed = table.Column<int>(type: "int", nullable: true),
                    Wins = table.Column<int>(type: "int", nullable: false),
                    Losses = table.Column<int>(type: "int", nullable: false),
                    FinalPlacement = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TournamentRegistrations", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TournamentRegistrations_Tournaments_TournamentId",
                        column: x => x.TournamentId,
                        principalTable: "Tournaments",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_TournamentRegistrations_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Matches",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TournamentId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TournamentGroupId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    Stage = table.Column<int>(type: "int", nullable: false),
                    Status = table.Column<int>(type: "int", nullable: false),
                    RoundNumber = table.Column<int>(type: "int", nullable: false),
                    Sequence = table.Column<int>(type: "int", nullable: false),
                    PlayerOneRegistrationId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    PlayerTwoRegistrationId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    PlayerOneScore = table.Column<int>(type: "int", nullable: true),
                    PlayerTwoScore = table.Column<int>(type: "int", nullable: true),
                    WinnerRegistrationId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    NextMatchId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    NextMatchSlot = table.Column<int>(type: "int", nullable: true),
                    ResultReportedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    PlayerOneConfirmedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    PlayerTwoConfirmedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ConfirmedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Matches", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Matches_TournamentGroups_TournamentGroupId",
                        column: x => x.TournamentGroupId,
                        principalTable: "TournamentGroups",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_Matches_Tournaments_TournamentId",
                        column: x => x.TournamentId,
                        principalTable: "Tournaments",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "TournamentGroupMembers",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TournamentGroupId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TournamentRegistrationId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Points = table.Column<int>(type: "int", nullable: false),
                    Wins = table.Column<int>(type: "int", nullable: false),
                    Losses = table.Column<int>(type: "int", nullable: false),
                    ScoreFor = table.Column<int>(type: "int", nullable: false),
                    ScoreAgainst = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TournamentGroupMembers", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TournamentGroupMembers_TournamentGroups_TournamentGroupId",
                        column: x => x.TournamentGroupId,
                        principalTable: "TournamentGroups",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_TournamentGroupMembers_TournamentRegistrations_TournamentRegistrationId",
                        column: x => x.TournamentRegistrationId,
                        principalTable: "TournamentRegistrations",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateIndex(
                name: "IX_Matches_TournamentGroupId",
                table: "Matches",
                column: "TournamentGroupId");

            migrationBuilder.CreateIndex(
                name: "IX_Matches_TournamentId_Stage_RoundNumber_Sequence",
                table: "Matches",
                columns: new[] { "TournamentId", "Stage", "RoundNumber", "Sequence" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_PlayerFeedback_FromUserId",
                table: "PlayerFeedback",
                column: "FromUserId");

            migrationBuilder.CreateIndex(
                name: "IX_PlayerFeedback_TournamentId_FromUserId_ToUserId",
                table: "PlayerFeedback",
                columns: new[] { "TournamentId", "FromUserId", "ToUserId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_PlayerFeedback_ToUserId",
                table: "PlayerFeedback",
                column: "ToUserId");

            migrationBuilder.CreateIndex(
                name: "IX_TournamentGroupMembers_TournamentGroupId_TournamentRegistrationId",
                table: "TournamentGroupMembers",
                columns: new[] { "TournamentGroupId", "TournamentRegistrationId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_TournamentGroupMembers_TournamentRegistrationId",
                table: "TournamentGroupMembers",
                column: "TournamentRegistrationId");

            migrationBuilder.CreateIndex(
                name: "IX_TournamentGroups_TournamentId_Order",
                table: "TournamentGroups",
                columns: new[] { "TournamentId", "Order" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_TournamentRegistrations_TournamentId_UserId",
                table: "TournamentRegistrations",
                columns: new[] { "TournamentId", "UserId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_TournamentRegistrations_UserId",
                table: "TournamentRegistrations",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_Tournaments_OrganizerId",
                table: "Tournaments",
                column: "OrganizerId");

            migrationBuilder.CreateIndex(
                name: "IX_UserAchievements_UserId_Code",
                table: "UserAchievements",
                columns: new[] { "UserId", "Code" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Users_Email",
                table: "Users",
                column: "Email",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Users_Nickname",
                table: "Users",
                column: "Nickname",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_XpLedgerEntries_UserId",
                table: "XpLedgerEntries",
                column: "UserId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Matches");

            migrationBuilder.DropTable(
                name: "PlayerFeedback");

            migrationBuilder.DropTable(
                name: "TournamentGroupMembers");

            migrationBuilder.DropTable(
                name: "UserAchievements");

            migrationBuilder.DropTable(
                name: "XpLedgerEntries");

            migrationBuilder.DropTable(
                name: "TournamentGroups");

            migrationBuilder.DropTable(
                name: "TournamentRegistrations");

            migrationBuilder.DropTable(
                name: "Tournaments");

            migrationBuilder.DropTable(
                name: "Users");
        }
    }
}
