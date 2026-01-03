using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace _10xCards.Persistance.Migrations
{
    /// <inheritdoc />
    public partial class AddCardCollections : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "CardCollections",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    UserId = table.Column<Guid>(type: "TEXT", nullable: false),
                    Name = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    Description = table.Column<string>(type: "TEXT", maxLength: 1000, nullable: false),
                    CreatedUtc = table.Column<DateTime>(type: "TEXT", nullable: false),
                    UpdatedUtc = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CardCollections", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "CardCollectionBackups",
                columns: table => new
                {
                    CollectionId = table.Column<Guid>(type: "TEXT", nullable: false),
                    PreviousName = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    PreviousDescription = table.Column<string>(type: "TEXT", maxLength: 1000, nullable: false),
                    BackedUpUtc = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CardCollectionBackups", x => x.CollectionId);
                    table.ForeignKey(
                        name: "FK_CardCollectionBackups_CardCollections_CollectionId",
                        column: x => x.CollectionId,
                        principalTable: "CardCollections",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "CardCollectionCards",
                columns: table => new
                {
                    CollectionId = table.Column<Guid>(type: "TEXT", nullable: false),
                    CardId = table.Column<Guid>(type: "TEXT", nullable: false),
                    AddedUtc = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CardCollectionCards", x => new { x.CollectionId, x.CardId });
                    table.ForeignKey(
                        name: "FK_CardCollectionCards_CardCollections_CollectionId",
                        column: x => x.CollectionId,
                        principalTable: "CardCollections",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_CardCollectionCards_Cards_CardId",
                        column: x => x.CardId,
                        principalTable: "Cards",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_CardCollectionCards_CardId",
                table: "CardCollectionCards",
                column: "CardId");

            migrationBuilder.CreateIndex(
                name: "IX_CardCollectionCards_CollectionId",
                table: "CardCollectionCards",
                column: "CollectionId");

            migrationBuilder.CreateIndex(
                name: "IX_CardCollections_CreatedUtc",
                table: "CardCollections",
                column: "CreatedUtc");

            migrationBuilder.CreateIndex(
                name: "IX_CardCollections_UserId",
                table: "CardCollections",
                column: "UserId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "CardCollectionBackups");

            migrationBuilder.DropTable(
                name: "CardCollectionCards");

            migrationBuilder.DropTable(
                name: "CardCollections");
        }
    }
}
