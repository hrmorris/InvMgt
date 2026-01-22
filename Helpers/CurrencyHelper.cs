using InvoiceManagement.Services;

namespace InvoiceManagement.Helpers
{
    public static class CurrencyHelper
    {
        public static string FormatCurrency(decimal amount, CurrencySettings settings)
        {
            // Format the number with the specified decimal places
            var formatString = $"F{settings.DecimalPlaces}";
            var formattedAmount = amount.ToString(formatString, System.Globalization.CultureInfo.InvariantCulture);

            // Split into integer and decimal parts
            var parts = formattedAmount.Split('.');
            var integerPart = parts[0];
            var decimalPart = parts.Length > 1 ? parts[1] : "";

            // Add thousands separator
            if (!string.IsNullOrEmpty(settings.ThousandsSeparator) && integerPart.Length > 3)
            {
                var reversedInteger = new string(integerPart.Reverse().ToArray());
                var groups = new List<string>();
                
                for (int i = 0; i < reversedInteger.Length; i += 3)
                {
                    var length = Math.Min(3, reversedInteger.Length - i);
                    groups.Add(reversedInteger.Substring(i, length));
                }
                
                integerPart = string.Join(settings.ThousandsSeparator, groups.Select(g => new string(g.Reverse().ToArray())).Reverse());
            }

            // Combine with decimal separator
            var finalAmount = decimalPart.Length > 0 
                ? $"{integerPart}{settings.DecimalSeparator}{decimalPart}"
                : integerPart;

            // Add currency symbol based on position
            if (settings.Position == "before")
            {
                return $"{settings.Symbol} {finalAmount}";
            }
            else
            {
                return $"{finalAmount} {settings.Symbol}";
            }
        }
    }
}

