using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class CatalogApiSupport : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_DrugItem_DrugId",
                table: "DrugItem");

            migrationBuilder.AlterDatabase()
                .Annotation("Npgsql:PostgresExtension:pg_trgm", ",,");

            migrationBuilder.AddColumn<DateTime>(
                name: "GeocodedAt",
                table: "DrugStore",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<double>(
                name: "Latitude",
                table: "DrugStore",
                type: "double precision",
                nullable: true);

            migrationBuilder.AddColumn<double>(
                name: "Longitude",
                table: "DrugStore",
                type: "double precision",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "UpdatedAt",
                table: "DrugItem",
                type: "timestamp with time zone",
                nullable: false,
                defaultValueSql: "now()");

            // Перед уникальными индексами убираем дубли, которые могли накопиться без них
            // (параллельные проходы парсера, препарат искался только по названию).
            // Ссылки дублей переносятся на сохраняемую запись.
            migrationBuilder.Sql("""
                DROP TABLE IF EXISTS _dedupe_map;

                CREATE TEMP TABLE _dedupe_map AS
                SELECT "Id" AS dup, first_value("Id") OVER (PARTITION BY "DrugNetwork", "Number" ORDER BY "Id") AS keep
                FROM "DrugStore";
                DELETE FROM _dedupe_map WHERE dup = keep;
                UPDATE "DrugItem" t SET "DrugStoreId" = m.keep FROM _dedupe_map m WHERE t."DrugStoreId" = m.dup;
                UPDATE "FavoriteDrug" t SET "DrugStoreId" = m.keep FROM _dedupe_map m WHERE t."DrugStoreId" = m.dup;
                DELETE FROM "DrugStore" s USING _dedupe_map m WHERE s."Id" = m.dup;
                DROP TABLE _dedupe_map;

                CREATE TEMP TABLE _dedupe_map AS
                SELECT "Id" AS dup, first_value("Id") OVER (PARTITION BY "Name", "Manufacturer" ORDER BY "Id") AS keep
                FROM "Drug";
                DELETE FROM _dedupe_map WHERE dup = keep;
                UPDATE "DrugItem" t SET "DrugId" = m.keep FROM _dedupe_map m WHERE t."DrugId" = m.dup;
                UPDATE "FavoriteDrug" t SET "DrugId" = m.keep FROM _dedupe_map m WHERE t."DrugId" = m.dup;
                DELETE FROM "Drug" d USING _dedupe_map m WHERE d."Id" = m.dup;
                DROP TABLE _dedupe_map;

                -- После слияния у пары (препарат, аптека) может оказаться несколько цен — оставляем одну.
                DELETE FROM "DrugItem" a USING "DrugItem" b
                WHERE a."DrugId" = b."DrugId" AND a."DrugStoreId" = b."DrugStoreId" AND a.ctid < b.ctid;
                """);

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
                name: "IX_Drug_Name_Manufacturer",
                table: "Drug",
                columns: new[] { "Name", "Manufacturer" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Drug_Name_trgm",
                table: "Drug",
                column: "Name")
                .Annotation("Npgsql:IndexMethod", "gin")
                .Annotation("Npgsql:IndexOperators", new[] { "gin_trgm_ops" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_DrugStore_DrugNetwork_Number",
                table: "DrugStore");

            migrationBuilder.DropIndex(
                name: "IX_DrugItem_DrugId_DrugStoreId",
                table: "DrugItem");

            migrationBuilder.DropIndex(
                name: "IX_Drug_Name_Manufacturer",
                table: "Drug");

            migrationBuilder.DropIndex(
                name: "IX_Drug_Name_trgm",
                table: "Drug");

            migrationBuilder.DropColumn(
                name: "GeocodedAt",
                table: "DrugStore");

            migrationBuilder.DropColumn(
                name: "Latitude",
                table: "DrugStore");

            migrationBuilder.DropColumn(
                name: "Longitude",
                table: "DrugStore");

            migrationBuilder.DropColumn(
                name: "UpdatedAt",
                table: "DrugItem");

            migrationBuilder.AlterDatabase()
                .OldAnnotation("Npgsql:PostgresExtension:pg_trgm", ",,");

            migrationBuilder.CreateIndex(
                name: "IX_DrugItem_DrugId",
                table: "DrugItem",
                column: "DrugId");
        }
    }
}
