CREATE TABLE IF NOT EXISTS "__EFMigrationsHistory" (
    "MigrationId" character varying(150) NOT NULL,
    "ProductVersion" character varying(32) NOT NULL,
    CONSTRAINT "PK___EFMigrationsHistory" PRIMARY KEY ("MigrationId")
);

START TRANSACTION;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251107074826_InitialCreate') THEN
    CREATE TABLE "Invoices" (
        "Id" INTEGER NOT NULL,
        "InvoiceNumber" TEXT NOT NULL,
        "InvoiceDate" TEXT NOT NULL,
        "DueDate" TEXT NOT NULL,
        "CustomerName" TEXT NOT NULL,
        "CustomerAddress" TEXT,
        "CustomerEmail" TEXT,
        "CustomerPhone" TEXT,
        "TotalAmount" TEXT NOT NULL,
        "PaidAmount" TEXT NOT NULL,
        "Status" TEXT NOT NULL,
        "Notes" TEXT,
        "CreatedDate" TEXT NOT NULL,
        "ModifiedDate" TEXT,
        CONSTRAINT "PK_Invoices" PRIMARY KEY ("Id")
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251107074826_InitialCreate') THEN
    CREATE TABLE "InvoiceItems" (
        "Id" INTEGER NOT NULL,
        "InvoiceId" INTEGER NOT NULL,
        "Description" TEXT NOT NULL,
        "Quantity" INTEGER NOT NULL,
        "UnitPrice" TEXT NOT NULL,
        CONSTRAINT "PK_InvoiceItems" PRIMARY KEY ("Id"),
        CONSTRAINT "FK_InvoiceItems_Invoices_InvoiceId" FOREIGN KEY ("InvoiceId") REFERENCES "Invoices" ("Id") ON DELETE CASCADE
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251107074826_InitialCreate') THEN
    CREATE TABLE "Payments" (
        "Id" INTEGER NOT NULL,
        "PaymentNumber" TEXT NOT NULL,
        "InvoiceId" INTEGER NOT NULL,
        "PaymentDate" TEXT NOT NULL,
        "Amount" TEXT NOT NULL,
        "PaymentMethod" TEXT NOT NULL,
        "ReferenceNumber" TEXT,
        "Notes" TEXT,
        "CreatedDate" TEXT NOT NULL,
        CONSTRAINT "PK_Payments" PRIMARY KEY ("Id"),
        CONSTRAINT "FK_Payments_Invoices_InvoiceId" FOREIGN KEY ("InvoiceId") REFERENCES "Invoices" ("Id") ON DELETE RESTRICT
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251107074826_InitialCreate') THEN
    CREATE INDEX "IX_InvoiceItems_InvoiceId" ON "InvoiceItems" ("InvoiceId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251107074826_InitialCreate') THEN
    CREATE UNIQUE INDEX "IX_Invoices_InvoiceNumber" ON "Invoices" ("InvoiceNumber");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251107074826_InitialCreate') THEN
    CREATE INDEX "IX_Payments_InvoiceId" ON "Payments" ("InvoiceId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251107074826_InitialCreate') THEN
    CREATE UNIQUE INDEX "IX_Payments_PaymentNumber" ON "Payments" ("PaymentNumber");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251107074826_InitialCreate') THEN
    INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
    VALUES ('20251107074826_InitialCreate', '9.0.0');
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251107094703_AddProcurementSystem') THEN
    ALTER TABLE "Invoices" ADD "InvoiceType" TEXT NOT NULL DEFAULT '';
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251107094703_AddProcurementSystem') THEN
    ALTER TABLE "Invoices" ADD "PurchaseOrderId" INTEGER;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251107094703_AddProcurementSystem') THEN
    ALTER TABLE "Invoices" ADD "SupplierId" INTEGER;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251107094703_AddProcurementSystem') THEN
    CREATE TABLE "Requisitions" (
        "Id" INTEGER NOT NULL,
        "RequisitionNumber" TEXT NOT NULL,
        "RequisitionDate" TEXT NOT NULL,
        "RequestedBy" TEXT NOT NULL,
        "Department" TEXT NOT NULL,
        "FacilityType" TEXT NOT NULL,
        "Purpose" TEXT NOT NULL,
        "EstimatedAmount" TEXT NOT NULL,
        "CostCode" TEXT NOT NULL,
        "BudgetCode" TEXT NOT NULL,
        "Status" TEXT NOT NULL,
        "SupervisorName" TEXT,
        "SupervisorApprovalDate" TEXT,
        "SupervisorComments" TEXT,
        "FinanceOfficerName" TEXT,
        "FinanceApprovalDate" TEXT,
        "FinanceComments" TEXT,
        "BudgetApproved" INTEGER,
        "NeedApproved" INTEGER,
        "CostCodeApproved" INTEGER,
        "FinalApproverName" TEXT,
        "FinalApprovalDate" TEXT,
        "FinalApproverComments" TEXT,
        "RejectionReason" TEXT,
        "Notes" TEXT,
        "CreatedDate" TEXT NOT NULL,
        "ModifiedDate" TEXT,
        CONSTRAINT "PK_Requisitions" PRIMARY KEY ("Id")
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251107094703_AddProcurementSystem') THEN
    CREATE TABLE "Suppliers" (
        "Id" INTEGER NOT NULL,
        "SupplierName" TEXT NOT NULL,
        "SupplierCode" TEXT,
        "Address" TEXT,
        "Email" TEXT,
        "Phone" TEXT,
        "Mobile" TEXT,
        "ContactPerson" TEXT,
        "TIN" TEXT,
        "RegistrationNumber" TEXT,
        "BankName" TEXT,
        "BankAccountNumber" TEXT,
        "ProductsServices" TEXT,
        "Status" TEXT NOT NULL,
        "PaymentTermsDays" INTEGER NOT NULL,
        "Notes" TEXT,
        "CreatedDate" TEXT NOT NULL,
        "ModifiedDate" TEXT,
        CONSTRAINT "PK_Suppliers" PRIMARY KEY ("Id")
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251107094703_AddProcurementSystem') THEN
    CREATE TABLE "RequisitionItems" (
        "Id" INTEGER NOT NULL,
        "RequisitionId" INTEGER NOT NULL,
        "ItemDescription" TEXT NOT NULL,
        "ItemCode" TEXT,
        "QuantityRequested" INTEGER NOT NULL,
        "Unit" TEXT NOT NULL,
        "EstimatedUnitPrice" TEXT NOT NULL,
        "Justification" TEXT,
        CONSTRAINT "PK_RequisitionItems" PRIMARY KEY ("Id"),
        CONSTRAINT "FK_RequisitionItems_Requisitions_RequisitionId" FOREIGN KEY ("RequisitionId") REFERENCES "Requisitions" ("Id") ON DELETE CASCADE
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251107094703_AddProcurementSystem') THEN
    CREATE TABLE "PurchaseOrders" (
        "Id" INTEGER NOT NULL,
        "PONumber" TEXT NOT NULL,
        "PODate" TEXT NOT NULL,
        "RequisitionId" INTEGER,
        "SupplierId" INTEGER NOT NULL,
        "ExpectedDeliveryDate" TEXT NOT NULL,
        "DeliveryAddress" TEXT NOT NULL,
        "TotalAmount" TEXT NOT NULL,
        "Status" TEXT NOT NULL,
        "PreparedBy" TEXT NOT NULL,
        "ApprovedBy" TEXT,
        "ApprovalDate" TEXT,
        "TermsAndConditions" TEXT,
        "Notes" TEXT,
        "CreatedDate" TEXT NOT NULL,
        "ModifiedDate" TEXT,
        CONSTRAINT "PK_PurchaseOrders" PRIMARY KEY ("Id"),
        CONSTRAINT "FK_PurchaseOrders_Requisitions_RequisitionId" FOREIGN KEY ("RequisitionId") REFERENCES "Requisitions" ("Id") ON DELETE RESTRICT,
        CONSTRAINT "FK_PurchaseOrders_Suppliers_SupplierId" FOREIGN KEY ("SupplierId") REFERENCES "Suppliers" ("Id") ON DELETE RESTRICT
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251107094703_AddProcurementSystem') THEN
    CREATE TABLE "PurchaseOrderItems" (
        "Id" INTEGER NOT NULL,
        "PurchaseOrderId" INTEGER NOT NULL,
        "ItemDescription" TEXT NOT NULL,
        "ItemCode" TEXT,
        "QuantityOrdered" INTEGER NOT NULL,
        "Unit" TEXT NOT NULL,
        "UnitPrice" TEXT NOT NULL,
        "QuantityReceived" INTEGER NOT NULL,
        "ReceivedDate" TEXT,
        CONSTRAINT "PK_PurchaseOrderItems" PRIMARY KEY ("Id"),
        CONSTRAINT "FK_PurchaseOrderItems_PurchaseOrders_PurchaseOrderId" FOREIGN KEY ("PurchaseOrderId") REFERENCES "PurchaseOrders" ("Id") ON DELETE CASCADE
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251107094703_AddProcurementSystem') THEN
    CREATE INDEX "IX_Invoices_PurchaseOrderId" ON "Invoices" ("PurchaseOrderId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251107094703_AddProcurementSystem') THEN
    CREATE INDEX "IX_Invoices_SupplierId" ON "Invoices" ("SupplierId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251107094703_AddProcurementSystem') THEN
    CREATE INDEX "IX_PurchaseOrderItems_PurchaseOrderId" ON "PurchaseOrderItems" ("PurchaseOrderId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251107094703_AddProcurementSystem') THEN
    CREATE UNIQUE INDEX "IX_PurchaseOrders_PONumber" ON "PurchaseOrders" ("PONumber");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251107094703_AddProcurementSystem') THEN
    CREATE INDEX "IX_PurchaseOrders_RequisitionId" ON "PurchaseOrders" ("RequisitionId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251107094703_AddProcurementSystem') THEN
    CREATE INDEX "IX_PurchaseOrders_SupplierId" ON "PurchaseOrders" ("SupplierId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251107094703_AddProcurementSystem') THEN
    CREATE INDEX "IX_RequisitionItems_RequisitionId" ON "RequisitionItems" ("RequisitionId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251107094703_AddProcurementSystem') THEN
    CREATE UNIQUE INDEX "IX_Requisitions_RequisitionNumber" ON "Requisitions" ("RequisitionNumber");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251107094703_AddProcurementSystem') THEN
    CREATE UNIQUE INDEX "IX_Suppliers_SupplierCode" ON "Suppliers" ("SupplierCode");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251107094703_AddProcurementSystem') THEN
    ALTER TABLE "Invoices" ADD CONSTRAINT "FK_Invoices_PurchaseOrders_PurchaseOrderId" FOREIGN KEY ("PurchaseOrderId") REFERENCES "PurchaseOrders" ("Id");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251107094703_AddProcurementSystem') THEN
    ALTER TABLE "Invoices" ADD CONSTRAINT "FK_Invoices_Suppliers_SupplierId" FOREIGN KEY ("SupplierId") REFERENCES "Suppliers" ("Id");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251107094703_AddProcurementSystem') THEN
    INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
    VALUES ('20251107094703_AddProcurementSystem', '9.0.0');
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251107115037_AddAdminPortal') THEN
    CREATE TABLE "SystemSettings" (
        "Id" INTEGER NOT NULL,
        "SettingKey" TEXT NOT NULL,
        "SettingValue" TEXT,
        "Description" TEXT,
        "Category" TEXT NOT NULL,
        "ModifiedDate" TEXT NOT NULL,
        "ModifiedBy" TEXT,
        CONSTRAINT "PK_SystemSettings" PRIMARY KEY ("Id")
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251107115037_AddAdminPortal') THEN
    CREATE TABLE "Users" (
        "Id" INTEGER NOT NULL,
        "Username" TEXT NOT NULL,
        "FullName" TEXT NOT NULL,
        "Email" TEXT NOT NULL,
        "Phone" TEXT,
        "Department" TEXT NOT NULL,
        "Facility" TEXT NOT NULL,
        "FacilityType" TEXT NOT NULL,
        "Role" TEXT NOT NULL,
        "Status" TEXT NOT NULL,
        "PasswordHash" TEXT,
        "CreatedDate" TEXT NOT NULL,
        "ModifiedDate" TEXT,
        "LastLoginDate" TEXT,
        "Notes" TEXT,
        CONSTRAINT "PK_Users" PRIMARY KEY ("Id")
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251107115037_AddAdminPortal') THEN
    CREATE TABLE "AuditLogs" (
        "Id" INTEGER NOT NULL,
        "UserId" INTEGER,
        "Username" TEXT,
        "Action" TEXT NOT NULL,
        "Entity" TEXT NOT NULL,
        "EntityId" INTEGER,
        "Details" TEXT,
        "ActionDate" TEXT NOT NULL,
        "IpAddress" TEXT,
        CONSTRAINT "PK_AuditLogs" PRIMARY KEY ("Id"),
        CONSTRAINT "FK_AuditLogs_Users_UserId" FOREIGN KEY ("UserId") REFERENCES "Users" ("Id") ON DELETE SET NULL
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251107115037_AddAdminPortal') THEN
    CREATE INDEX "IX_AuditLogs_UserId" ON "AuditLogs" ("UserId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251107115037_AddAdminPortal') THEN
    CREATE UNIQUE INDEX "IX_SystemSettings_SettingKey" ON "SystemSettings" ("SettingKey");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251107115037_AddAdminPortal') THEN
    CREATE UNIQUE INDEX "IX_Users_Email" ON "Users" ("Email");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251107115037_AddAdminPortal') THEN
    CREATE UNIQUE INDEX "IX_Users_Username" ON "Users" ("Username");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251107115037_AddAdminPortal') THEN
    INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
    VALUES ('20251107115037_AddAdminPortal', '9.0.0');
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251108092942_AddRoleBasedAccessControl') THEN
    CREATE TABLE "Permissions" (
        "Id" INTEGER NOT NULL,
        "Name" TEXT NOT NULL,
        "DisplayName" TEXT,
        "Description" TEXT,
        "Module" TEXT NOT NULL,
        "IsActive" INTEGER NOT NULL,
        "CreatedDate" TEXT NOT NULL,
        CONSTRAINT "PK_Permissions" PRIMARY KEY ("Id")
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251108092942_AddRoleBasedAccessControl') THEN
    CREATE TABLE "Roles" (
        "Id" INTEGER NOT NULL,
        "Name" TEXT NOT NULL,
        "DisplayName" TEXT,
        "Description" TEXT,
        "IsActive" INTEGER NOT NULL,
        "CreatedDate" TEXT NOT NULL,
        CONSTRAINT "PK_Roles" PRIMARY KEY ("Id")
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251108092942_AddRoleBasedAccessControl') THEN
    CREATE TABLE "RolePermissions" (
        "Id" INTEGER NOT NULL,
        "RoleId" INTEGER NOT NULL,
        "PermissionId" INTEGER NOT NULL,
        "AssignedDate" TEXT NOT NULL,
        CONSTRAINT "PK_RolePermissions" PRIMARY KEY ("Id"),
        CONSTRAINT "FK_RolePermissions_Permissions_PermissionId" FOREIGN KEY ("PermissionId") REFERENCES "Permissions" ("Id") ON DELETE CASCADE,
        CONSTRAINT "FK_RolePermissions_Roles_RoleId" FOREIGN KEY ("RoleId") REFERENCES "Roles" ("Id") ON DELETE CASCADE
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251108092942_AddRoleBasedAccessControl') THEN
    CREATE TABLE "UserRoles" (
        "Id" INTEGER NOT NULL,
        "UserId" INTEGER NOT NULL,
        "RoleId" INTEGER NOT NULL,
        "AssignedDate" TEXT NOT NULL,
        "AssignedBy" TEXT,
        CONSTRAINT "PK_UserRoles" PRIMARY KEY ("Id"),
        CONSTRAINT "FK_UserRoles_Roles_RoleId" FOREIGN KEY ("RoleId") REFERENCES "Roles" ("Id") ON DELETE CASCADE,
        CONSTRAINT "FK_UserRoles_Users_UserId" FOREIGN KEY ("UserId") REFERENCES "Users" ("Id") ON DELETE CASCADE
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251108092942_AddRoleBasedAccessControl') THEN
    CREATE UNIQUE INDEX "IX_Permissions_Name" ON "Permissions" ("Name");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251108092942_AddRoleBasedAccessControl') THEN
    CREATE INDEX "IX_RolePermissions_PermissionId" ON "RolePermissions" ("PermissionId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251108092942_AddRoleBasedAccessControl') THEN
    CREATE UNIQUE INDEX "IX_RolePermissions_RoleId_PermissionId" ON "RolePermissions" ("RoleId", "PermissionId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251108092942_AddRoleBasedAccessControl') THEN
    CREATE UNIQUE INDEX "IX_Roles_Name" ON "Roles" ("Name");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251108092942_AddRoleBasedAccessControl') THEN
    CREATE INDEX "IX_UserRoles_RoleId" ON "UserRoles" ("RoleId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251108092942_AddRoleBasedAccessControl') THEN
    CREATE UNIQUE INDEX "IX_UserRoles_UserId_RoleId" ON "UserRoles" ("UserId", "RoleId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251108092942_AddRoleBasedAccessControl') THEN
    INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
    VALUES ('20251108092942_AddRoleBasedAccessControl', '9.0.0');
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251108101918_AddPaymentAllocationSystem') THEN
    ALTER TABLE "Payments" ALTER COLUMN "InvoiceId" DROP NOT NULL;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251108101918_AddPaymentAllocationSystem') THEN
    ALTER TABLE "Payments" ADD "ModifiedDate" TEXT;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251108101918_AddPaymentAllocationSystem') THEN
    ALTER TABLE "Payments" ADD "Status" TEXT NOT NULL DEFAULT '';
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251108101918_AddPaymentAllocationSystem') THEN
    CREATE TABLE "PaymentAllocations" (
        "Id" INTEGER NOT NULL,
        "PaymentId" INTEGER NOT NULL,
        "InvoiceId" INTEGER NOT NULL,
        "AllocatedAmount" TEXT NOT NULL,
        "AllocationDate" TEXT NOT NULL,
        "Notes" TEXT,
        CONSTRAINT "PK_PaymentAllocations" PRIMARY KEY ("Id"),
        CONSTRAINT "FK_PaymentAllocations_Invoices_InvoiceId" FOREIGN KEY ("InvoiceId") REFERENCES "Invoices" ("Id") ON DELETE RESTRICT,
        CONSTRAINT "FK_PaymentAllocations_Payments_PaymentId" FOREIGN KEY ("PaymentId") REFERENCES "Payments" ("Id") ON DELETE CASCADE
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251108101918_AddPaymentAllocationSystem') THEN
    CREATE INDEX "IX_PaymentAllocations_InvoiceId" ON "PaymentAllocations" ("InvoiceId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251108101918_AddPaymentAllocationSystem') THEN
    CREATE INDEX "IX_PaymentAllocations_PaymentId_InvoiceId" ON "PaymentAllocations" ("PaymentId", "InvoiceId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251108101918_AddPaymentAllocationSystem') THEN
    INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
    VALUES ('20251108101918_AddPaymentAllocationSystem', '9.0.0');
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251129062343_AddGSTToInvoices') THEN
    ALTER TABLE "Invoices" ADD "GSTAmount" TEXT NOT NULL DEFAULT '0';
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251129062343_AddGSTToInvoices') THEN
    ALTER TABLE "Invoices" ADD "GSTRate" TEXT NOT NULL DEFAULT '0';
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251129062343_AddGSTToInvoices') THEN
    ALTER TABLE "Invoices" ADD "SubTotal" TEXT NOT NULL DEFAULT '0';
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251129062343_AddGSTToInvoices') THEN
    INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
    VALUES ('20251129062343_AddGSTToInvoices', '9.0.0');
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251129063255_UpdateExistingInvoicesWithGST') THEN

                    UPDATE Invoices 
                    SET SubTotal = TotalAmount,
                        GSTAmount = ROUND(TotalAmount * 0.10, 2),
                        TotalAmount = ROUND(TotalAmount + (TotalAmount * 0.10), 2),
                        GSTRate = 10.0
                    WHERE SubTotal = 0 OR SubTotal = '0.0';
                
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251129063255_UpdateExistingInvoicesWithGST') THEN
    INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
    VALUES ('20251129063255_UpdateExistingInvoicesWithGST', '9.0.0');
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260111065745_FixPendingChanges') THEN
    ALTER TABLE "Payments" ADD "AccountType" TEXT;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260111065745_FixPendingChanges') THEN
    ALTER TABLE "Payments" ADD "BankAccountNumber" TEXT;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260111065745_FixPendingChanges') THEN
    ALTER TABLE "Payments" ADD "BankName" TEXT;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260111065745_FixPendingChanges') THEN
    ALTER TABLE "Payments" ADD "Currency" TEXT;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260111065745_FixPendingChanges') THEN
    ALTER TABLE "Payments" ADD "CustomerId" INTEGER;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260111065745_FixPendingChanges') THEN
    ALTER TABLE "Payments" ADD "PayeeAccountNumber" TEXT;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260111065745_FixPendingChanges') THEN
    ALTER TABLE "Payments" ADD "PayeeBranchNumber" TEXT;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260111065745_FixPendingChanges') THEN
    ALTER TABLE "Payments" ADD "PayeeName" TEXT;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260111065745_FixPendingChanges') THEN
    ALTER TABLE "Payments" ADD "PayerAccountNumber" TEXT;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260111065745_FixPendingChanges') THEN
    ALTER TABLE "Payments" ADD "PayerBankAccountNumber" TEXT;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260111065745_FixPendingChanges') THEN
    ALTER TABLE "Payments" ADD "PayerBranchNumber" TEXT;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260111065745_FixPendingChanges') THEN
    ALTER TABLE "Payments" ADD "PayerName" TEXT;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260111065745_FixPendingChanges') THEN
    ALTER TABLE "Payments" ADD "Purpose" TEXT;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260111065745_FixPendingChanges') THEN
    ALTER TABLE "Payments" ADD "SupplierId" INTEGER;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260111065745_FixPendingChanges') THEN
    ALTER TABLE "Payments" ADD "TransferTo" TEXT;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260111065745_FixPendingChanges') THEN
    ALTER TABLE "Invoices" ADD "CustomerId" INTEGER;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260111065745_FixPendingChanges') THEN
    ALTER TABLE "Invoices" ADD "GSTEnabled" INTEGER NOT NULL DEFAULT 0;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260111065745_FixPendingChanges') THEN
    CREATE TABLE "Customers" (
        "Id" INTEGER NOT NULL,
        "CustomerName" TEXT NOT NULL,
        "CustomerCode" TEXT,
        "Address" TEXT,
        "Email" TEXT,
        "Phone" TEXT,
        "Mobile" TEXT,
        "ContactPerson" TEXT,
        "TIN" TEXT,
        "RegistrationNumber" TEXT,
        "BankName" TEXT,
        "BankAccountNumber" TEXT,
        "Industry" TEXT,
        "Status" TEXT NOT NULL,
        "PaymentTermsDays" INTEGER NOT NULL,
        "CreditLimit" TEXT NOT NULL,
        "Notes" TEXT,
        "CreatedDate" TEXT NOT NULL,
        "ModifiedDate" TEXT,
        CONSTRAINT "PK_Customers" PRIMARY KEY ("Id")
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260111065745_FixPendingChanges') THEN
    CREATE TABLE "ImportedDocuments" (
        "Id" INTEGER NOT NULL,
        "FileName" TEXT NOT NULL,
        "OriginalFileName" TEXT NOT NULL,
        "ContentType" TEXT,
        "FileSize" INTEGER NOT NULL,
        "FileContent" BLOB NOT NULL,
        "DocumentType" TEXT NOT NULL,
        "InvoiceId" INTEGER,
        "PaymentId" INTEGER,
        "ExtractedText" TEXT,
        "ExtractedAccountNumber" TEXT,
        "ExtractedBankName" TEXT,
        "ExtractedSupplierName" TEXT,
        "ExtractedCustomerName" TEXT,
        "ProcessingStatus" TEXT NOT NULL,
        "ProcessingNotes" TEXT,
        "UploadDate" TEXT NOT NULL,
        "ProcessedDate" TEXT,
        "UploadedBy" TEXT,
        CONSTRAINT "PK_ImportedDocuments" PRIMARY KEY ("Id"),
        CONSTRAINT "FK_ImportedDocuments_Invoices_InvoiceId" FOREIGN KEY ("InvoiceId") REFERENCES "Invoices" ("Id") ON DELETE SET NULL,
        CONSTRAINT "FK_ImportedDocuments_Payments_PaymentId" FOREIGN KEY ("PaymentId") REFERENCES "Payments" ("Id") ON DELETE SET NULL
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260111065745_FixPendingChanges') THEN
    CREATE INDEX "IX_Payments_CustomerId" ON "Payments" ("CustomerId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260111065745_FixPendingChanges') THEN
    CREATE INDEX "IX_Payments_SupplierId" ON "Payments" ("SupplierId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260111065745_FixPendingChanges') THEN
    CREATE INDEX "IX_Invoices_CustomerId" ON "Invoices" ("CustomerId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260111065745_FixPendingChanges') THEN
    CREATE INDEX "IX_ImportedDocuments_FileName" ON "ImportedDocuments" ("FileName");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260111065745_FixPendingChanges') THEN
    CREATE INDEX "IX_ImportedDocuments_InvoiceId" ON "ImportedDocuments" ("InvoiceId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260111065745_FixPendingChanges') THEN
    CREATE INDEX "IX_ImportedDocuments_PaymentId" ON "ImportedDocuments" ("PaymentId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260111065745_FixPendingChanges') THEN
    ALTER TABLE "Invoices" ADD CONSTRAINT "FK_Invoices_Customers_CustomerId" FOREIGN KEY ("CustomerId") REFERENCES "Customers" ("Id") ON DELETE SET NULL;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260111065745_FixPendingChanges') THEN
    ALTER TABLE "Payments" ADD CONSTRAINT "FK_Payments_Customers_CustomerId" FOREIGN KEY ("CustomerId") REFERENCES "Customers" ("Id") ON DELETE SET NULL;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260111065745_FixPendingChanges') THEN
    ALTER TABLE "Payments" ADD CONSTRAINT "FK_Payments_Suppliers_SupplierId" FOREIGN KEY ("SupplierId") REFERENCES "Suppliers" ("Id") ON DELETE SET NULL;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260111065745_FixPendingChanges') THEN
    INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
    VALUES ('20260111065745_FixPendingChanges', '9.0.0');
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260123035202_ExpandProcessingNotesColumn') THEN
    CREATE TABLE "DataProtectionKeys" (
        "Id" INTEGER NOT NULL,
        "FriendlyName" TEXT,
        "Xml" TEXT,
        CONSTRAINT "PK_DataProtectionKeys" PRIMARY KEY ("Id")
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260123035202_ExpandProcessingNotesColumn') THEN
    INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
    VALUES ('20260123035202_ExpandProcessingNotesColumn', '9.0.0');
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260123042829_FixProcessingNotesColumnType') THEN

                    DO $$ 
                    BEGIN 
                        IF EXISTS (SELECT 1 FROM information_schema.columns 
                                   WHERE table_name = 'ImportedDocuments' 
                                   AND column_name = 'ProcessingNotes') THEN
                            ALTER TABLE "ImportedDocuments" 
                            ALTER COLUMN "ProcessingNotes" TYPE TEXT;
                        END IF;
                    END $$;
                
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260123042829_FixProcessingNotesColumnType') THEN
    INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
    VALUES ('20260123042829_FixProcessingNotesColumnType', '9.0.0');
    END IF;
END $EF$;
COMMIT;

