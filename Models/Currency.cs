using System.Collections.Generic;
using System.Linq;

namespace InvoiceManagement.Models
{
    public class Currency
    {
        public string Code { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string Symbol { get; set; } = string.Empty;
        public string Region { get; set; } = string.Empty;

        // Comprehensive list of world currencies
        public static List<Currency> GetAllCurrencies()
        {
            return new List<Currency>
            {
                // Major Currencies
                new Currency { Code = "USD", Name = "US Dollar", Symbol = "$", Region = "United States" },
                new Currency { Code = "EUR", Name = "Euro", Symbol = "€", Region = "European Union" },
                new Currency { Code = "GBP", Name = "British Pound", Symbol = "£", Region = "United Kingdom" },
                new Currency { Code = "JPY", Name = "Japanese Yen", Symbol = "¥", Region = "Japan" },
                new Currency { Code = "CHF", Name = "Swiss Franc", Symbol = "CHF", Region = "Switzerland" },
                new Currency { Code = "CAD", Name = "Canadian Dollar", Symbol = "C$", Region = "Canada" },
                new Currency { Code = "AUD", Name = "Australian Dollar", Symbol = "A$", Region = "Australia" },
                new Currency { Code = "NZD", Name = "New Zealand Dollar", Symbol = "NZ$", Region = "New Zealand" },

                // Pacific Region (including Papua New Guinea)
                new Currency { Code = "PGK", Name = "Papua New Guinean Kina", Symbol = "K", Region = "Papua New Guinea" },
                new Currency { Code = "FJD", Name = "Fijian Dollar", Symbol = "FJ$", Region = "Fiji" },
                new Currency { Code = "WST", Name = "Samoan Tala", Symbol = "WS$", Region = "Samoa" },
                new Currency { Code = "TOP", Name = "Tongan Paʻanga", Symbol = "T$", Region = "Tonga" },
                new Currency { Code = "VUV", Name = "Vanuatu Vatu", Symbol = "VT", Region = "Vanuatu" },
                new Currency { Code = "SBD", Name = "Solomon Islands Dollar", Symbol = "SI$", Region = "Solomon Islands" },
                new Currency { Code = "XPF", Name = "CFP Franc", Symbol = "₣", Region = "French Pacific Territories" },

                // Asia
                new Currency { Code = "CNY", Name = "Chinese Yuan", Symbol = "¥", Region = "China" },
                new Currency { Code = "INR", Name = "Indian Rupee", Symbol = "₹", Region = "India" },
                new Currency { Code = "KRW", Name = "South Korean Won", Symbol = "₩", Region = "South Korea" },
                new Currency { Code = "SGD", Name = "Singapore Dollar", Symbol = "S$", Region = "Singapore" },
                new Currency { Code = "HKD", Name = "Hong Kong Dollar", Symbol = "HK$", Region = "Hong Kong" },
                new Currency { Code = "MYR", Name = "Malaysian Ringgit", Symbol = "RM", Region = "Malaysia" },
                new Currency { Code = "THB", Name = "Thai Baht", Symbol = "฿", Region = "Thailand" },
                new Currency { Code = "IDR", Name = "Indonesian Rupiah", Symbol = "Rp", Region = "Indonesia" },
                new Currency { Code = "PHP", Name = "Philippine Peso", Symbol = "₱", Region = "Philippines" },
                new Currency { Code = "VND", Name = "Vietnamese Dong", Symbol = "₫", Region = "Vietnam" },
                new Currency { Code = "PKR", Name = "Pakistani Rupee", Symbol = "Rs", Region = "Pakistan" },
                new Currency { Code = "BDT", Name = "Bangladeshi Taka", Symbol = "৳", Region = "Bangladesh" },
                new Currency { Code = "LKR", Name = "Sri Lankan Rupee", Symbol = "Rs", Region = "Sri Lanka" },
                new Currency { Code = "MMK", Name = "Myanmar Kyat", Symbol = "K", Region = "Myanmar" },
                new Currency { Code = "KHR", Name = "Cambodian Riel", Symbol = "៛", Region = "Cambodia" },
                new Currency { Code = "LAK", Name = "Lao Kip", Symbol = "₭", Region = "Laos" },
                new Currency { Code = "BND", Name = "Brunei Dollar", Symbol = "B$", Region = "Brunei" },
                new Currency { Code = "TWD", Name = "Taiwan Dollar", Symbol = "NT$", Region = "Taiwan" },
                new Currency { Code = "MOP", Name = "Macanese Pataca", Symbol = "MOP$", Region = "Macau" },
                new Currency { Code = "KPW", Name = "North Korean Won", Symbol = "₩", Region = "North Korea" },
                new Currency { Code = "MNT", Name = "Mongolian Tugrik", Symbol = "₮", Region = "Mongolia" },

                // Middle East
                new Currency { Code = "AED", Name = "UAE Dirham", Symbol = "د.إ", Region = "United Arab Emirates" },
                new Currency { Code = "SAR", Name = "Saudi Riyal", Symbol = "﷼", Region = "Saudi Arabia" },
                new Currency { Code = "QAR", Name = "Qatari Riyal", Symbol = "﷼", Region = "Qatar" },
                new Currency { Code = "KWD", Name = "Kuwaiti Dinar", Symbol = "د.ك", Region = "Kuwait" },
                new Currency { Code = "BHD", Name = "Bahraini Dinar", Symbol = "د.ب", Region = "Bahrain" },
                new Currency { Code = "OMR", Name = "Omani Rial", Symbol = "﷼", Region = "Oman" },
                new Currency { Code = "JOD", Name = "Jordanian Dinar", Symbol = "د.ا", Region = "Jordan" },
                new Currency { Code = "ILS", Name = "Israeli New Shekel", Symbol = "₪", Region = "Israel" },
                new Currency { Code = "LBP", Name = "Lebanese Pound", Symbol = "ل.ل", Region = "Lebanon" },
                new Currency { Code = "SYP", Name = "Syrian Pound", Symbol = "£", Region = "Syria" },
                new Currency { Code = "IQD", Name = "Iraqi Dinar", Symbol = "ع.د", Region = "Iraq" },
                new Currency { Code = "IRR", Name = "Iranian Rial", Symbol = "﷼", Region = "Iran" },
                new Currency { Code = "TRY", Name = "Turkish Lira", Symbol = "₺", Region = "Turkey" },
                new Currency { Code = "YER", Name = "Yemeni Rial", Symbol = "﷼", Region = "Yemen" },

                // Africa
                new Currency { Code = "ZAR", Name = "South African Rand", Symbol = "R", Region = "South Africa" },
                new Currency { Code = "NGN", Name = "Nigerian Naira", Symbol = "₦", Region = "Nigeria" },
                new Currency { Code = "EGP", Name = "Egyptian Pound", Symbol = "£", Region = "Egypt" },
                new Currency { Code = "KES", Name = "Kenyan Shilling", Symbol = "KSh", Region = "Kenya" },
                new Currency { Code = "GHS", Name = "Ghanaian Cedi", Symbol = "₵", Region = "Ghana" },
                new Currency { Code = "TZS", Name = "Tanzanian Shilling", Symbol = "TSh", Region = "Tanzania" },
                new Currency { Code = "UGX", Name = "Ugandan Shilling", Symbol = "USh", Region = "Uganda" },
                new Currency { Code = "MAD", Name = "Moroccan Dirham", Symbol = "د.م", Region = "Morocco" },
                new Currency { Code = "ETB", Name = "Ethiopian Birr", Symbol = "Br", Region = "Ethiopia" },
                new Currency { Code = "XOF", Name = "West African CFA Franc", Symbol = "Fr", Region = "West Africa" },
                new Currency { Code = "XAF", Name = "Central African CFA Franc", Symbol = "Fr", Region = "Central Africa" },
                new Currency { Code = "MUR", Name = "Mauritian Rupee", Symbol = "Rs", Region = "Mauritius" },
                new Currency { Code = "SCR", Name = "Seychellois Rupee", Symbol = "Rs", Region = "Seychelles" },
                new Currency { Code = "ZMW", Name = "Zambian Kwacha", Symbol = "ZK", Region = "Zambia" },
                new Currency { Code = "BWP", Name = "Botswana Pula", Symbol = "P", Region = "Botswana" },
                new Currency { Code = "NAD", Name = "Namibian Dollar", Symbol = "N$", Region = "Namibia" },
                new Currency { Code = "MZN", Name = "Mozambican Metical", Symbol = "MT", Region = "Mozambique" },
                new Currency { Code = "AOA", Name = "Angolan Kwanza", Symbol = "Kz", Region = "Angola" },
                new Currency { Code = "RWF", Name = "Rwandan Franc", Symbol = "Fr", Region = "Rwanda" },

                // Europe
                new Currency { Code = "NOK", Name = "Norwegian Krone", Symbol = "kr", Region = "Norway" },
                new Currency { Code = "SEK", Name = "Swedish Krona", Symbol = "kr", Region = "Sweden" },
                new Currency { Code = "DKK", Name = "Danish Krone", Symbol = "kr", Region = "Denmark" },
                new Currency { Code = "ISK", Name = "Icelandic Króna", Symbol = "kr", Region = "Iceland" },
                new Currency { Code = "CZK", Name = "Czech Koruna", Symbol = "Kč", Region = "Czech Republic" },
                new Currency { Code = "PLN", Name = "Polish Zloty", Symbol = "zł", Region = "Poland" },
                new Currency { Code = "HUF", Name = "Hungarian Forint", Symbol = "Ft", Region = "Hungary" },
                new Currency { Code = "RON", Name = "Romanian Leu", Symbol = "lei", Region = "Romania" },
                new Currency { Code = "BGN", Name = "Bulgarian Lev", Symbol = "лв", Region = "Bulgaria" },
                new Currency { Code = "HRK", Name = "Croatian Kuna", Symbol = "kn", Region = "Croatia" },
                new Currency { Code = "RUB", Name = "Russian Ruble", Symbol = "₽", Region = "Russia" },
                new Currency { Code = "UAH", Name = "Ukrainian Hryvnia", Symbol = "₴", Region = "Ukraine" },
                new Currency { Code = "BYN", Name = "Belarusian Ruble", Symbol = "Br", Region = "Belarus" },
                new Currency { Code = "RSD", Name = "Serbian Dinar", Symbol = "дин", Region = "Serbia" },
                new Currency { Code = "MKD", Name = "Macedonian Denar", Symbol = "ден", Region = "North Macedonia" },
                new Currency { Code = "ALL", Name = "Albanian Lek", Symbol = "L", Region = "Albania" },

                // Americas
                new Currency { Code = "MXN", Name = "Mexican Peso", Symbol = "$", Region = "Mexico" },
                new Currency { Code = "BRL", Name = "Brazilian Real", Symbol = "R$", Region = "Brazil" },
                new Currency { Code = "ARS", Name = "Argentine Peso", Symbol = "$", Region = "Argentina" },
                new Currency { Code = "CLP", Name = "Chilean Peso", Symbol = "$", Region = "Chile" },
                new Currency { Code = "COP", Name = "Colombian Peso", Symbol = "$", Region = "Colombia" },
                new Currency { Code = "PEN", Name = "Peruvian Sol", Symbol = "S/", Region = "Peru" },
                new Currency { Code = "VES", Name = "Venezuelan Bolívar", Symbol = "Bs", Region = "Venezuela" },
                new Currency { Code = "UYU", Name = "Uruguayan Peso", Symbol = "$", Region = "Uruguay" },
                new Currency { Code = "PYG", Name = "Paraguayan Guaraní", Symbol = "₲", Region = "Paraguay" },
                new Currency { Code = "BOB", Name = "Bolivian Boliviano", Symbol = "Bs", Region = "Bolivia" },
                new Currency { Code = "CRC", Name = "Costa Rican Colón", Symbol = "₡", Region = "Costa Rica" },
                new Currency { Code = "GTQ", Name = "Guatemalan Quetzal", Symbol = "Q", Region = "Guatemala" },
                new Currency { Code = "HNL", Name = "Honduran Lempira", Symbol = "L", Region = "Honduras" },
                new Currency { Code = "NIO", Name = "Nicaraguan Córdoba", Symbol = "C$", Region = "Nicaragua" },
                new Currency { Code = "PAB", Name = "Panamanian Balboa", Symbol = "B/.", Region = "Panama" },
                new Currency { Code = "DOP", Name = "Dominican Peso", Symbol = "RD$", Region = "Dominican Republic" },
                new Currency { Code = "JMD", Name = "Jamaican Dollar", Symbol = "J$", Region = "Jamaica" },
                new Currency { Code = "TTD", Name = "Trinidad and Tobago Dollar", Symbol = "TT$", Region = "Trinidad and Tobago" },
                new Currency { Code = "BSD", Name = "Bahamian Dollar", Symbol = "B$", Region = "Bahamas" },
                new Currency { Code = "BBD", Name = "Barbadian Dollar", Symbol = "Bds$", Region = "Barbados" },
                new Currency { Code = "XCD", Name = "East Caribbean Dollar", Symbol = "EC$", Region = "Eastern Caribbean" },

                // Cryptocurrencies (optional)
                new Currency { Code = "BTC", Name = "Bitcoin", Symbol = "₿", Region = "Digital" },
                new Currency { Code = "ETH", Name = "Ethereum", Symbol = "Ξ", Region = "Digital" },
            };
        }

        public static Currency? GetByCode(string code)
        {
            return GetAllCurrencies().FirstOrDefault(c => c.Code.Equals(code, StringComparison.OrdinalIgnoreCase));
        }

        public static List<Currency> GetByRegion(string region)
        {
            return GetAllCurrencies()
                .Where(c => c.Region.Contains(region, StringComparison.OrdinalIgnoreCase))
                .OrderBy(c => c.Name)
                .ToList();
        }

        public static List<string> GetAllRegions()
        {
            return GetAllCurrencies()
                .Select(c => c.Region)
                .Distinct()
                .OrderBy(r => r)
                .ToList();
        }
    }
}

