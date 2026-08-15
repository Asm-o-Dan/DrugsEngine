using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class ChangeDrugItemRelation : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_DrugItem_Drug_DrugId1",
                table: "DrugItem");

            migrationBuilder.DropIndex(
                name: "IX_DrugItem_DrugId1",
                table: "DrugItem");

            migrationBuilder.DropColumn(
                name: "DrugId1",
                table: "DrugItem");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "DrugId1",
                table: "DrugItem",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_DrugItem_DrugId1",
                table: "DrugItem",
                column: "DrugId1");

            migrationBuilder.AddForeignKey(
                name: "FK_DrugItem_Drug_DrugId1",
                table: "DrugItem",
                column: "DrugId1",
                principalTable: "Drug",
                principalColumn: "Id");
        }
    }
}
