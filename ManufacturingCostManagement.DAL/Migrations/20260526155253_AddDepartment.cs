using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ManufacturingCostManagement.DAL.Migrations
{
    /// <inheritdoc />
    public partial class AddDepartment : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "DepartmentId",
                table: "ProductionOrders",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "DepartmentId",
                table: "OverheadCosts",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "DepartmentId",
                table: "LaborCosts",
                type: "int",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "Departments",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Code = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Name = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    Manager = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: true),
                    Description = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Departments", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ProductionOrders_DepartmentId",
                table: "ProductionOrders",
                column: "DepartmentId");

            migrationBuilder.CreateIndex(
                name: "IX_OverheadCosts_DepartmentId",
                table: "OverheadCosts",
                column: "DepartmentId");

            migrationBuilder.CreateIndex(
                name: "IX_LaborCosts_DepartmentId",
                table: "LaborCosts",
                column: "DepartmentId");

            migrationBuilder.CreateIndex(
                name: "IX_Departments_Code",
                table: "Departments",
                column: "Code",
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_LaborCosts_Departments_DepartmentId",
                table: "LaborCosts",
                column: "DepartmentId",
                principalTable: "Departments",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_OverheadCosts_Departments_DepartmentId",
                table: "OverheadCosts",
                column: "DepartmentId",
                principalTable: "Departments",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_ProductionOrders_Departments_DepartmentId",
                table: "ProductionOrders",
                column: "DepartmentId",
                principalTable: "Departments",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_LaborCosts_Departments_DepartmentId",
                table: "LaborCosts");

            migrationBuilder.DropForeignKey(
                name: "FK_OverheadCosts_Departments_DepartmentId",
                table: "OverheadCosts");

            migrationBuilder.DropForeignKey(
                name: "FK_ProductionOrders_Departments_DepartmentId",
                table: "ProductionOrders");

            migrationBuilder.DropTable(
                name: "Departments");

            migrationBuilder.DropIndex(
                name: "IX_ProductionOrders_DepartmentId",
                table: "ProductionOrders");

            migrationBuilder.DropIndex(
                name: "IX_OverheadCosts_DepartmentId",
                table: "OverheadCosts");

            migrationBuilder.DropIndex(
                name: "IX_LaborCosts_DepartmentId",
                table: "LaborCosts");

            migrationBuilder.DropColumn(
                name: "DepartmentId",
                table: "ProductionOrders");

            migrationBuilder.DropColumn(
                name: "DepartmentId",
                table: "OverheadCosts");

            migrationBuilder.DropColumn(
                name: "DepartmentId",
                table: "LaborCosts");
        }
    }
}
