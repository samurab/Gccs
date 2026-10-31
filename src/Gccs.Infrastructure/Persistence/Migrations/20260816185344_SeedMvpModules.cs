using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace Gccs.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class SeedMvpModules : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.InsertData(
                schema: "gccs",
                table: "mvp_modules",
                columns: new[] { "key", "name", "purpose", "status" },
                values: new object[,]
                {
                    { "calendar", "Compliance calendar", "Track renewals, reports, training, affirmations, deliverables, and policy reviews.", "planned" },
                    { "cmmc", "CMMC readiness tracker", "Track Level 1 and Level 2 controls, evidence, SSP, POA&M, assets, and affirmations.", "planned" },
                    { "company-profile", "Company compliance profile", "Capture UEI, CAGE, SAM, NAICS, certifications, roles, and data posture.", "planned" },
                    { "contract-intake", "Contract and clause intake", "Collect solicitations, contracts, flow-downs, wage determinations, and CUI guides.", "active" },
                    { "evidence-vault", "Evidence vault", "Tag evidence by obligation, contract, control, vendor, employee, and expiration date.", "planned" },
                    { "obligations", "Obligation dashboard", "Map clauses to required actions, owners, evidence, deadlines, and source links.", "seeded" },
                    { "reports", "Basic reports", "Generate obligation matrices, readiness reports, evidence packages, and risk dashboards.", "planned" },
                    { "subcontractors", "Subcontractor flow-down tracker", "Track flow-down clauses, CMMC status, insurance, NDAs, CUI access, and workshare.", "planned" }
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                schema: "gccs",
                table: "mvp_modules",
                keyColumn: "key",
                keyValue: "calendar");

            migrationBuilder.DeleteData(
                schema: "gccs",
                table: "mvp_modules",
                keyColumn: "key",
                keyValue: "cmmc");

            migrationBuilder.DeleteData(
                schema: "gccs",
                table: "mvp_modules",
                keyColumn: "key",
                keyValue: "company-profile");

            migrationBuilder.DeleteData(
                schema: "gccs",
                table: "mvp_modules",
                keyColumn: "key",
                keyValue: "contract-intake");

            migrationBuilder.DeleteData(
                schema: "gccs",
                table: "mvp_modules",
                keyColumn: "key",
                keyValue: "evidence-vault");

            migrationBuilder.DeleteData(
                schema: "gccs",
                table: "mvp_modules",
                keyColumn: "key",
                keyValue: "obligations");

            migrationBuilder.DeleteData(
                schema: "gccs",
                table: "mvp_modules",
                keyColumn: "key",
                keyValue: "reports");

            migrationBuilder.DeleteData(
                schema: "gccs",
                table: "mvp_modules",
                keyColumn: "key",
                keyValue: "subcontractors");
        }
    }
}
