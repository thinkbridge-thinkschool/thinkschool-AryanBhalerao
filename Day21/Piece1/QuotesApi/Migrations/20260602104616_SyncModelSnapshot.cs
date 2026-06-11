using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace QuotesApi.Migrations
{
    /// <inheritdoc />
    public partial class SyncModelSnapshot : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Tags, Categories, QuoteTags, QuoteCategories were already created by
            // the SeedFromDbSetupSql migration's raw SQL. This migration exists only
            // to bring the EF Core model snapshot in sync with those tables so that
            // EF Core no longer reports pending model changes.
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Tables are owned by SeedFromDbSetupSql; nothing to roll back here.
        }
    }
}
