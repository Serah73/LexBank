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
                    Category = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: true),
                    UserId = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Transaction", x => x.Id);
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
                    { 1, "Interest", "interés", 1, "Income" },
                    { 2, "Interest", "intereses", 1, "Income" },
                    { 3, "Salary", "nómina", 2, "Income" },
                    { 4, "Salary", "salario", 2, "Income" },
                    { 5, "Savings", "cuenta remunerada", 3, "Expense" },
                    { 6, "SavingsWithdrawal", "cuenta remunerada", 4, "Income" },
                    { 7, "Internal", "traspaso", 5, "Any" },
                    { 8, "Groceries", "supermercado*", 6, "Expense" },
                    { 9, "Groceries", "hiper*", 6, "Expense" },
                    { 10, "Groceries", "minimarket", 6, "Expense" },
                    { 11, "Groceries", "mercado", 6, "Expense" },
                    { 12, "Groceries", "frutería", 6, "Expense" },
                    { 13, "Groceries", "carnicería", 6, "Expense" },
                    { 14, "Groceries", "pescadería", 6, "Expense" },
                    { 15, "Groceries", "panadería", 6, "Expense" },
                    { 16, "Groceries", "horno", 6, "Expense" },
                    { 17, "Groceries", "*alcampo*", 6, "Expense" },
                    { 18, "Groceries", "*hiperdino*", 6, "Expense" },
                    { 19, "Groceries", "*carrefour*", 6, "Expense" },
                    { 20, "Groceries", "lidl", 6, "Expense" },
                    { 21, "Groceries", "dia", 6, "Expense" },
                    { 22, "Groceries", "*mercadona*", 6, "Expense" },
                    { 23, "Groceries", "5 oceanos", 6, "Expense" },
                    { 24, "Restaurants", "restaurante*", 7, "Expense" },
                    { 25, "Restaurants", "cafetería*", 7, "Expense" },
                    { 26, "Restaurants", "bar", 7, "Expense" },
                    { 27, "Restaurants", "pizzería", 7, "Expense" },
                    { 28, "Restaurants", "burger", 7, "Expense" },
                    { 29, "Restaurants", "comida", 7, "Expense" },
                    { 30, "Restaurants", "cena", 7, "Expense" },
                    { 31, "Restaurants", "desayuno", 7, "Expense" },
                    { 32, "Restaurants", "bocata", 7, "Expense" },
                    { 33, "Restaurants", "*mcdonald*", 7, "Expense" },
                    { 34, "Restaurants", "mc donalds", 7, "Expense" },
                    { 35, "Restaurants", "burger king", 7, "Expense" },
                    { 36, "Clothing", "moda", 8, "Expense" },
                    { 37, "Clothing", "textil", 8, "Expense" },
                    { 38, "Clothing", "boutique", 8, "Expense" },
                    { 39, "Clothing", "zara", 8, "Expense" },
                    { 40, "Clothing", "*springfield*", 8, "Expense" },
                    { 41, "Clothing", "*primark*", 8, "Expense" },
                    { 42, "Clothing", "*bershka*", 8, "Expense" },
                    { 43, "Footwear", "zapatería", 9, "Expense" },
                    { 44, "Footwear", "calzado", 9, "Expense" },
                    { 45, "Jewelry", "joyería", 10, "Expense" },
                    { 46, "Jewelry", "relojería", 10, "Expense" },
                    { 47, "Jewelry", "*pandora*", 10, "Expense" },
                    { 48, "Home", "ferretería", 11, "Expense" },
                    { 49, "Home", "bricolaje", 11, "Expense" },
                    { 50, "Home", "muebles", 11, "Expense" },
                    { 51, "Home", "ikea", 11, "Expense" },
                    { 52, "Home", "*leroy*", 11, "Expense" },
                    { 53, "Health", "farmacia*", 12, "Expense" },
                    { 54, "Health", "fcia", 12, "Expense" },
                    { 55, "Health", "médico", 12, "Expense" },
                    { 56, "Health", "clínica", 12, "Expense" },
                    { 57, "Health", "dental", 12, "Expense" },
                    { 58, "Health", "óptica", 12, "Expense" },
                    { 59, "Health", "hospital", 12, "Expense" },
                    { 60, "Transport", "parking", 13, "Expense" },
                    { 61, "Transport", "aparcamiento", 13, "Expense" },
                    { 62, "Transport", "gasolinera", 13, "Expense" },
                    { 63, "Transport", "combustible*", 13, "Expense" },
                    { 64, "Transport", "taxi", 13, "Expense" },
                    { 65, "Transport", "itv", 13, "Expense" },
                    { 66, "Transport", "peaje", 13, "Expense" },
                    { 67, "Transport", "guagua", 13, "Expense" },
                    { 68, "Transport", "*tgas*", 13, "Expense" },
                    { 69, "Transport", "*titsa*", 13, "Expense" },
                    { 70, "Transport", "*renfe*", 13, "Expense" },
                    { 71, "Travel", "airline*", 14, "Expense" },
                    { 72, "Travel", "hotel*", 14, "Expense" },
                    { 73, "Travel", "*airbnb*", 14, "Expense" },
                    { 74, "Travel", "aeropuerto", 14, "Expense" },
                    { 75, "Travel", "booking", 14, "Expense" },
                    { 76, "Travel", "bkg", 14, "Expense" },
                    { 77, "Travel", "*vueling*", 14, "Expense" },
                    { 78, "Travel", "*binter*", 14, "Expense" },
                    { 79, "Travel", "*balearia*", 14, "Expense" },
                    { 80, "Travel", "sixt", 14, "Expense" },
                    { 81, "Pets", "veterinario", 15, "Expense" },
                    { 82, "Pets", "mascota", 15, "Expense" },
                    { 83, "Pets", "*kiwoko*", 15, "Expense" },
                    { 84, "Cash", "cajero", 16, "Expense" },
                    { 85, "Cash", "retirada", 16, "Expense" },
                    { 86, "TransfersOut", "bizum", 17, "Expense" },
                    { 87, "TransfersOut", "trf", 17, "Expense" },
                    { 88, "TransfersOut", "transferencia", 17, "Expense" },
                    { 89, "TransfersIn", "bizum", 18, "Income" },
                    { 90, "TransfersIn", "trf", 18, "Income" },
                    { 91, "TransfersIn", "transferencia", 18, "Income" },
                    { 92, "TransfersIn", "devolución", 18, "Income" }
                });

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
                name: "Rule");

            migrationBuilder.DropTable(
                name: "Transaction");

            migrationBuilder.DropTable(
                name: "Users");
        }
    }
}
