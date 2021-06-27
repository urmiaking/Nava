using System;
using Microsoft.EntityFrameworkCore.Migrations;

namespace Nava.Data.Migrations
{
    public partial class RefactorVisitRelationship : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Visits");

            migrationBuilder.CreateTable(
                name: "VisitedMedia",
                columns: table => new
                {
                    UserId = table.Column<int>(type: "int", nullable: false),
                    MediaId = table.Column<int>(type: "int", nullable: false),
                    TimeStamp = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_VisitedMedia", x => new { x.UserId, x.MediaId });
                    table.ForeignKey(
                        name: "FK_VisitedMedia_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_VisitedMedia_Media_MediaId",
                        column: x => x.MediaId,
                        principalTable: "Media",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_VisitedMedia_MediaId",
                table: "VisitedMedia",
                column: "MediaId");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "VisitedMedia");

            migrationBuilder.CreateTable(
                name: "Visits",
                columns: table => new
                {
                    VisitedMediasId = table.Column<int>(type: "int", nullable: false),
                    VisitedUsersId = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Visits", x => new { x.VisitedMediasId, x.VisitedUsersId });
                    table.ForeignKey(
                        name: "FK_Visits_AspNetUsers_VisitedUsersId",
                        column: x => x.VisitedUsersId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Visits_Media_VisitedMediasId",
                        column: x => x.VisitedMediasId,
                        principalTable: "Media",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Visits_VisitedUsersId",
                table: "Visits",
                column: "VisitedUsersId");
        }
    }
}
