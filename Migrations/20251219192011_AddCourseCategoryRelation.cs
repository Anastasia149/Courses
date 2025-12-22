using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Courses.Migrations
{
    /// <inheritdoc />
    public partial class AddCourseCategoryRelation : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Сначала добавляем новое поле CategoryId
            migrationBuilder.AddColumn<int>(
                name: "CategoryId",
                table: "Courses",
                type: "int",
                nullable: true);

            // Переносим данные из Category (строка) в CategoryId
            // Сопоставляем строковые значения категорий с ID из таблицы CourseCategories
            migrationBuilder.Sql(@"
                UPDATE c
                SET c.CategoryId = cc.Id
                FROM Courses c
                INNER JOIN CourseCategories cc ON LOWER(LTRIM(RTRIM(cc.Name))) = LOWER(LTRIM(RTRIM(c.Category)))
                WHERE c.Category IS NOT NULL AND c.Category != ''
            ");

            // Создаем индекс для CategoryId
            migrationBuilder.CreateIndex(
                name: "IX_Courses_CategoryId",
                table: "Courses",
                column: "CategoryId");

            // Добавляем внешний ключ
            migrationBuilder.AddForeignKey(
                name: "FK_Courses_CourseCategories_CategoryId",
                table: "Courses",
                column: "CategoryId",
                principalTable: "CourseCategories",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);

            // Только после переноса данных удаляем старое поле Category
            migrationBuilder.DropColumn(
                name: "Category",
                table: "Courses");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Удаляем внешний ключ
            migrationBuilder.DropForeignKey(
                name: "FK_Courses_CourseCategories_CategoryId",
                table: "Courses");

            // Удаляем индекс
            migrationBuilder.DropIndex(
                name: "IX_Courses_CategoryId",
                table: "Courses");

            // Восстанавливаем старое поле Category
            migrationBuilder.AddColumn<string>(
                name: "Category",
                table: "Courses",
                type: "nvarchar(max)",
                nullable: true);

            // Переносим данные обратно из CategoryId в Category (строка)
            migrationBuilder.Sql(@"
                UPDATE c
                SET c.Category = cc.Name
                FROM Courses c
                INNER JOIN CourseCategories cc ON cc.Id = c.CategoryId
                WHERE c.CategoryId IS NOT NULL
            ");

            // Удаляем новое поле CategoryId
            migrationBuilder.DropColumn(
                name: "CategoryId",
                table: "Courses");
        }
    }
}
