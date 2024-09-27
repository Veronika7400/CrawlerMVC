using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CrawlerMVC.Migrations
{
    /// <inheritdoc />
    public partial class Migration_v2 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Frequency",
                table: "SubscriptionTargets",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Frequency",
                table: "SubscriptionTargets");
        }
    }
}
