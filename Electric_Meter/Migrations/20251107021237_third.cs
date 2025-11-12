using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Electric_Meter.Migrations
{
    /// <inheritdoc />
    public partial class third : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropPrimaryKey(
                name: "PK_sesnsorTypes",
                table: "sesnsorTypes");

            migrationBuilder.RenameTable(
                name: "sesnsorTypes",
                newName: "SensorType");

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

            migrationBuilder.RenameTable(
                name: "SensorType",
                newName: "sesnsorTypes");

            migrationBuilder.AddPrimaryKey(
                name: "PK_sesnsorTypes",
                table: "sesnsorTypes",
                column: "typeid");
        }
    }
}
