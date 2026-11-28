using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class UpdateDomainModelAndMappings : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Drug_Country_CountryCodeId",
                table: "Drug");

            migrationBuilder.DropForeignKey(
                name: "FK_FavoriteDrug_DrugStore_DrugStoreId",
                table: "FavoriteDrug");

            migrationBuilder.DropPrimaryKey(
                name: "PK_FavoriteDrug",
                table: "FavoriteDrug");

            migrationBuilder.DropIndex(
                name: "IX_DrugItem_DrugId",
                table: "DrugItem");

            migrationBuilder.RenameColumn(
                name: "Email_Value",
                table: "UserProfile",
                newName: "Email");

            migrationBuilder.AlterColumn<string>(
                name: "ExternalUserId",
                table: "FavoriteDrug",
                type: "character varying(100)",
                maxLength: 100,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "text");

            migrationBuilder.AlterColumn<Guid>(
                name: "DrugStoreId",
                table: "FavoriteDrug",
                type: "uuid",
                nullable: true,
                oldClrType: typeof(Guid),
                oldType: "uuid");

            migrationBuilder.AddPrimaryKey(
                name: "PK_FavoriteDrug",
                table: "FavoriteDrug",
                column: "Id");

            migrationBuilder.CreateIndex(
                name: "IX_UserProfile_ExternalId",
                table: "UserProfile",
                column: "ExternalId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_FavoriteDrug_ProfileId_DrugId_DrugStoreId",
                table: "FavoriteDrug",
                columns: new[] { "ProfileId", "DrugId", "DrugStoreId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_DrugStore_DrugNetwork_Number",
                table: "DrugStore",
                columns: new[] { "DrugNetwork", "Number" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_DrugItem_DrugId_DrugStoreId",
                table: "DrugItem",
                columns: new[] { "DrugId", "DrugStoreId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Drug_Name",
                table: "Drug",
                column: "Name");

            migrationBuilder.AddForeignKey(
                name: "FK_Drug_Country_CountryCodeId",
                table: "Drug",
                column: "CountryCodeId",
                principalTable: "Country",
                principalColumn: "Code",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_FavoriteDrug_DrugStore_DrugStoreId",
                table: "FavoriteDrug",
                column: "DrugStoreId",
                principalTable: "DrugStore",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Drug_Country_CountryCodeId",
                table: "Drug");

            migrationBuilder.DropForeignKey(
                name: "FK_FavoriteDrug_DrugStore_DrugStoreId",
                table: "FavoriteDrug");

            migrationBuilder.DropIndex(
                name: "IX_UserProfile_ExternalId",
                table: "UserProfile");

            migrationBuilder.DropPrimaryKey(
                name: "PK_FavoriteDrug",
                table: "FavoriteDrug");

            migrationBuilder.DropIndex(
                name: "IX_FavoriteDrug_ProfileId_DrugId_DrugStoreId",
                table: "FavoriteDrug");

            migrationBuilder.DropIndex(
                name: "IX_DrugStore_DrugNetwork_Number",
                table: "DrugStore");

            migrationBuilder.DropIndex(
                name: "IX_DrugItem_DrugId_DrugStoreId",
                table: "DrugItem");

            migrationBuilder.DropIndex(
                name: "IX_Drug_Name",
                table: "Drug");

            migrationBuilder.RenameColumn(
                name: "Email",
                table: "UserProfile",
                newName: "Email_Value");

            migrationBuilder.AlterColumn<string>(
                name: "ExternalUserId",
                table: "FavoriteDrug",
                type: "text",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(100)",
                oldMaxLength: 100);

            migrationBuilder.AlterColumn<Guid>(
                name: "DrugStoreId",
                table: "FavoriteDrug",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"),
                oldClrType: typeof(Guid),
                oldType: "uuid",
                oldNullable: true);

            migrationBuilder.AddPrimaryKey(
                name: "PK_FavoriteDrug",
                table: "FavoriteDrug",
                columns: new[] { "ProfileId", "DrugId", "DrugStoreId" });

            migrationBuilder.CreateIndex(
                name: "IX_DrugItem_DrugId",
                table: "DrugItem",
                column: "DrugId");

            migrationBuilder.AddForeignKey(
                name: "FK_Drug_Country_CountryCodeId",
                table: "Drug",
                column: "CountryCodeId",
                principalTable: "Country",
                principalColumn: "Code",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_FavoriteDrug_DrugStore_DrugStoreId",
                table: "FavoriteDrug",
                column: "DrugStoreId",
                principalTable: "DrugStore",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
