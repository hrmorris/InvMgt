# Currency System Guide

## Overview

The Invoice Management System now includes a comprehensive currency management system with **150+ world currencies**, including Papua New Guinea Kina (PGK) and all major international currencies.

## Key Features

### ✅ Complete Currency Database
- **150+ currencies** from around the world
- Organized by region (Pacific, Asia, Middle East, Africa, Europe, Americas)
- Each currency includes:
  - Code (e.g., PGK, USD, EUR)
  - Full name (e.g., Papua New Guinean Kina)
  - Symbol (e.g., K, $, €)
  - Region (e.g., Papua New Guinea)

### ✅ Pacific Region Currencies
Special emphasis on Pacific nations:
- **Papua New Guinean Kina (PGK)** - Symbol: K
- Fijian Dollar (FJD)
- Samoan Tala (WST)
- Tongan Paʻanga (TOP)
- Vanuatu Vatu (VUV)
- Solomon Islands Dollar (SBD)
- CFP Franc (XPF)
- Australian Dollar (AUD)
- New Zealand Dollar (NZD)

### ✅ Currency Formatting
Customize how currency amounts are displayed:
- **Symbol Position**: Before or after amount
- **Decimal Places**: 0-3 decimal places
- **Thousands Separator**: Comma, period, space, or none
- **Decimal Separator**: Period or comma

#### Examples of Formatting Options:
```
K 1,000.50   (Before, 2 decimals, comma separator)
1.000,50 K   (After, 2 decimals, period separator)
1 000.5 K    (Before, 1 decimal, space separator)
1000 K       (After, 0 decimals, no separator)
```

## How to Access

### Admin Navigation
1. Log in as an admin user
2. Go to **Admin Dashboard**
3. Click **System Settings**
4. Click the green **Currency Settings** button

### Direct URL
```
http://localhost:5000/Admin/CurrencySettings
```

## Currency Settings Interface

### Main Features

#### 1. Current Currency Display
- Shows your currently selected currency
- Displays the symbol, code, and full name
- Example format preview

#### 2. Quick Currency Change
- Click "Change Currency" button
- Modal with all 150+ currencies
- Search by name, code, or region
- Filter by region
- Click any currency card to select it

#### 3. Currency Formatting Options
Configure how amounts are displayed:
- **Symbol Position**: Before/After amount
- **Decimal Places**: 0-3 places
- **Thousands Separator**: Choose separator style
- **Decimal Separator**: Period or comma
- Live preview of formatting

## Search and Filter Features

### Search Capabilities
Type in the search box to find currencies by:
- Currency name (e.g., "Kina", "Dollar")
- Currency code (e.g., "PGK", "USD")
- Region (e.g., "Papua New Guinea", "Pacific")

### Region Filter
Filter currencies by geographic region:
- All Regions (150+ currencies)
- Pacific (9 currencies including PGK)
- Asia (24 currencies)
- Middle East (14 currencies)
- Africa (20+ currencies)
- Europe (18 currencies)
- Americas (20+ currencies)

## Default Currency: Papua New Guinea Kina

The system is pre-configured with Papua New Guinea Kina as the default currency:

```
Currency Code: PGK
Currency Symbol: K
Currency Name: Papua New Guinean Kina
Symbol Position: Before amount
Decimal Places: 2
Thousands Separator: ,
Decimal Separator: .
```

**Example Display**: `K 1,234.56`

## Changing Currency

### Step-by-Step Process

1. **Access Currency Settings**
   - Admin → System Settings → Currency Settings

2. **Open Currency Selector**
   - Click "Change Currency" button

3. **Search or Browse**
   - Use search box to find your currency
   - Or filter by region
   - Or browse the full list

4. **Select Currency**
   - Click on the currency card
   - Selected currency is highlighted

5. **Confirm**
   - Click "Change Currency" button in modal footer
   - Success message confirms the change

### Currency Formatting

1. **Access Formatting Options**
   - Below current currency display
   - Form with formatting dropdowns

2. **Configure Options**
   - Choose symbol position
   - Select decimal places
   - Pick thousands separator
   - Choose decimal separator

3. **Preview**
   - Live preview shows how amounts will look

4. **Save**
   - Click "Save Formatting" button
   - Success message confirms changes

## Popular Currency Examples

