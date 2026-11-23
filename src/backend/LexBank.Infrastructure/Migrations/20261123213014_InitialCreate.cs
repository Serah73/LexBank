using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace LexBank.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Rule",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Pattern = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Category = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    Sign = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: false),
                    Priority = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Rule", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Users",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Email = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    PasswordHash = table.Column<string>(type: "nvarchar(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Users", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Transaction",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Date = table.Column<DateOnly>(type: "date", nullable: false),
                    Description = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Amount = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    Balance = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    Bank = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    Category = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    RuleId = table.Column<int>(type: "int", nullable: true),
                    Corrected = table.Column<bool>(type: "bit", nullable: false),
                    UserId = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Transaction", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Transaction_Rule_RuleId",
                        column: x => x.RuleId,
                        principalTable: "Rule",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_Transaction_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.InsertData(
                table: "Rule",
                columns: new[] { "Id", "Category", "Pattern", "Priority", "Sign" },
                values: new object[,]
                {
                    { 101, "Interest", "interés", 1, "Income" },
                    { 102, "Interest", "intereses", 1, "Income" },
                    { 201, "Salary", "nómina", 2, "Income" },
                    { 202, "Salary", "salario", 2, "Income" },
                    { 301, "Savings", "cuenta remunerada", 3, "Expense" },
                    { 401, "SavingsWithdrawal", "cuenta remunerada", 4, "Income" },
                    { 501, "Internal", "traspaso", 5, "Any" },
                    { 601, "Groceries", "supermercado*", 6, "Expense" },
                    { 602, "Groceries", "hiper*", 6, "Expense" },
                    { 603, "Groceries", "minimarket", 6, "Expense" },
                    { 604, "Groceries", "mercado", 6, "Expense" },
                    { 605, "Groceries", "frutería", 6, "Expense" },
                    { 606, "Groceries", "carnicería", 6, "Expense" },
                    { 607, "Groceries", "pescadería", 6, "Expense" },
                    { 608, "Groceries", "panadería", 6, "Expense" },
                    { 609, "Groceries", "horno", 6, "Expense" },
                    { 610, "Groceries", "*alcampo*", 6, "Expense" },
                    { 611, "Groceries", "*hiperdino*", 6, "Expense" },
                    { 612, "Groceries", "*carrefour*", 6, "Expense" },
                    { 613, "Groceries", "lidl", 6, "Expense" },
                    { 614, "Groceries", "dia", 6, "Expense" },
                    { 615, "Groceries", "*mercadona*", 6, "Expense" },
                    { 616, "Groceries", "5 oceanos", 6, "Expense" },
                    { 701, "Restaurants", "restaurante*", 7, "Expense" },
                    { 702, "Restaurants", "cafetería*", 7, "Expense" },
                    { 703, "Restaurants", "bar", 7, "Expense" },
                    { 704, "Restaurants", "pizzería", 7, "Expense" },
                    { 705, "Restaurants", "burger", 7, "Expense" },
                    { 706, "Restaurants", "comida", 7, "Expense" },
                    { 707, "Restaurants", "cena", 7, "Expense" },
                    { 708, "Restaurants", "desayuno", 7, "Expense" },
                    { 709, "Restaurants", "bocata", 7, "Expense" },
                    { 710, "Restaurants", "*mcdonald*", 7, "Expense" },
                    { 711, "Restaurants", "mc donalds", 7, "Expense" },
                    { 712, "Restaurants", "burger king", 7, "Expense" },
                    { 801, "Clothing", "moda", 8, "Expense" },
                    { 802, "Clothing", "textil", 8, "Expense" },
                    { 803, "Clothing", "boutique", 8, "Expense" },
                    { 804, "Clothing", "zara", 8, "Expense" },
                    { 805, "Clothing", "*springfield*", 8, "Expense" },
                    { 806, "Clothing", "*primark*", 8, "Expense" },
                    { 807, "Clothing", "*bershka*", 8, "Expense" },
                    { 901, "Footwear", "zapatería", 9, "Expense" },
                    { 902, "Footwear", "calzado", 9, "Expense" },
                    { 1001, "Jewelry", "joyería", 10, "Expense" },
                    { 1002, "Jewelry", "relojería", 10, "Expense" },
                    { 1003, "Jewelry", "*pandora*", 10, "Expense" },
                    { 1101, "Home", "ferretería", 11, "Expense" },
                    { 1102, "Home", "bricolaje", 11, "Expense" },
                    { 1103, "Home", "muebles", 11, "Expense" },
                    { 1104, "Home", "ikea", 11, "Expense" },
                    { 1105, "Home", "*leroy*", 11, "Expense" },
                    { 1201, "Health", "farmacia*", 12, "Expense" },
                    { 1202, "Health", "fcia", 12, "Expense" },
                    { 1203, "Health", "médico", 12, "Expense" },
                    { 1204, "Health", "clínica", 12, "Expense" },
                    { 1205, "Health", "dental", 12, "Expense" },
                    { 1206, "Health", "óptica", 12, "Expense" },
                    { 1207, "Health", "hospital", 12, "Expense" },
                    { 1301, "Transport", "parking", 13, "Expense" },
                    { 1302, "Transport", "aparcamiento", 13, "Expense" },
                    { 1303, "Transport", "gasolinera", 13, "Expense" },
                    { 1304, "Transport", "combustible*", 13, "Expense" },
                    { 1305, "Transport", "taxi", 13, "Expense" },
                    { 1306, "Transport", "itv", 13, "Expense" },
                    { 1307, "Transport", "peaje", 13, "Expense" },
                    { 1308, "Transport", "guagua", 13, "Expense" },
                    { 1309, "Transport", "*tgas*", 13, "Expense" },
                    { 1310, "Transport", "*titsa*", 13, "Expense" },
                    { 1311, "Transport", "*renfe*", 13, "Expense" },
                    { 1401, "Travel", "airline*", 14, "Expense" },
                    { 1402, "Travel", "hotel*", 14, "Expense" },
                    { 1403, "Travel", "*airbnb*", 14, "Expense" },
                    { 1404, "Travel", "aeropuerto", 14, "Expense" },
                    { 1405, "Travel", "booking", 14, "Expense" },
                    { 1406, "Travel", "bkg", 14, "Expense" },
                    { 1407, "Travel", "*vueling*", 14, "Expense" },
                    { 1408, "Travel", "*binter*", 14, "Expense" },
                    { 1409, "Travel", "*balearia*", 14, "Expense" },
                    { 1410, "Travel", "sixt", 14, "Expense" },
                    { 1501, "Pets", "veterinario", 15, "Expense" },
                    { 1502, "Pets", "mascota", 15, "Expense" },
                    { 1503, "Pets", "*kiwoko*", 15, "Expense" },
                    { 1601, "Cash", "cajero", 16, "Expense" },
                    { 1602, "Cash", "retirada", 16, "Expense" },
                    { 1701, "TransfersOut", "bizum", 17, "Expense" },
                    { 1702, "TransfersOut", "trf", 17, "Expense" },
                    { 1703, "TransfersOut", "transferencia", 17, "Expense" },
                    { 1801, "TransfersIn", "bizum", 18, "Income" },
                    { 1802, "TransfersIn", "trf", 18, "Income" },
                    { 1803, "TransfersIn", "transferencia", 18, "Income" },
                    { 1804, "TransfersIn", "devolución", 18, "Income" }
                });

            migrationBuilder.CreateIndex(
                name: "IX_Transaction_RuleId",
                table: "Transaction",
                column: "RuleId");

            migrationBuilder.CreateIndex(
                name: "IX_Transaction_UserId",
                table: "Transaction",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_Users_Email",
                table: "Users",
                column: "Email",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Transaction");

            migrationBuilder.DropTable(
                name: "Rule");

            migrationBuilder.DropTable(
                name: "Users");
        }
    }
}
