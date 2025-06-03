using CsvHelper.Configuration.Attributes;

namespace sberbank2beancount
{
    /// <summary>
    /// Transaction entry from CSV file
    /// </summary>
    internal class TransactionEntry
    {
        [Name("Дата операции")]
        public string? Date { get; set; }
        [Name("Сумма в валюте счёта")]
        public string? TotalValue { get; set; }
        [Name("Категория")]
        public string? Category { get; set; }
        [Name("Описание операции")]
        public string? Description { get; set; }
    }
}
