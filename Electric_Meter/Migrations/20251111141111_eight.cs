using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Electric_Meter.Migrations
{
    /// <inheritdoc />
    public partial class eight : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropPrimaryKey(
                name: "PK_SesnsorType",
                table: "SesnsorType");

            migrationBuilder.RenameTable(
                name: "SesnsorType",
                newName: "SensorType");

            migrationBuilder.AddColumn<string>(
                name: "assembling",
                table: "devices",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddPrimaryKey(
                name: "PK_SensorType",
                table: "SensorType",
                column: "typeid");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropPrimaryKey(
                name: "PK_SensorType",
                table: "SensorType");

            migrationBuilder.DropColumn(
                name: "assembling",
                table: "devices");

            migrationBuilder.RenameTable(
                name: "SensorType",
                newName: "SesnsorType");

            migrationBuilder.AddPrimaryKey(
                name: "PK_SesnsorType",
                table: "SesnsorType",
                column: "typeid");
        }
    }
}
