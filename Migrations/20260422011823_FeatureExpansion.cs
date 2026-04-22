using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace nexusarena.Migrations
{
    /// <inheritdoc />
    public partial class FeatureExpansion : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "GameCatalogItemId",
                table: "Tournaments",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "DisputedAtUtc",
                table: "Matches",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ResolutionNote",
                table: "Matches",
                type: "nvarchar(400)",
                maxLength: 400,
                nullable: true);

            migrationBuilder.CreateTable(
                name: "GameCatalogItems",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Title = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: false),
                    Slug = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_GameCatalogItems", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "MatchResultDisputes",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    MatchId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TournamentRegistrationId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Status = table.Column<int>(type: "int", nullable: false),
                    Reason = table.Column<string>(type: "nvarchar(400)", maxLength: 400, nullable: false),
                    ResolutionNote = table.Column<string>(type: "nvarchar(400)", maxLength: 400, nullable: true),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ResolvedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MatchResultDisputes", x => x.Id);
                    table.ForeignKey(
                        name: "FK_MatchResultDisputes_Matches_MatchId",
                        column: x => x.MatchId,
                        principalTable: "Matches",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_MatchResultDisputes_TournamentRegistrations_TournamentRegistrationId",
                        column: x => x.TournamentRegistrationId,
                        principalTable: "TournamentRegistrations",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "TournamentNotifications",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    UserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TournamentId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    Type = table.Column<int>(type: "int", nullable: false),
                    Message = table.Column<string>(type: "nvarchar(240)", maxLength: 240, nullable: false),
                    IsRead = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TournamentNotifications", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TournamentNotifications_Tournaments_TournamentId",
                        column: x => x.TournamentId,
                        principalTable: "Tournaments",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_TournamentNotifications_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Tournaments_GameCatalogItemId",
                table: "Tournaments",
                column: "GameCatalogItemId");

            migrationBuilder.CreateIndex(
                name: "IX_Matches_PlayerOneRegistrationId",
                table: "Matches",
                column: "PlayerOneRegistrationId");

            migrationBuilder.CreateIndex(
                name: "IX_Matches_PlayerTwoRegistrationId",
                table: "Matches",
                column: "PlayerTwoRegistrationId");

            migrationBuilder.CreateIndex(
                name: "IX_Matches_WinnerRegistrationId",
                table: "Matches",
                column: "WinnerRegistrationId");

            migrationBuilder.CreateIndex(
                name: "IX_GameCatalogItems_Slug",
                table: "GameCatalogItems",
                column: "Slug",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_GameCatalogItems_Title",
                table: "GameCatalogItems",
                column: "Title",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_MatchResultDisputes_MatchId",
                table: "MatchResultDisputes",
                column: "MatchId");

            migrationBuilder.CreateIndex(
                name: "IX_MatchResultDisputes_TournamentRegistrationId",
                table: "MatchResultDisputes",
                column: "TournamentRegistrationId");

            migrationBuilder.CreateIndex(
                name: "IX_TournamentNotifications_TournamentId",
                table: "TournamentNotifications",
                column: "TournamentId");

            migrationBuilder.CreateIndex(
                name: "IX_TournamentNotifications_UserId",
                table: "TournamentNotifications",
                column: "UserId");

            migrationBuilder.AddForeignKey(
                name: "FK_Matches_TournamentRegistrations_PlayerOneRegistrationId",
                table: "Matches",
                column: "PlayerOneRegistrationId",
                principalTable: "TournamentRegistrations",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_Matches_TournamentRegistrations_PlayerTwoRegistrationId",
                table: "Matches",
                column: "PlayerTwoRegistrationId",
                principalTable: "TournamentRegistrations",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_Matches_TournamentRegistrations_WinnerRegistrationId",
                table: "Matches",
                column: "WinnerRegistrationId",
                principalTable: "TournamentRegistrations",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_Tournaments_GameCatalogItems_GameCatalogItemId",
                table: "Tournaments",
                column: "GameCatalogItemId",
                principalTable: "GameCatalogItems",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Matches_TournamentRegistrations_PlayerOneRegistrationId",
                table: "Matches");

            migrationBuilder.DropForeignKey(
                name: "FK_Matches_TournamentRegistrations_PlayerTwoRegistrationId",
                table: "Matches");

            migrationBuilder.DropForeignKey(
                name: "FK_Matches_TournamentRegistrations_WinnerRegistrationId",
                table: "Matches");

            migrationBuilder.DropForeignKey(
                name: "FK_Tournaments_GameCatalogItems_GameCatalogItemId",
                table: "Tournaments");

            migrationBuilder.DropTable(
                name: "GameCatalogItems");

            migrationBuilder.DropTable(
                name: "MatchResultDisputes");

            migrationBuilder.DropTable(
                name: "TournamentNotifications");

            migrationBuilder.DropIndex(
                name: "IX_Tournaments_GameCatalogItemId",
                table: "Tournaments");

            migrationBuilder.DropIndex(
                name: "IX_Matches_PlayerOneRegistrationId",
                table: "Matches");

            migrationBuilder.DropIndex(
                name: "IX_Matches_PlayerTwoRegistrationId",
                table: "Matches");

            migrationBuilder.DropIndex(
                name: "IX_Matches_WinnerRegistrationId",
                table: "Matches");

            migrationBuilder.DropColumn(
                name: "GameCatalogItemId",
                table: "Tournaments");

            migrationBuilder.DropColumn(
                name: "DisputedAtUtc",
                table: "Matches");

            migrationBuilder.DropColumn(
                name: "ResolutionNote",
                table: "Matches");
        }
    }
}
