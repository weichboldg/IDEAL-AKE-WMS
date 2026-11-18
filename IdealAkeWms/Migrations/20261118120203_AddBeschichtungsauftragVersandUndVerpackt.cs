using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace IdealAkeWms.Migrations
{
    /// <inheritdoc />
    public partial class AddBeschichtungsauftragVersandUndVerpackt : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "OhneFa",
                schema: "dbo",
                table: "BeschichtungsauftragPositionen",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "Verpackt",
                schema: "dbo",
                table: "BeschichtungsauftragPositionen",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<int>(
                name: "CreditorId",
                schema: "dbo",
                table: "Beschichtungsauftraege",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "VersendetAt",
                schema: "dbo",
                table: "Beschichtungsauftraege",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "VersendetByUserId",
                schema: "dbo",
                table: "Beschichtungsauftraege",
                type: "int",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Beschichtungsauftraege_CreditorId",
                schema: "dbo",
                table: "Beschichtungsauftraege",
                column: "CreditorId");

            migrationBuilder.AddForeignKey(
                name: "FK_Beschichtungsauftraege_Creditors_CreditorId",
                schema: "dbo",
                table: "Beschichtungsauftraege",
                column: "CreditorId",
                principalSchema: "dbo",
                principalTable: "Creditors",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Beschichtungsauftraege_Creditors_CreditorId",
                schema: "dbo",
                table: "Beschichtungsauftraege");

            migrationBuilder.DropIndex(
                name: "IX_Beschichtungsauftraege_CreditorId",
                schema: "dbo",
                table: "Beschichtungsauftraege");

            migrationBuilder.DropColumn(
                name: "OhneFa",
                schema: "dbo",
                table: "BeschichtungsauftragPositionen");

            migrationBuilder.DropColumn(
                name: "Verpackt",
                schema: "dbo",
                table: "BeschichtungsauftragPositionen");

            migrationBuilder.DropColumn(
                name: "CreditorId",
                schema: "dbo",
                table: "Beschichtungsauftraege");

            migrationBuilder.DropColumn(
                name: "VersendetAt",
                schema: "dbo",
                table: "Beschichtungsauftraege");

            migrationBuilder.DropColumn(
                name: "VersendetByUserId",
                schema: "dbo",
                table: "Beschichtungsauftraege");
        }
    }
}
