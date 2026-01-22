namespace InvoiceManagement.Services
{
    public interface ICurrencyService
    {
        Task<string> GetCurrencySymbolAsync();
        Task<string> GetCurrencyCodeAsync();
        Task<string> GetCurrencyNameAsync();
        Task<string> FormatAmountAsync(decimal amount);
        Task<CurrencySettings> GetCurrencySettingsAsync();
    }

    public class CurrencySettings
    {
        public string Code { get; set; } = "PGK";
        public string Symbol { get; set; } = "K";
        public string Name { get; set; } = "Papua New Guinean Kina";
        public string Position { get; set; } = "before";
        public int DecimalPlaces { get; set; } = 2;
        public string ThousandsSeparator { get; set; } = ",";
        public string DecimalSeparator { get; set; } = ".";
    }
}

