using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class StableStoresAndDrugDetails : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Drug_Country_CountryCodeId",
                table: "Drug");

            migrationBuilder.AddColumn<string>(
                name: "ExternalKey",
                table: "DrugStore",
                type: "character varying(20)",
                maxLength: 20,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Hours",
                table: "DrugStore",
                type: "character varying(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "SeenAt",
                table: "DrugItem",
                type: "timestamp with time zone",
                nullable: false,
                defaultValueSql: "now()");

            migrationBuilder.AlterColumn<string>(
                name: "Name",
                table: "Drug",
                type: "character varying(250)",
                maxLength: 250,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(150)",
                oldMaxLength: 150);

            migrationBuilder.AlterColumn<string>(
                name: "CountryCodeId",
                table: "Drug",
                type: "character varying(2)",
                maxLength: 2,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "character varying(2)",
                oldMaxLength: 2);

            migrationBuilder.AddColumn<string>(
                name: "ActiveIngredient",
                table: "Drug",
                type: "character varying(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Form",
                table: "Drug",
                type: "character varying(50)",
                maxLength: 50,
                nullable: true);

            // Ключ аптеки — цифры телефона без ведущих нулей (см. DrugStore.PhoneKey).
            // Раньше номер аптеки выдумывался по порядку на странице контактов, поэтому одна и та же аптека
            // могла попасть в базу несколько раз под разными номерами — сливаем такие записи.
            migrationBuilder.Sql("""
                UPDATE "DrugStore"
                SET "ExternalKey" = NULLIF(ltrim(regexp_replace("PhoneNumber", '\D', '', 'g'), '0'), '')
                WHERE "ExternalKey" IS NULL;
                UPDATE "DrugStore" SET "ExternalKey" = NULL WHERE length("ExternalKey") < 5;

                DROP TABLE IF EXISTS _dedupe_map;
                CREATE TEMP TABLE _dedupe_map AS
                SELECT "Id" AS dup,
                       first_value("Id") OVER (PARTITION BY "DrugNetwork", "ExternalKey" ORDER BY "Number", "Id") AS keep
                FROM "DrugStore"
                WHERE "ExternalKey" IS NOT NULL;
                DELETE FROM _dedupe_map WHERE dup = keep;

                -- Уникальный индекс (DrugId, DrugStoreId) уже есть: прежде чем переносить цены дублей,
                -- оставляем в каждой группе «аптека + препарат» только самую свежую цену.
                CREATE TEMP TABLE _store_group AS
                SELECT s."Id", coalesce(m.keep, s."Id") AS grp
                FROM "DrugStore" s LEFT JOIN _dedupe_map m ON m.dup = s."Id";
                DELETE FROM "DrugItem" a
                USING "DrugItem" b, _store_group ga, _store_group gb
                WHERE a."DrugStoreId" = ga."Id" AND b."DrugStoreId" = gb."Id"
                  AND ga.grp = gb.grp AND a."DrugId" = b."DrugId" AND a."Id" <> b."Id"
                  AND (a."UpdatedAt", a.ctid) < (b."UpdatedAt", b.ctid);
                DROP TABLE _store_group;

                UPDATE "DrugItem" t SET "DrugStoreId" = m.keep FROM _dedupe_map m WHERE t."DrugStoreId" = m.dup;
                UPDATE "FavoriteDrug" t SET "DrugStoreId" = m.keep FROM _dedupe_map m WHERE t."DrugStoreId" = m.dup;
                DELETE FROM "DrugStore" s USING _dedupe_map m WHERE s."Id" = m.dup;
                DROP TABLE _dedupe_map;

                """);

            migrationBuilder.CreateIndex(
                name: "IX_DrugStore_DrugNetwork_ExternalKey",
                table: "DrugStore",
                columns: new[] { "DrugNetwork", "ExternalKey" },
                unique: true,
                filter: "\"ExternalKey\" IS NOT NULL");

            migrationBuilder.AddForeignKey(
                name: "FK_Drug_Country_CountryCodeId",
                table: "Drug",
                column: "CountryCodeId",
                principalTable: "Country",
                principalColumn: "Code",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Drug_Country_CountryCodeId",
                table: "Drug");

            migrationBuilder.DropIndex(
                name: "IX_DrugStore_DrugNetwork_ExternalKey",
                table: "DrugStore");

            migrationBuilder.DropColumn(
                name: "ExternalKey",
                table: "DrugStore");

            migrationBuilder.DropColumn(
                name: "Hours",
                table: "DrugStore");

            migrationBuilder.DropColumn(
                name: "SeenAt",
                table: "DrugItem");

            migrationBuilder.DropColumn(
                name: "ActiveIngredient",
                table: "Drug");

            migrationBuilder.DropColumn(
                name: "Form",
                table: "Drug");

            migrationBuilder.AlterColumn<string>(
                name: "Name",
                table: "Drug",
                type: "character varying(150)",
                maxLength: 150,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(250)",
                oldMaxLength: 250);

            migrationBuilder.AlterColumn<string>(
                name: "CountryCodeId",
                table: "Drug",
                type: "character varying(2)",
                maxLength: 2,
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "character varying(2)",
                oldMaxLength: 2,
                oldNullable: true);

            migrationBuilder.AddForeignKey(
                name: "FK_Drug_Country_CountryCodeId",
                table: "Drug",
                column: "CountryCodeId",
                principalTable: "Country",
                principalColumn: "Code",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
