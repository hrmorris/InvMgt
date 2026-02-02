-- Enable Row Level Security (RLS) on all tables
-- Run this in Supabase SQL Editor

-- Enable RLS on all tables
ALTER TABLE public."BatchPaymentItems" ENABLE ROW LEVEL SECURITY;
ALTER TABLE public."BatchPayments" ENABLE ROW LEVEL SECURITY;
ALTER TABLE public."AuditLogs" ENABLE ROW LEVEL SECURITY;
ALTER TABLE public."UserRoles" ENABLE ROW LEVEL SECURITY;
ALTER TABLE public."Permissions" ENABLE ROW LEVEL SECURITY;
ALTER TABLE public."RolePermissions" ENABLE ROW LEVEL SECURITY;
ALTER TABLE public."PurchaseOrderItems" ENABLE ROW LEVEL SECURITY;
ALTER TABLE public."RequisitionItems" ENABLE ROW LEVEL SECURITY;
ALTER TABLE public."PaymentAllocations" ENABLE ROW LEVEL SECURITY;
ALTER TABLE public."ImportedDocuments" ENABLE ROW LEVEL SECURITY;
ALTER TABLE public."InvoiceItems" ENABLE ROW LEVEL SECURITY;
ALTER TABLE public."Payments" ENABLE ROW LEVEL SECURITY;
ALTER TABLE public."Invoices" ENABLE ROW LEVEL SECURITY;
ALTER TABLE public."Suppliers" ENABLE ROW LEVEL SECURITY;
ALTER TABLE public."PurchaseOrders" ENABLE ROW LEVEL SECURITY;
ALTER TABLE public."Requisitions" ENABLE ROW LEVEL SECURITY;
ALTER TABLE public."Users" ENABLE ROW LEVEL SECURITY;
ALTER TABLE public."Roles" ENABLE ROW LEVEL SECURITY;
ALTER TABLE public."SystemSettings" ENABLE ROW LEVEL SECURITY;
ALTER TABLE public."Customers" ENABLE ROW LEVEL SECURITY;
ALTER TABLE public."__EFMigrationsHistory" ENABLE ROW LEVEL SECURITY;
ALTER TABLE public."DataProtectionKeys" ENABLE ROW LEVEL SECURITY;

-- Create policies to allow the service role (your app backend) full access
-- The service role bypasses RLS by default, but we add policies for the postgres role

-- BatchPaymentItems
CREATE POLICY "Allow all for authenticated service" ON public."BatchPaymentItems" FOR ALL USING (true) WITH CHECK (true);

-- BatchPayments
CREATE POLICY "Allow all for authenticated service" ON public."BatchPayments" FOR ALL USING (true) WITH CHECK (true);

-- AuditLogs
CREATE POLICY "Allow all for authenticated service" ON public."AuditLogs" FOR ALL USING (true) WITH CHECK (true);

-- UserRoles
CREATE POLICY "Allow all for authenticated service" ON public."UserRoles" FOR ALL USING (true) WITH CHECK (true);

-- Permissions
CREATE POLICY "Allow all for authenticated service" ON public."Permissions" FOR ALL USING (true) WITH CHECK (true);

-- RolePermissions
CREATE POLICY "Allow all for authenticated service" ON public."RolePermissions" FOR ALL USING (true) WITH CHECK (true);

-- PurchaseOrderItems
CREATE POLICY "Allow all for authenticated service" ON public."PurchaseOrderItems" FOR ALL USING (true) WITH CHECK (true);

-- RequisitionItems
CREATE POLICY "Allow all for authenticated service" ON public."RequisitionItems" FOR ALL USING (true) WITH CHECK (true);

-- PaymentAllocations
CREATE POLICY "Allow all for authenticated service" ON public."PaymentAllocations" FOR ALL USING (true) WITH CHECK (true);

-- ImportedDocuments
CREATE POLICY "Allow all for authenticated service" ON public."ImportedDocuments" FOR ALL USING (true) WITH CHECK (true);

-- InvoiceItems
CREATE POLICY "Allow all for authenticated service" ON public."InvoiceItems" FOR ALL USING (true) WITH CHECK (true);

-- Payments
CREATE POLICY "Allow all for authenticated service" ON public."Payments" FOR ALL USING (true) WITH CHECK (true);

-- Invoices
CREATE POLICY "Allow all for authenticated service" ON public."Invoices" FOR ALL USING (true) WITH CHECK (true);

-- Suppliers
CREATE POLICY "Allow all for authenticated service" ON public."Suppliers" FOR ALL USING (true) WITH CHECK (true);

-- PurchaseOrders
CREATE POLICY "Allow all for authenticated service" ON public."PurchaseOrders" FOR ALL USING (true) WITH CHECK (true);

-- Requisitions
CREATE POLICY "Allow all for authenticated service" ON public."Requisitions" FOR ALL USING (true) WITH CHECK (true);

-- Users
CREATE POLICY "Allow all for authenticated service" ON public."Users" FOR ALL USING (true) WITH CHECK (true);

-- Roles
CREATE POLICY "Allow all for authenticated service" ON public."Roles" FOR ALL USING (true) WITH CHECK (true);

-- SystemSettings
CREATE POLICY "Allow all for authenticated service" ON public."SystemSettings" FOR ALL USING (true) WITH CHECK (true);

-- Customers
CREATE POLICY "Allow all for authenticated service" ON public."Customers" FOR ALL USING (true) WITH CHECK (true);

-- __EFMigrationsHistory
CREATE POLICY "Allow all for authenticated service" ON public."__EFMigrationsHistory" FOR ALL USING (true) WITH CHECK (true);

-- DataProtectionKeys
CREATE POLICY "Allow all for authenticated service" ON public."DataProtectionKeys" FOR ALL USING (true) WITH CHECK (true);

-- Also update AIModel setting while we're here
UPDATE "SystemSettings" 
SET "SettingValue" = 'gpt-5.2', 
    "Description" = 'OpenAI model for document processing',
    "ModifiedDate" = NOW() 
WHERE "SettingKey" = 'AIModel';

-- Check if OpenAIApiKey exists, if not create it
INSERT INTO "SystemSettings" ("Category", "SettingKey", "SettingValue", "Description", "ModifiedBy", "ModifiedDate")
SELECT 'API', 'OpenAIApiKey', '', 'OpenAI API key for AI-powered invoice/payment import. Get from https://platform.openai.com/api-keys', 'System', NOW()
WHERE NOT EXISTS (SELECT 1 FROM "SystemSettings" WHERE "SettingKey" = 'OpenAIApiKey');

SELECT 'RLS enabled on all tables and AI settings updated!' as result;
