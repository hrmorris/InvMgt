#!/bin/bash

# Script to update all views to use dynamic currency formatting

# List of view files to update
views=(
    "Views/AiImport/ReviewPayments.cshtml"
    "Views/Requisitions/PendingApprovals.cshtml"
    "Views/Requisitions/Approve.cshtml"
    "Views/Invoices/Edit.cshtml"
    "Views/Payments/ManageAllocations.cshtml"
    "Views/Payments/Details.cshtml"
    "Views/Payments/Edit.cshtml"
    "Views/AiImport/EditPayment.cshtml"
    "Views/AiImport/DeletePayment.cshtml"
    "Views/AiImport/ViewPayment.cshtml"
    "Views/AiImport/DeleteInvoice.cshtml"
    "Views/AiImport/ViewInvoice.cshtml"
    "Views/AiImport/ReviewInvoices.cshtml"
    "Views/Invoices/Details.cshtml"
    "Views/Invoices/Overdue.cshtml"
    "Views/Payments/Index.cshtml"
    "Views/Payments/Delete.cshtml"
    "Views/Invoices/Delete.cshtml"
    "Views/PurchaseOrders/Index.cshtml"
    "Views/Invoices/Index.cshtml"
    "Views/Requisitions/Details.cshtml"
    "Views/Requisitions/Index.cshtml"
    "Views/PurchaseOrders/CreateFromRequisition.cshtml"
    "Views/PurchaseOrders/Details.cshtml"
    "Views/PurchaseOrders/ReceiveGoods.cshtml"
    "Views/Admin/Index.cshtml"
)

for view in "${views[@]}"; do
    if [ -f "$view" ]; then
        echo "Processing $view..."
        
        # Add currency functions import at the beginning if not already present
        if ! grep -q "@functions {" "$view" 2>/dev/null; then
            # Find the first @{ block and add functions after it
            sed -i.bak '/@{/,/^}/a\
\
@functions {\
    public string FormatCurrency(decimal amount)\
    {\
        var settings = ViewData["CurrencySettings"] as CurrencySettings;\
        if (settings != null)\
        {\
            return CurrencyHelper.FormatCurrency(amount, settings);\
        }\
        return $"K {amount:N2}";\
    }\
    public string CurrencySymbol()\
    {\
        return ViewData["CurrencySymbol"]?.ToString() ?? "K";\
    }\
}\
' "$view"
        fi
        
        # Replace currency patterns
        # Pattern 1: $@variable.ToString("N2") or ("N0") etc
        sed -i.bak2 -E 's/\$@([a-zA-Z0-9_.()]+)\.ToString\("N[0-9]"\)/@FormatCurrency(\1)/g' "$view"
        
        # Pattern 2: $@Model.variable with ToString
        sed -i.bak3 -E 's/\$@(Model\.[a-zA-Z0-9_]+)\.ToString\("N[0-9]"\)/@FormatCurrency(\1)/g' "$view"
        
        # Pattern 3: $@(expression).ToString
        sed -i.bak4 -E 's/\$@\(([^)]+)\)\.ToString\("N[0-9]"\)/@FormatCurrency(\1)/g' "$view"
        
        echo "✓ Updated $view"
    else
        echo "✗ File not found: $view"
    fi
done

# Clean up backup files
find Views -name "*.bak*" -delete

echo ""
echo "✅ Currency view update complete!"
echo "All views have been updated to use dynamic currency formatting."

