using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ConstructionFinance.Migrations
{
    /// <inheritdoc />
    public partial class AddFeaturesV1 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "IsDefault",
                table: "Sites",
                type: "INTEGER",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "IsDefault",
                table: "Sites");
        }
    }
}
