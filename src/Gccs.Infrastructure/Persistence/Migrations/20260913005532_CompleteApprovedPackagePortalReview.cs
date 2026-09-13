using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Gccs.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class CompleteApprovedPackagePortalReview : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "approved_source_fingerprint",
                schema: "gccs",
                table: "shared_portal_packages",
                type: "character varying(64)",
                maxLength: 64,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<int>(
                name: "approved_source_version",
                schema: "gccs",
                table: "shared_portal_packages",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "external_review_approval_reason",
                schema: "gccs",
                table: "shared_portal_packages",
                type: "character varying(1000)",
                maxLength: 1000,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "external_review_approved_at",
                schema: "gccs",
                table: "shared_portal_packages",
                type: "timestamp with time zone",
                nullable: false,
                defaultValue: new DateTimeOffset(new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)));

            migrationBuilder.AddColumn<Guid>(
                name: "external_review_approved_by_user_id",
                schema: "gccs",
                table: "shared_portal_packages",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "review_due_at",
                schema: "gccs",
                table: "shared_portal_packages",
                type: "timestamp with time zone",
                nullable: false,
                defaultValue: new DateTimeOffset(new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)));

            migrationBuilder.Sql("""
                UPDATE gccs.shared_portal_packages
                SET review_due_at = expires_at,
                    external_review_approved_at = created_at,
                    external_review_approved_by_user_id = created_by_user_id,
                    external_review_approval_reason = 'Legacy explicit portal share; source fingerprint was not captured.',
                    approved_source_version = version;

                ALTER TABLE gccs.shared_portal_packages
                    ALTER COLUMN approved_source_fingerprint DROP DEFAULT,
                    ALTER COLUMN approved_source_version DROP DEFAULT,
                    ALTER COLUMN external_review_approval_reason DROP DEFAULT,
                    ALTER COLUMN external_review_approved_at DROP DEFAULT,
                    ALTER COLUMN review_due_at DROP DEFAULT;
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "approved_source_fingerprint",
                schema: "gccs",
                table: "shared_portal_packages");

            migrationBuilder.DropColumn(
                name: "approved_source_version",
                schema: "gccs",
                table: "shared_portal_packages");

            migrationBuilder.DropColumn(
                name: "external_review_approval_reason",
                schema: "gccs",
                table: "shared_portal_packages");

            migrationBuilder.DropColumn(
                name: "external_review_approved_at",
                schema: "gccs",
                table: "shared_portal_packages");

            migrationBuilder.DropColumn(
                name: "external_review_approved_by_user_id",
                schema: "gccs",
                table: "shared_portal_packages");

            migrationBuilder.DropColumn(
                name: "review_due_at",
                schema: "gccs",
                table: "shared_portal_packages");
        }
    }
}
