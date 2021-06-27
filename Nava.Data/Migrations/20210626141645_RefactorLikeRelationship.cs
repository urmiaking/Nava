using System;
using Microsoft.EntityFrameworkCore.Migrations;

namespace Nava.Data.Migrations
{
    public partial class RefactorLikeRelationship : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Likes");

            migrationBuilder.CreateTable(
                name: "LikedMedia",
                columns: table => new
                {
                    UserId = table.Column<int>(type: "int", nullable: false),
                    MediaId = table.Column<int>(type: "int", nullable: false),
                    TimeStamp = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LikedMedia", x => new { x.UserId, x.MediaId });
                    table.ForeignKey(
                        name: "FK_LikedMedia_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_LikedMedia_Media_MediaId",
                        column: x => x.MediaId,
                        principalTable: "Media",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_LikedMedia_MediaId",
                table: "LikedMedia",
                column: "MediaId");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "LikedMedia");

            migrationBuilder.CreateTable(
                name: "Likes",
                columns: table => new
                {
                    LikedMediasId = table.Column<int>(type: "int", nullable: false),
                    LikedUsersId = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Likes", x => new { x.LikedMediasId, x.LikedUsersId });
                    table.ForeignKey(
                        name: "FK_Likes_AspNetUsers_LikedUsersId",
                        column: x => x.LikedUsersId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Likes_Media_LikedMediasId",
                        column: x => x.LikedMediasId,
                        principalTable: "Media",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Likes_LikedUsersId",
                table: "Likes",
                column: "LikedUsersId");
        }
    }
}
