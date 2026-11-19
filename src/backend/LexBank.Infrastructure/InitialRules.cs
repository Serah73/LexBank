using LexBank.Domain;

namespace LexBank.Infrastructure;

// Priority 1 is the highest. A category's patterns share its priority; categories follow the order of the story's annex.
// A rule's id is its priority times 100 plus its position in the category's list, so append new patterns at the end of the list.
public static class InitialRules
{
    public static IReadOnlyList<(int Id, Rule Rule)> All { get; } = Build(
        (1, Category.Interest, AmountSign.Income, ["interés", "intereses"]),
        (2, Category.Salary, AmountSign.Income, ["nómina", "salario"]),
        (3, Category.Savings, AmountSign.Expense, ["cuenta remunerada"]),
        (4, Category.SavingsWithdrawal, AmountSign.Income, ["cuenta remunerada"]),
        (5, Category.Internal, AmountSign.Any, ["traspaso"]),
        (6, Category.Groceries, AmountSign.Expense,
            ["supermercado*", "hiper*", "minimarket", "mercado", "frutería", "carnicería", "pescadería", "panadería", "horno",
             "*alcampo*", "*hiperdino*", "*carrefour*", "lidl", "dia", "*mercadona*", "5 oceanos"]),
        (7, Category.Restaurants, AmountSign.Expense,
            ["restaurante*", "cafetería*", "bar", "pizzería", "burger", "comida", "cena", "desayuno", "bocata",
             "*mcdonald*", "mc donalds", "burger king"]),
        (8, Category.Clothing, AmountSign.Expense, ["moda", "textil", "boutique", "zara", "*springfield*", "*primark*", "*bershka*"]),
        (9, Category.Footwear, AmountSign.Expense, ["zapatería", "calzado"]),
        (10, Category.Jewelry, AmountSign.Expense, ["joyería", "relojería", "*pandora*"]),
        (11, Category.Home, AmountSign.Expense, ["ferretería", "bricolaje", "muebles", "ikea", "*leroy*"]),
        (12, Category.Health, AmountSign.Expense, ["farmacia*", "fcia", "médico", "clínica", "dental", "óptica", "hospital"]),
        (13, Category.Transport, AmountSign.Expense,
            ["parking", "aparcamiento", "gasolinera", "combustible*", "taxi", "itv", "peaje", "guagua",
             "*tgas*", "*titsa*", "*renfe*"]),
        (14, Category.Travel, AmountSign.Expense,
            ["airline*", "hotel*", "*airbnb*", "aeropuerto", "booking", "bkg", "*vueling*", "*binter*", "*balearia*", "sixt"]),
        (15, Category.Pets, AmountSign.Expense, ["veterinario", "mascota", "*kiwoko*"]),
        (16, Category.Cash, AmountSign.Expense, ["cajero", "retirada"]),
        (17, Category.TransfersOut, AmountSign.Expense, ["bizum", "trf", "transferencia"]),
        (18, Category.TransfersIn, AmountSign.Income, ["bizum", "trf", "transferencia", "devolución"]));

    private static List<(int Id, Rule Rule)> Build(params (int Priority, Category Category, AmountSign Sign, string[] Patterns)[] groups) =>
        groups.SelectMany(g => g.Patterns.Select((p, position) =>
            (g.Priority * 100 + position + 1, new Rule(p, g.Category, g.Sign, g.Priority)))).ToList();
}