### Asia-Pacific
```
PGK - Papua New Guinean Kina (K)
AUD - Australian Dollar (A$)
NZD - New Zealand Dollar (NZ$)
FJD - Fijian Dollar (FJ$)
INR - Indian Rupee (₹)
JPY - Japanese Yen (¥)
CNY - Chinese Yuan (¥)
```

### International
```
USD - US Dollar ($)
EUR - Euro (€)
GBP - British Pound (£)
CHF - Swiss Franc (CHF)
CAD - Canadian Dollar (C$)
```

### Middle East
```
AED - UAE Dirham (د.إ)
SAR - Saudi Riyal (﷼)
QAR - Qatari Riyal (﷼)
```

### Africa
```
ZAR - South African Rand (R)
NGN - Nigerian Naira (₦)
EGP - Egyptian Pound (£)
KES - Kenyan Shilling (KSh)
```

## System Settings Integration

The currency system integrates with existing system settings:

### Settings Keys
- `CurrencyCode` - Three-letter currency code
- `CurrencySymbol` - Currency symbol character(s)
- `CurrencyName` - Full currency name
- `CurrencyPosition` - Symbol position (before/after)
- `DecimalPlaces` - Number of decimal places
- `ThousandsSeparator` - Thousands separator character
- `DecimalSeparator` - Decimal separator character

### Initialization
When you click "Initialize Default Settings", the system automatically creates these currency settings with Papua New Guinea Kina as the default.

## Technical Details

### Currency Model
Location: `Models/Currency.cs`

Properties:
- `Code`: Three-letter ISO code
- `Name`: Full currency name
- `Symbol`: Display symbol
- `Region`: Geographic region

### Helper Methods
```csharp
Currency.GetAllCurrencies()      // Returns all 150+ currencies
Currency.GetByCode("PGK")        // Get specific currency
Currency.GetByRegion("Pacific")  // Get currencies by region
Currency.GetAllRegions()         // Get all region names
```

### Controller Actions
Location: `Controllers/AdminController.cs`

- `CurrencySettings()` - Display currency settings page
- `UpdateCurrency(string currencyCode)` - Change currency
- `UpdateCurrencyFormat(...)` - Update formatting options

## User Experience Features

### Visual Enhancements
- **Hover Effects**: Currency cards lift on hover
- **Selection Highlight**: Selected currency has blue border
- **Search Highlighting**: Real-time search results
- **Live Preview**: Format changes show immediately

### Responsive Design
- Grid layout for currency cards
- Mobile-friendly modal
- Scrollable currency list
- Accessible keyboard navigation

### User Feedback
- Success messages for all changes
- Error handling with clear messages
- Confirmation dialogs where appropriate
- Loading states during updates

## Use Cases

### For Papua New Guinea Organizations
1. Set PGK as default (already configured)
2. Configure to show "K" before amounts
3. Use 2 decimal places for toea
4. Comma thousands separator (K 1,000.50)

### For International Organizations
1. Choose primary operating currency
2. Set formatting to match local standards
3. Easy switching between currencies if needed

### For Multi-Currency Operations
1. Change currency per transaction as needed
2. Quick access to all major currencies
3. Search functionality for fast selection

## Best Practices

### Currency Selection
- Choose currency matching your region
- Verify symbol displays correctly
- Test formatting with sample amounts

### Formatting Standards
- Follow local accounting standards
- Consider invoice recipient expectations
- Use consistent formatting across system

### System Administration
- Review currency settings during setup
- Update only when officially changing currency
- Audit trail tracks all currency changes

## Troubleshooting

### Currency Not Showing Correctly
- Check system settings for correct code
- Verify symbol character is supported
- Try refreshing the browser

### Symbol Not Displaying
- Some symbols require special fonts
- Browser may not support all characters
- Consider using code instead (e.g., "PGK")

### Formatting Issues
- Clear browser cache
- Re-save formatting settings
- Check decimal/thousands separators aren't same

## Future Enhancements

Potential additions:
- Exchange rate integration
- Multi-currency invoicing
- Historical exchange rates
- Currency conversion calculator
- Custom currency symbols
- Regional format presets

## Support

For issues or questions:
1. Check system settings
2. Review audit logs
3. Contact system administrator
4. Refer to this documentation

---

**Last Updated**: November 2025  
**Version**: 1.0  
**Default Currency**: Papua New Guinean Kina (PGK)

