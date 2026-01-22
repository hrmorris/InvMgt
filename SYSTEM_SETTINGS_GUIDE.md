# System Settings Guide

## Overview
The System Settings module provides a centralized configuration management system for your Invoice Management application. All application-wide settings can be managed from a single interface without requiring code changes or server restarts.

## Access System Settings

**Navigation:** Admin Dashboard → System Settings

**Required Role:** Administrator

## Features

### 1. Settings Organization
Settings are organized into logical categories for easy management:

- **General** - Application-wide settings
- **Company** - Company information for documents
- **Invoice** - Invoice-related configurations
- **Payment** - Payment processing settings
- **Procurement** - Requisition and purchase order settings
- **Email** - Email server and notification settings
- **Notification** - Alert and notification preferences
- **Security** - Security and authentication settings
- **API** - External API configurations
- **Finance** - Financial and accounting settings

### 2. Initialize Default Settings
When you first access System Settings, you can initialize a comprehensive set of default settings with one click:

1. Click **"Initialize Defaults"** button
2. Confirm the initialization
3. All standard settings will be created with sensible default values
4. Existing settings will not be overwritten

### 3. Quick Edit Settings
Each setting can be edited directly from the settings list:

1. Navigate to the category containing the setting
2. Modify the value in the input field
3. Click the checkmark button to save
4. The setting takes effect immediately

**Input Types:**
- Boolean settings show as dropdown (Enabled/Disabled)
- Numeric settings use number input
- Text settings use text input

### 4. Create Custom Settings

To add a new setting:

1. Click **"Add New Setting"** button
2. Fill in the form:
   - **Category** - Select from predefined categories
   - **Setting Key** - Unique identifier (use camelCase or snake_case)
   - **Setting Value** - The actual value
   - **Description** - Explain what the setting does
3. Click **"Create Setting"**

**Naming Conventions:**
- Use descriptive, clear names
- Examples: `InvoiceDueDays`, `MaxUploadSize`, `EnableAutoBackup`
- Avoid spaces and special characters

### 5. Delete Settings

To remove a setting:

1. Find the setting in its category
2. Click the trash icon (🗑️)
3. Confirm deletion

⚠️ **Warning:** Deleting a setting that is used by the application may cause errors. Only delete custom settings you created.

## Default Settings Reference

### General Settings

| Setting Key | Default Value | Description |
|------------|---------------|-------------|
| ApplicationName | Invoice Management System | Application display name |
| DateFormat | dd/MM/yyyy | Date format for entire application |
| CurrencySymbol | $ | Currency symbol for financial displays |
| TimeZone | Pacific/Auckland | Application timezone |

### Company Settings

| Setting Key | Default Value | Description |
|------------|---------------|-------------|
| CompanyName | Your Company Name | Company name for documents |
| CompanyAddress | 123 Main Street, City, Country | Company address |
| CompanyPhone | +1234567890 | Company phone number |
| CompanyEmail | info@company.com | Company email address |
| CompanyWebsite | www.company.com | Company website URL |

### Invoice Settings

| Setting Key | Default Value | Description |
|------------|---------------|-------------|
| InvoiceDueDays | 30 | Default days until invoice due |
| InvoicePrefix | INV- | Prefix for invoice numbers |
| TaxRate | 15 | Default tax rate percentage |
| EnableAutoInvoiceNumbers | true | Auto-generate invoice numbers |

### Payment Settings

| Setting Key | Default Value | Description |
|------------|---------------|-------------|
| PaymentPrefix | PAY- | Prefix for payment numbers |
| EnablePaymentReminders | true | Send automatic payment reminders |
| PaymentReminderDays | 7 | Days before due date for reminders |

### Procurement Settings

| Setting Key | Default Value | Description |
|------------|---------------|-------------|
| RequisitionPrefix | REQ- | Prefix for requisition numbers |
| PurchaseOrderPrefix | PO- | Prefix for purchase order numbers |
| RequireApproval | true | Require approval for procurement |
| ApprovalThreshold | 1000 | Amount requiring additional approval |

### Email Settings

| Setting Key | Default Value | Description |
|------------|---------------|-------------|
| SMTPServer | smtp.gmail.com | SMTP server address |
| SMTPPort | 587 | SMTP server port |
| EmailFrom | noreply@company.com | Default sender email |
| EnableEmailNotifications | true | Send email notifications |

