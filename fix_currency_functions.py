#!/usr/bin/env python3
"""
Script to add FormatCurrency functions to views that need them
"""

import re
import os

# Function block to add
FUNCTIONS_BLOCK = """
@functions {
    public string FormatCurrency(decimal amount)
    {
        var settings = ViewData["CurrencySettings"] as CurrencySettings;
        if (settings != null)
        {
            return CurrencyHelper.FormatCurrency(amount, settings);
        }
        return $"K {amount:N2}";
    }
    public string CurrencySymbol()
    {
        return ViewData["CurrencySymbol"]?.ToString() ?? "K";
    }
}
"""

# Files that need the functions block
files = [
    "Views/Requisitions/PendingApprovals.cshtml",
    "Views/Requisitions/Index.cshtml",
    "Views/Admin/Index.cshtml",
    "Views/Requisitions/Details.cshtml",
    "Views/AiImport/DeleteInvoice.cshtml",
    "Views/AiImport/DeletePayment.cshtml",
    "Views/AiImport/ViewInvoice.cshtml",
    "Views/Requisitions/Approve.cshtml",
    "Views/AiImport/ReviewPayments.cshtml",
    "Views/AiImport/ReviewInvoices.cshtml",
    "Views/Invoices/Delete.cshtml",
    "Views/AiImport/EditPayment.cshtml",
    "Views/AiImport/ViewPayment.cshtml",
    "Views/Payments/Delete.cshtml",
    "Views/Invoices/Edit.cshtml",
    "Views/Payments/Edit.cshtml",
    "Views/Payments/ManageAllocations.cshtml",
    "Views/Payments/Details.cshtml",
    "Views/Invoices/Details.cshtml",
    "Views/Invoices/Overdue.cshtml",
    "Views/Payments/Index.cshtml",
    "Views/PurchaseOrders/Index.cshtml",
    "Views/Invoices/Index.cshtml",
    "Views/PurchaseOrders/CreateFromRequisition.cshtml",
    "Views/PurchaseOrders/Details.cshtml",
    "Views/PurchaseOrders/ReceiveGoods.cshtml",
]

def add_functions_to_file(filepath):
    """Add @functions block to a file if it doesn't already have one"""
    if not os.path.exists(filepath):
        print(f"✗ File not found: {filepath}")
        return False
    
    with open(filepath, 'r', encoding='utf-8') as f:
        content = f.read()
    
    # Check if functions block already exists
    if '@functions {' in content and 'FormatCurrency' in content:
        print(f"⊙ Already has functions: {filepath}")
        return True
    
    # Find the first @{ block and add functions after it
    # Pattern: @{ ... }
    pattern = r'(@\{[^}]*\})'
    match = re.search(pattern, content, re.DOTALL)
    
    if match:
        # Insert functions block after the first @{ } block
        insert_pos = match.end()
        new_content = content[:insert_pos] + FUNCTIONS_BLOCK + content[insert_pos:]
        
        with open(filepath, 'w', encoding='utf-8') as f:
            f.write(new_content)
        
        print(f"✓ Added functions to: {filepath}")
        return True
    else:
        # No @{ block found, add at the beginning
        new_content = FUNCTIONS_BLOCK + "\n" + content
        
        with open(filepath, 'w', encoding='utf-8') as f:
            f.write(new_content)
        
        print(f"✓ Added functions at start: {filepath}")
        return True

def main():
    print("Adding FormatCurrency functions to views...\n")
    
    success_count = 0
    for filepath in files:
        if add_functions_to_file(filepath):
            success_count += 1
    
    print(f"\n✅ Complete! Updated {success_count}/{len(files)} files.")

if __name__ == "__main__":
    main()

