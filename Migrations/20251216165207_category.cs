using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Courses.Migrations
{
    /// <inheritdoc />
    public partial class category : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "CourseCategories",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CourseCategories", x => x.Id);
                });

            migrationBuilder.InsertData(
                table: "CourseCategories",
                columns: new[] { "Id", "Name" },
                values: new object[,]
                {
                    { 1, "программирование" },
                    { 2, "веб-дизайн" },
                    { 3, "дизайн" },
                    { 4, "архитектура" },
                    { 5, "музыка" },
                    { 6, "спорт" },
                    { 7, "фотография" },
                    { 8, "мода" },
                    { 9, "литература" },
                    { 10, "подготовка к экзамену" },
                    { 11, "еда" }
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "CourseCategories");
        }
    }
}
