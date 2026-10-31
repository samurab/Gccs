using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Gccs.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class EnforceEvidenceDateRange : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                """
                ALTER TABLE gccs.evidence_items
                ADD CONSTRAINT "CK_evidence_items_effective_expiration_range"
                CHECK (effective_at IS NULL OR expires_at IS NULL OR expires_at >= effective_at)
                NOT VALID;
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropCheckConstraint(
                name: "CK_evidence_items_effective_expiration_range",
                schema: "gccs",
                table: "evidence_items");
        }
    }
}
