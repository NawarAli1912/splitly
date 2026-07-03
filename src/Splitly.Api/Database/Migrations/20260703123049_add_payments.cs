using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Splitly.Api.Database.Migrations
{
    /// <inheritdoc />
    public partial class add_payments : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "payments",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    from_participant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    to_participant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    amount = table.Column<long>(type: "bigint", nullable: false),
                    paid_on = table.Column<DateOnly>(type: "date", nullable: false),
                    expense_group_id = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_payments", x => x.id);
                    table.ForeignKey(
                        name: "fk_payments_expense_groups_expense_group_id",
                        column: x => x.expense_group_id,
                        principalTable: "expense_groups",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "ix_payments_expense_group_id",
                table: "payments",
                column: "expense_group_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "payments");
        }
    }
}
