
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Buyer.Infrastructure.Data.Migrations
{
    public partial class Baseline_ExistingV2 : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Existing V2 database schema.
            // Do not recreate existing tables.
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Do not drop existing database tables.
        }
    }
}
