using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace darsakApi.Migrations
{
    /// <inheritdoc />
    public partial class AddTagColumnToPost : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "Tags",
                table: "Posts",
                newName: "Tag");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "Tag",
                table: "Posts",
                newName: "Tags");
        }
    }
}