### Security Settings

| Setting Key | Default Value | Description |
|------------|---------------|-------------|
| SessionTimeout | 120 | Session timeout in minutes |
| PasswordMinLength | 8 | Minimum password length |
| EnableAuditLogging | true | Log all user actions |

### API Settings

| Setting Key | Default Value | Description |
|------------|---------------|-------------|
| MaxUploadSize | 7 | Max file upload size in MB |
| AIModel | gemini-2.5-pro | Google AI model for document processing |

## Best Practices

### 1. Document Custom Settings
When creating custom settings, always add a clear description explaining:
- What the setting controls
- Valid values or ranges
- Impact on the application

### 2. Test Changes
After modifying critical settings (like email or API configurations), test the affected functionality to ensure it works correctly.

### 3. Backup Important Settings
Before making major changes, note down current values of important settings so you can restore them if needed.

### 4. Use Appropriate Categories
Place new settings in the most logical category. If none fit, use "General" category.

### 5. Naming Consistency
Follow existing naming patterns:
- Use camelCase: `MaxUploadSize`, `EnableFeature`
- Be descriptive: `InvoiceDueDays` not `Days`
- Avoid abbreviations unless universally understood

### 6. Boolean Values
For true/false settings, always use lowercase:
- ✅ `true` or `false`
- ❌ `True`, `TRUE`, `1`, `0`

### 7. Numeric Values
Store numeric values as plain numbers without units:
- ✅ `30` (use description to note it's days)
- ❌ `30 days`

## Common Use Cases

### Customize Invoice Numbering
1. Find `InvoicePrefix` in Invoice category
2. Change from `INV-` to your preferred prefix (e.g., `2025-INV-`)
3. Save the change
4. New invoices will use the new prefix

### Update Company Information
1. Navigate to Company category
2. Update all company fields with your actual information
3. These values appear on PDF invoices and reports

### Configure Payment Terms
1. Find `InvoiceDueDays` in Invoice category
2. Set to your standard payment terms (e.g., 14, 30, 60 days)
3. All new invoices will use this default

### Adjust File Upload Limits
1. Find `MaxUploadSize` in API category
2. Set to desired size in MB (max 7 MB for AI processing)
3. Files larger than this will be rejected

### Configure Approval Thresholds
1. Find `ApprovalThreshold` in Procurement category
2. Set the monetary amount requiring manager approval
3. Requisitions above this amount will require additional approval

## Troubleshooting

### Setting Not Taking Effect
- **Check saved value:** Ensure the change was saved (green success message appears)
- **Refresh page:** Some settings may require a page refresh
- **Check application code:** Verify the application is reading the setting

### Cannot Delete Setting
- **System settings:** Some core settings cannot be deleted
- **In use:** Settings referenced by the application should not be deleted

### Boolean Setting Not Working
- **Verify format:** Must be lowercase `true` or `false`
- **Case sensitive:** `True` or `FALSE` won't work

### Lost Settings
- **Re-initialize:** Click "Initialize Defaults" to restore standard settings
- **Create manually:** Recreate custom settings if needed

## Audit Trail

All setting changes are tracked with:
- **Modified Date** - When the setting was last changed
- **Modified By** - User who made the change
- **Timestamp** - Exact date and time of modification

This information is displayed in the settings table and helps track configuration changes over time.

## Security Considerations

### Access Control
- Only administrators can access System Settings
- All changes are logged in the audit trail
- Session-based authentication prevents unauthorized access

### Sensitive Settings
Handle these settings with care:
- Email server credentials
- API keys
- Security thresholds
- File size limits

### Regular Review
Periodically review your settings to ensure:
- Values are still appropriate
- Unused custom settings are removed
- Security settings are up to date

## Future Enhancements

Planned improvements for System Settings:
- **Import/Export** - Backup and restore settings
- **Setting Templates** - Pre-configured setting packages
- **Validation Rules** - Prevent invalid values
- **Setting Groups** - Bundle related settings
- **Change History** - Track all historical changes
- **Environment Settings** - Different settings for dev/prod

## Support

If you need help with System Settings:
1. Review this guide
2. Check default values reference
3. Test in a non-production environment first
4. Contact your system administrator

---

**Last Updated:** November 8, 2025  
**Version:** 1.0  
**Module:** System Settings

