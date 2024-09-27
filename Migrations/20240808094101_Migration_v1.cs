using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CrawlerMVC.Migrations
{
    /// <inheritdoc />
    public partial class Migration_v1 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "PriceListSubscribers");

            migrationBuilder.DropTable(
                name: "SubscriptionTypes");

            migrationBuilder.CreateTable(
                name: "NotificationTypes",
                columns: table => new
                {
                    NotificationTypeId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    NotificationName = table.Column<string>(type: "nvarchar(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_NotificationTypes", x => x.NotificationTypeId);
                });

            migrationBuilder.CreateTable(
                name: "Subscribers",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    UserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    NotificationTypeId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SubscriptionTargetId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SubscriptionWord = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Active = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Subscribers", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "SubscriptionTargets",
                columns: table => new
                {
                    SubscriptionTargetId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TargetName = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    StoreId = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SubscriptionTargets", x => x.SubscriptionTargetId);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "NotificationTypes");

            migrationBuilder.DropTable(
                name: "Subscribers");

            migrationBuilder.DropTable(
                name: "SubscriptionTargets");

            migrationBuilder.CreateTable(
                name: "PriceListSubscribers",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Active = table.Column<bool>(type: "bit", nullable: false),
                    SubscriptionTypeId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SubscriptionWord = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    UserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PriceListSubscribers", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "SubscriptionTypes",
                columns: table => new
                {
                    SubscriptionTypeId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SubscriptionName = table.Column<string>(type: "nvarchar(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SubscriptionTypes", x => x.SubscriptionTypeId);
                });
        }
    }
}
