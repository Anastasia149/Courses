using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Courses.Migrations
{
    /// <inheritdoc />
    public partial class RemoveOldCategoryTable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Categories");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {

        }
    }
}
