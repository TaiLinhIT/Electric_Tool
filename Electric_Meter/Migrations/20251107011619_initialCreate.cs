using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Electric_Meter.Migrations
{
    /// <inheritdoc />
    public partial class initialCreate : Migration
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
                name: "devices",
                columns: table => new
                {
                    devid = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    name = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    activeid = table.Column<int>(type: "int", nullable: false),
                    typeid = table.Column<int>(type: "int", nullable: false),
                    ifshow = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_devices", x => x.devid);
                });

            migrationBuilder.CreateTable(
                name: "dv_ElectricDataTemp",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    IdMachine = table.Column<int>(type: "int", nullable: false),
                    Ia = table.Column<double>(type: "float", nullable: true),
                    Ib = table.Column<double>(type: "float", nullable: true),
                    Ic = table.Column<double>(type: "float", nullable: true),
                    Pt = table.Column<double>(type: "float", nullable: true),
                    Pa = table.Column<double>(type: "float", nullable: true),
                    Pb = table.Column<double>(type: "float", nullable: true),
                    Pc = table.Column<double>(type: "float", nullable: true),
                    Ua = table.Column<double>(type: "float", nullable: true),
                    Ub = table.Column<double>(type: "float", nullable: true),
                    Uc = table.Column<double>(type: "float", nullable: true),
                    Exp = table.Column<double>(type: "float", nullable: true),
                    Imp = table.Column<double>(type: "float", nullable: true),
                    TotalElectric = table.Column<double>(type: "float", nullable: true),
                    UploadDate = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_dv_ElectricDataTemp", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "dv_FactoryAddress_Configs",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Factory = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Assembling = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_dv_FactoryAddress_Configs", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "dv_Machine",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Port = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Line = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Baudrate = table.Column<int>(type: "int", nullable: false),
                    Address = table.Column<int>(type: "int", nullable: false),
                    LineCode = table.Column<string>(type: "nvarchar(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_dv_Machine", x => x.Id);
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
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ActiveType");

            migrationBuilder.DropTable(
                name: "controlcode");

            migrationBuilder.DropTable(
                name: "devices");

            migrationBuilder.DropTable(
                name: "dv_ElectricDataTemp");

            migrationBuilder.DropTable(
                name: "dv_FactoryAddress_Configs");

            migrationBuilder.DropTable(
                name: "dv_Machine");

            migrationBuilder.DropTable(
                name: "SensorData");
        }
    }
}
