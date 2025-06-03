using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Country",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Code = table.Column<string>(type: "character varying(2)", maxLength: 2, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Country", x => x.Id);
                    table.UniqueConstraint("AK_Country_Code", x => x.Code);
                });

            migrationBuilder.CreateTable(
                name: "DrugStore",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Address_City = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    Address_Street = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Address_House = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false),
                    Address_PostalCode = table.Column<int>(type: "integer", nullable: false),
                    Number = table.Column<int>(type: "integer", nullable: false),
                    PhoneNumber = table.Column<string>(type: "text", nullable: false),
                    DrugNetwork = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DrugStore", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "UserProfile",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ExternalId = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Email_Value = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserProfile", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Drug",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                    Manufacturer = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    CountryCodeId = table.Column<string>(type: "character varying(2)", maxLength: 2, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Drug", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Drug_Country_CountryCodeId",
                        column: x => x.CountryCodeId,
                        principalTable: "Country",
                        principalColumn: "Code",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "DrugItem",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    DrugId = table.Column<Guid>(type: "uuid", nullable: false),
                    DrugStoreId = table.Column<Guid>(type: "uuid", nullable: false),
                    Count = table.Column<double>(type: "double precision", nullable: false),
                    Cost = table.Column<decimal>(type: "numeric", nullable: false),
                    DrugId1 = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DrugItem", x => x.Id);
                    table.ForeignKey(
                        name: "FK_DrugItem_DrugStore_DrugStoreId",
                        column: x => x.DrugStoreId,
                        principalTable: "DrugStore",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_DrugItem_Drug_DrugId",
                        column: x => x.DrugId,
                        principalTable: "Drug",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_DrugItem_Drug_DrugId1",
                        column: x => x.DrugId1,
                        principalTable: "Drug",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "FavoriteDrug",
                columns: table => new
                {
                    DrugId = table.Column<Guid>(type: "uuid", nullable: false),
                    DrugStoreId = table.Column<Guid>(type: "uuid", nullable: false),
                    ProfileId = table.Column<Guid>(type: "uuid", nullable: false),
                    ExternalUserId = table.Column<string>(type: "text", nullable: false),
                    Id = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FavoriteDrug", x => new { x.ProfileId, x.DrugId, x.DrugStoreId });
                    table.ForeignKey(
                        name: "FK_FavoriteDrug_DrugStore_DrugStoreId",
                        column: x => x.DrugStoreId,
                        principalTable: "DrugStore",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_FavoriteDrug_Drug_DrugId",
                        column: x => x.DrugId,
                        principalTable: "Drug",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_FavoriteDrug_UserProfile_ProfileId",
                        column: x => x.ProfileId,
                        principalTable: "UserProfile",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Drug_CountryCodeId",
                table: "Drug",
                column: "CountryCodeId");

            migrationBuilder.CreateIndex(
                name: "IX_DrugItem_DrugId",
                table: "DrugItem",
                column: "DrugId");

            migrationBuilder.CreateIndex(
                name: "IX_DrugItem_DrugId1",
                table: "DrugItem",
                column: "DrugId1");

            migrationBuilder.CreateIndex(
                name: "IX_DrugItem_DrugStoreId",
                table: "DrugItem",
                column: "DrugStoreId");

            migrationBuilder.CreateIndex(
                name: "IX_FavoriteDrug_DrugId",
                table: "FavoriteDrug",
                column: "DrugId");

            migrationBuilder.CreateIndex(
                name: "IX_FavoriteDrug_DrugStoreId",
                table: "FavoriteDrug",
                column: "DrugStoreId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "DrugItem");

            migrationBuilder.DropTable(
                name: "FavoriteDrug");

            migrationBuilder.DropTable(
                name: "DrugStore");

            migrationBuilder.DropTable(
                name: "Drug");

            migrationBuilder.DropTable(
                name: "UserProfile");

            migrationBuilder.DropTable(
                name: "Country");
        }
    }
}
