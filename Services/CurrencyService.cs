using System.Globalization;

namespace InvoiceManagement.Services
{
    public class CurrencyService : ICurrencyService
    {
        private readonly IAdminService _adminService;

        public CurrencyService(IAdminService adminService)
        {
            _adminService = adminService;
        }

        public async Task<string> GetCurrencySymbolAsync()
        {
            return await _adminService.GetSettingValueAsync("CurrencySymbol") ?? "K";
        }

        public async Task<string> GetCurrencyCodeAsync()
        {
            return await _adminService.GetSettingValueAsync("CurrencyCode") ?? "PGK";
        }

        public async Task<string> GetCurrencyNameAsync()
        {
            return await _adminService.GetSettingValueAsync("CurrencyName") ?? "Papua New Guinean Kina";
        }

        public async Task<CurrencySettings> GetCurrencySettingsAsync()
        {
            var code = await _adminService.GetSettingValueAsync("CurrencyCode") ?? "PGK";
            var symbol = await _adminService.GetSettingValueAsync("CurrencySymbol") ?? "K";
            var name = await _adminService.GetSettingValueAsync("CurrencyName") ?? "Papua New Guinean Kina";
            var position = await _adminService.GetSettingValueAsync("CurrencyPosition") ?? "before";
            var decimalPlacesStr = await _adminService.GetSettingValueAsync("DecimalPlaces") ?? "2";
            var thousandsSeparator = await _adminService.GetSettingValueAsync("ThousandsSeparator") ?? ",";
            var decimalSeparator = await _adminService.GetSettingValueAsync("DecimalSeparator") ?? ".";

            int.TryParse(decimalPlacesStr, out int decimalPlaces);
            if (decimalPlaces < 0) decimalPlaces = 2;
            if (decimalPlaces > 3) decimalPlaces = 3;

            return new CurrencySettings
            {
                Code = code,
                Symbol = symbol,
                Name = name,
                Position = position,
                DecimalPlaces = decimalPlaces,
                ThousandsSeparator = thousandsSeparator,
                DecimalSeparator = decimalSeparator
            };
        }

        public async Task<string> FormatAmountAsync(decimal amount)
        {
            var settings = await GetCurrencySettingsAsync();

            // Format the number with the specified decimal places and separators
            var formatString = $"N{settings.DecimalPlaces}";
            var formattedAmount = amount.ToString(formatString, CultureInfo.InvariantCulture);

            // Replace separators based on settings
            formattedAmount = formattedAmount.Replace(",", "|TEMP|"); // Temporary placeholder
            formattedAmount = formattedAmount.Replace(".", settings.DecimalSeparator);
            formattedAmount = formattedAmount.Replace("|TEMP|", settings.ThousandsSeparator);

            // Add currency symbol based on position
            if (settings.Position == "before")
            {
                return $"{settings.Symbol} {formattedAmount}";
            }
            else
            {
                return $"{formattedAmount} {settings.Symbol}";
            }
        }
    }
}

