using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace QuotesApi.Migrations
{
    /// <inheritdoc />
    public partial class SeedFromDbSetupSql : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(DbSetupSeedSql.Script);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                IF OBJECT_ID(N'[dbo].[QuoteTags]', N'U') IS NOT NULL
                    DELETE FROM [QuoteTags];

                IF OBJECT_ID(N'[dbo].[QuoteCategories]', N'U') IS NOT NULL
                    DELETE FROM [QuoteCategories];

                IF OBJECT_ID(N'[dbo].[RefreshTokens]', N'U') IS NOT NULL
                    DELETE FROM [RefreshTokens];

                IF OBJECT_ID(N'[dbo].[Quotes]', N'U') IS NOT NULL
                    DELETE FROM [Quotes];

                IF OBJECT_ID(N'[dbo].[Users]', N'U') IS NOT NULL
                    DELETE FROM [Users];

                IF OBJECT_ID(N'[dbo].[Tags]', N'U') IS NOT NULL
                    DELETE FROM [Tags];

                IF OBJECT_ID(N'[dbo].[Categories]', N'U') IS NOT NULL
                    DELETE FROM [Categories];

                IF OBJECT_ID(N'[dbo].[QuoteTags]', N'U') IS NOT NULL
                    DROP TABLE [QuoteTags];

                IF OBJECT_ID(N'[dbo].[QuoteCategories]', N'U') IS NOT NULL
                    DROP TABLE [QuoteCategories];

                IF OBJECT_ID(N'[dbo].[Tags]', N'U') IS NOT NULL
                    DROP TABLE [Tags];

                IF OBJECT_ID(N'[dbo].[Categories]', N'U') IS NOT NULL
                    DROP TABLE [Categories];
                """);
        }
    }
}
