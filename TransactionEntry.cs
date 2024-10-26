using CsvHelper.Configuration.Attributes;

namespace sberbank2beancount
{
    /// <summary>
    /// Transaction entry from CSV file
    /// </summary>
    internal class TransactionEntry
    {
        [Name("ДАТА ОПЕРАЦИИ")]
        public string? Date { get; set; }
        [Name("СУММА В РУБЛЯХ")]
        public string? TotalValue { get; set; }
        [Name("КАТЕГОРИЯ")]
        public string? Category { get; set; }
        [Name("Описание операции")]
        public string? Description { get; set; }
    }
}
