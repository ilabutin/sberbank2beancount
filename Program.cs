using CsvHelper;
using CsvHelper.Configuration;
using System.Globalization;
using System.Text;
using sberbank2beancount;
using Tomlyn;
using Tomlyn.Model;

string configFile = args[0];
string outputFile = args[1];

Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);

var config = Toml.ToModel(File.ReadAllText(configFile, Encoding.UTF8));
TomlTable cardsTable = (TomlTable)config["cards"];
TomlTable categoriesTable = (TomlTable)config["categories"];
TomlTable dropStrings = (TomlTable)config["sberbank_drop"];

Dictionary<string, string> categoriesSubstrings = new Dictionary<string, string>();
foreach (var categoryPair in categoriesTable)
{
    if (categoryPair.Key.StartsWith("$$"))
    {
        categoriesSubstrings.Add(categoryPair.Key.Substring(2), (string)categoryPair.Value);
    }
}

NumberFormatInfo numberFormatInfo = new NumberFormatInfo();
numberFormatInfo.NumberDecimalSeparator = ".";
var csvConfig = new CsvConfiguration(CultureInfo.InvariantCulture)
{
    Delimiter = ";",
};
List<Transaction> transactions = new List<Transaction>();
for (int argN = 2; argN < args.Length; argN++)
{
    using (StreamReader reader = new StreamReader(args[argN], System.Text.Encoding.GetEncoding("utf-8")))
    {
        using (CsvReader csvReader = new CsvReader(reader, csvConfig))
        {
            var entries = csvReader.GetRecords<TransactionEntry>().ToList();
            Console.WriteLine($"{entries.Count} entries read");
            transactions.AddRange(
                entries.Select(ParseCsvEntry).Where(t => t.StatusOk).OrderBy(t => t.Date));
        }
    }
}

using StreamWriter writer = new StreamWriter(outputFile, false, Encoding.UTF8);
for (int i = 0; i < transactions.Count; i++)
{
    Transaction t = transactions[i];
    
    if (dropStrings.Values.Any(v => t.Description?.Contains((string)v) ?? false))
    {
        Console.WriteLine($"dropped: {t}");
        continue;
    }

    // Write header line
    string headerDescription = t.Description ?? "Прочее";
    // Write MCC if exists, otherwise 0
    string mcc = t.Mcc ?? "0";
    // Write main expense account
    string? cardNumber = t.CardNumber?.TrimStart('*');
    
    if (cardNumber == null || !cardsTable.TryGetValue(cardNumber, out object account))
    {
        account = "XX";
    }

    // Write category
    object category = "YY";
    if (t is { TotalValue: < 0, Description: not null } 
             && categoriesTable.TryGetValue(t.Description, out var foundCategory))
    {
        category = foundCategory;
    }
    else if (t is { TotalValue: < 0, Description: not null })
    {
        category = "Expenses:";
        foreach (string categorySubstring in categoriesSubstrings.Keys)
        {
            if (t.Description.Contains(categorySubstring))
            {
                category = categoriesSubstrings[categorySubstring];
            }
        } 
    }
    else if (t.TotalValue > 0)
    {
        category = "Income:";
    }
    else
    {
        category = "Expenses:";
    }
    
    writer.WriteLine($"{t.Date.ToString("yyyy-MM-dd")} * \"{headerDescription}\"");
    writer.WriteLine($"  mcc: {mcc}");
    writer.WriteLine($"  {account}     {t.TotalValue.ToString("F2", CultureInfo.InvariantCulture)} RUB");
    writer.WriteLine($"  {category}");
    writer.WriteLine();
}

Transaction ParseCsvEntry(TransactionEntry transactionEntry)
{
    DateOnly date = DateOnly.FromDateTime(DateTime.Now);
    if (transactionEntry.Date is { } d)
    {
        date = DateOnly.FromDateTime(DateTime.Parse(d));
    }

    decimal totalValue = 0.0M;
    if (transactionEntry.TotalValue is { } v)
    {
        totalValue = Convert.ToDecimal(v, numberFormatInfo);
    }

    string category = "";
    if (transactionEntry.Category is { } c)
    {
        category = c;
    }

    return new Transaction(date, "1418", totalValue, category,
        null, transactionEntry.Description, true);
}