using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace darsakApi.Migrations
{
    /// <inheritdoc />
    public partial class UpdatePostToTeacherRelation : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Post_Materials_MaterialId",
                table: "Post");

            migrationBuilder.DropPrimaryKey(
                name: "PK_Post",
                table: "Post");

            migrationBuilder.RenameTable(
                name: "Post",
                newName: "Posts");

            migrationBuilder.RenameColumn(
                name: "MaterialId",
                table: "Posts",
                newName: "TeacherId");

            migrationBuilder.RenameIndex(
                name: "IX_Post_MaterialId",
                table: "Posts",
                newName: "IX_Posts_TeacherId");

            migrationBuilder.AddPrimaryKey(
                name: "PK_Posts",
                table: "Posts",
                column: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_Posts_Teachers_TeacherId",
                table: "Posts",
                column: "TeacherId",
                principalTable: "Teachers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Posts_Teachers_TeacherId",
                table: "Posts");

            migrationBuilder.DropPrimaryKey(
                name: "PK_Posts",
                table: "Posts");

            migrationBuilder.RenameTable(
                name: "Posts",
                newName: "Post");

            migrationBuilder.RenameColumn(
                name: "TeacherId",
                table: "Post",
                newName: "MaterialId");

            migrationBuilder.RenameIndex(
                name: "IX_Posts_TeacherId",
                table: "Post",
                newName: "IX_Post_MaterialId");

            migrationBuilder.AddPrimaryKey(
                name: "PK_Post",
                table: "Post",
                column: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_Post_Materials_MaterialId",
                table: "Post",
                column: "MaterialId",
                principalTable: "Materials",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
