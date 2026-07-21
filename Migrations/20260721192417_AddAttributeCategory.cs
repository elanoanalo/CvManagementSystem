using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CvManagementSystem.Migrations
{
    /// <inheritdoc />
    public partial class AddAttributeCategory : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "Category",
                table: "AttributeDefinitions",
                type: "integer",
                nullable: false,
                defaultValue: 0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Category",
                table: "AttributeDefinitions");
        }
    }
}
