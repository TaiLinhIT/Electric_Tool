using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Electric_Meter.Migrations
{
    /// <inheritdoc />
    public partial class ebx : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ActiveType",
                columns: table => new
                {
                    activeid = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    name = table.Column<string>(type: "nvarchar(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ActiveType", x => x.activeid);
                });

            migrationBuilder.CreateTable(
                name: "controlcode",
                columns: table => new
                {
                    codeid = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    devid = table.Column<int>(type: "int", nullable: false),
                    code = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    activeid = table.Column<int>(type: "int", nullable: false),
                    codetypeid = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    name = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    factor = table.Column<double>(type: "float", nullable: false),
                    typeid = table.Column<int>(type: "int", nullable: true),
                    high = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
                    low = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
                    ifshow = table.Column<int>(type: "int", nullable: true),
                    ifcal = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_controlcode", x => x.codeid);
                });

            migrationBuilder.CreateTable(
                name: "DailyConsumptionDTO",
                columns: table => new
                {
                    dayData = table.Column<DateTime>(type: "datetime2", nullable: false),
                    TotalDailyConsumption = table.Column<double>(type: "float", nullable: false)
                },
                constraints: table =>
                {
                });

            migrationBuilder.CreateTable(
                name: "devices",
                columns: table => new
                {
                    devid = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    address = table.Column<int>(type: "int", nullable: false),
                    name = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    port = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    assembling = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    baudrate = table.Column<int>(type: "int", nullable: false),
                    activeid = table.Column<int>(type: "int", nullable: false),
                    typeid = table.Column<int>(type: "int", nullable: false),
                    ifshow = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_devices", x => x.devid);
                });

            migrationBuilder.CreateTable(
                name: "LatestSensorByDeviceYear",
                columns: table => new
                {
                    devid = table.Column<int>(type: "int", nullable: false),
                    device_name = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    TotalValue = table.Column<double>(type: "float", nullable: false)
                },
                constraints: table =>
                {
                });

            migrationBuilder.CreateTable(
                name: "SensorData",
                columns: table => new
                {
                    logid = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    devid = table.Column<int>(type: "int", nullable: false),
                    codeid = table.Column<int>(type: "int", nullable: false),
                    value = table.Column<double>(type: "float", nullable: false),
                    day = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SensorData", x => x.logid);
                });

            migrationBuilder.CreateTable(
                name: "SensorType",
                columns: table => new
                {
                    typeid = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    name = table.Column<string>(type: "nvarchar(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SensorType", x => x.typeid);
                });

            migrationBuilder.CreateTable(
                name: "TotalConsumptionPercentageDeviceDTO",
                columns: table => new
                {
                    devid = table.Column<int>(type: "int", nullable: false),
                    DeviceName = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    TotalConsumption = table.Column<double>(type: "float", nullable: false),
                    Percentage = table.Column<double>(type: "float", nullable: false)
                },
                constraints: table =>
                {
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ActiveType");

            migrationBuilder.DropTable(
                name: "controlcode");

            migrationBuilder.DropTable(
                name: "DailyConsumptionDTO");

            migrationBuilder.DropTable(
                name: "devices");

            migrationBuilder.DropTable(
                name: "LatestSensorByDeviceYear");

            migrationBuilder.DropTable(
                name: "SensorData");

            migrationBuilder.DropTable(
                name: "SensorType");

            migrationBuilder.DropTable(
                name: "TotalConsumptionPercentageDeviceDTO");
        }
    }
}
