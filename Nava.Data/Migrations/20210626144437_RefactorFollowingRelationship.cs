using System;
using Microsoft.EntityFrameworkCore.Migrations;

namespace Nava.Data.Migrations
{
    public partial class RefactorFollowingRelationship : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Followings");

            migrationBuilder.CreateTable(
                name: "Following",
                columns: table => new
                {
                    UserId = table.Column<int>(type: "int", nullable: false),
                    ArtistId = table.Column<int>(type: "int", nullable: false),
                    TimeStamp = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Following", x => new { x.UserId, x.ArtistId });
                    table.ForeignKey(
                        name: "FK_Following_Artist_ArtistId",
                        column: x => x.ArtistId,
                        principalTable: "Artist",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Following_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Following_ArtistId",
                table: "Following",
                column: "ArtistId");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Following");

            migrationBuilder.CreateTable(
                name: "Followings",
                columns: table => new
                {
                    FollowersId = table.Column<int>(type: "int", nullable: false),
                    FollowingArtistsId = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Followings", x => new { x.FollowersId, x.FollowingArtistsId });
                    table.ForeignKey(
                        name: "FK_Followings_Artist_FollowingArtistsId",
                        column: x => x.FollowingArtistsId,
                        principalTable: "Artist",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Followings_AspNetUsers_FollowersId",
                        column: x => x.FollowersId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Followings_FollowingArtistsId",
                table: "Followings",
                column: "FollowingArtistsId");
        }
    }
}
