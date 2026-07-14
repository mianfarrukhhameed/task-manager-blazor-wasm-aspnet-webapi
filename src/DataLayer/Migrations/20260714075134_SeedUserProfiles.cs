using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Fistix.TaskManager.DataLayer.Migrations
{
    /// <inheritdoc />
    public partial class SeedUserProfiles : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                """
                INSERT INTO "UserProfile" ("Id", "ExternalId", "Name", "EmailAddress", "IsAdmin")
                SELECT 1, '2ba3faed-ce16-46df-8b95-ab0ef26e8ad6'::uuid, 'dev', 'dev@test.com', false
                WHERE NOT EXISTS (
                    SELECT 1 FROM "UserProfile"
                    WHERE "ExternalId" = '2ba3faed-ce16-46df-8b95-ab0ef26e8ad6'::uuid
                );

                INSERT INTO "UserProfile" ("Id", "ExternalId", "Name", "EmailAddress", "IsAdmin")
                SELECT 2, '1efb2983-09be-47a5-ac2c-bff124d542ec'::uuid, 'admin', 'admin@test.com', true
                WHERE NOT EXISTS (
                    SELECT 1 FROM "UserProfile"
                    WHERE "ExternalId" = '1efb2983-09be-47a5-ac2c-bff124d542ec'::uuid
                );

                SELECT setval(
                    pg_get_serial_sequence('"UserProfile"', 'Id'),
                    COALESCE((SELECT MAX("Id") FROM "UserProfile"), 1)
                );
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                """
                DELETE FROM "UserProfile"
                WHERE "ExternalId" IN (
                    '2ba3faed-ce16-46df-8b95-ab0ef26e8ad6'::uuid,
                    '1efb2983-09be-47a5-ac2c-bff124d542ec'::uuid
                );
                """);
        }
    }
}
