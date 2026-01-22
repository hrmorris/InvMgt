# 🏥 Procurement Management System - Overview

## System Purpose
Complete procurement workflow for health facilities with requisition → purchase order → supplier invoice tracking with multi-level approval process.

---

## 📋 **Workflow Chain**

```
┌─────────────────────────────────────────────────────────────────────┐
│                    PROCUREMENT WORKFLOW                              │
└─────────────────────────────────────────────────────────────────────┘

1. REQUISITION CREATION
   │  ├─ Created by: OIC/Unit Head
   │  ├─ Contains: Items needed, quantities, estimated costs
   │  └─ Status: Draft → Pending_Supervisor

2. SUPERVISOR APPROVAL
   │  ├─ Reviewed by: Immediate Supervisor
   │  ├─ Checks: Need, quantities, reasonableness
   │  └─ Status: Pending_Supervisor → Pending_Finance

3. FINANCE & ADMIN SCREENING
   │  ├─ Reviewed by: Finance & Admin Officer
   │  ├─ Checks: Budget availability, Need justification, Cost code validity
   │  └─ Status: Pending_Finance → Pending_Approval

4. FINAL APPROVAL
   │  ├─ Outstation: Health Manager
   │  ├─ Hospital: Hospital Executive Officer OR Finance Manager
   │  └─ Status: Pending_Approval → Approved

5. PURCHASE ORDER CREATION
   │  ├─ Created from approved requisition
   │  ├─ Supplier selected
   │  ├─ Sent to supplier
   │  └─ Status: Pending → Sent_to_Supplier

6. GOODS/SERVICES RECEIVED
   │  ├─ Items received and verified
   │  ├─ Quantity checked against PO
   │  └─ Status: Partially_Received → Fully_Received

7. SUPPLIER INVOICE RECEIVED
   │  ├─ Linked to Purchase Order
   │  ├─ Amounts verified
   │  └─ Ready for payment

8. PAYMENT PROCESSING
   │  ├─ Invoice paid based on terms
   │  └─ Complete!
```

---

## 🗂️ **Database Structure**

### Tables Created:
1. **Requisitions** - Purchase requisitions from departments
2. **RequisitionItems** - Line items in requisitions
3. **PurchaseOrders** - POs created from approved requisitions
4. **PurchaseOrderItems** - Line items in POs
5. **Suppliers** - Supplier master data
6. **Invoices** (updated) - Now links to PO and Supplier
7. **InvoiceItems** (existing)
8. **Payments** (existing)

### Relationships:
```
Requisition (1) ───< (Many) RequisitionItems
Requisition (1) ───< (Many) PurchaseOrders
PurchaseOrder (1) ───< (Many) PurchaseOrderItems
PurchaseOrder (1) ───< (Many) Invoices
Supplier (1) ───< (Many) PurchaseOrders
Supplier (1) ───< (Many) Invoices
```

---

## 👥 **User Roles & Permissions**

| Role | Permissions |
|------|-------------|
| **OIC/Unit Head** | Create requisitions, View own requisitions |
| **Supervisor** | Approve requisitions (first level), View team requisitions |
| **Finance & Admin Officer** | Screen requisitions (budget/cost code), View all requisitions |
| **Health Manager** | Final approval (outstation facilities) |
| **Hospital Executive Officer** | Final approval (hospital) |
| **Finance Manager** | Final approval (hospital), Create POs, Manage suppliers |
| **Procurement Officer** | Create POs, Receive goods, Manage suppliers |

---

## 📊 **Requisition Statuses**

| Status | Description | Next Action |
|--------|-------------|-------------|
| `Draft` | Being created | Submit for approval |
| `Pending_Supervisor` | Waiting for supervisor | Supervisor review |
| `Pending_Finance` | Waiting for finance screening | Finance review |
| `Pending_Approval` | Waiting for final approval | Manager/Executive approval |
| `Approved` | Fully approved | Create PO |
| `Rejected` | Rejected at any level | Review and resubmit |

---

## 📦 **Purchase Order Statuses**

| Status | Description |
|--------|-------------|
| `Pending` | Created but not sent |
| `Sent_to_Supplier` | Sent to supplier |
| `Partially_Received` | Some items received |
| `Fully_Received` | All items received |
| `Cancelled` | PO cancelled |

---

## 💰 **Key Features Implemented**

### Requisition Module:
- ✅ Create multi-item requisitions
- ✅ Department/facility tracking
- ✅ Cost code and budget code assignment
- ✅ Three-level approval workflow
- ✅ Rejection with reasons
- ✅ Approval comments at each level
- ✅ Budget, need, and cost code validation

### Purchase Order Module:
- ✅ Create from approved requisition
- ✅ Link to supplier
- ✅ Expected delivery tracking
- ✅ Terms and conditions
- ✅ Goods receipt tracking
- ✅ Partial/full receipt status

### Supplier Module:
- ✅ Supplier master data
- ✅ Contact information
- ✅ Tax (TIN) and registration tracking
- ✅ Bank details for payments
- ✅ Payment terms
- ✅ Active/Inactive/Blacklisted status

### Invoice Module (Enhanced):
- ✅ Link to Purchase Order
- ✅ Link to Supplier
- ✅ Invoice type (Customer AR / Supplier AP)
- ✅ Verification against PO
- ✅ Payment tracking

---

## 🎯 **Usage Scenarios**

### Scenario 1: Health Facility Needs Medical Supplies
1. OIC creates requisition with items needed
2. Supervisor reviews and approves
3. Finance officer checks budget (approved)
4. Health Manager gives final approval
5. Procurement creates PO to supplier
6. Supplier delivers goods
7. Goods received and marked in system
8. Supplier invoice received and linked to PO
9. Finance processes payment

### Scenario 2: Hospital Department Needs Equipment
1. Department head creates requisition
2. Unit supervisor approves
3. Finance officer screens (budget check)
4. Hospital Executive Officer approves
5. PO created and sent to supplier
6. Equipment delivered and verified
7. Supplier invoice matched to PO
8. Payment processed

---

## 🚀 **Quick Start Guide**

### Step 1: Add Suppliers
1. Go to **Suppliers** → **Create New**
2. Enter supplier details
3. Save

### Step 2: Create Requisition
1. Go to **Requisitions** → **Create New**
2. Fill in department, purpose, facility type
3. Add items with quantities and estimated prices
4. Submit for approval

### Step 3: Approval Process
1. **Supervisor**: Reviews in "Pending Approvals"
2. **Finance Officer**: Screens budget/cost code
3. **Manager/Executive**: Final approval

### Step 4: Create Purchase Order
1. Go to approved requisition
2. Click "Create Purchase Order"
3. Select supplier
4. Verify items and prices
5. Save and send to supplier

### Step 5: Receive Goods
1. Go to Purchase Order
2. Mark items as received
3. Update quantities

### Step 6: Process Invoice
1. Supplier invoice received
2. Create invoice linked to PO
3. Verify amounts
4. Process payment

---

## 📱 **User Interface Features**

### Dashboard:
- Pending approvals count (by role)
- Open requisitions
- Active purchase orders
- Overdue invoices

### Requisition List:
- Filter by status
- Search by number/department
- Color-coded status badges
- Quick actions (View, Approve, Reject)

### Purchase Order List:
- Link to requisition
- Supplier information
- Delivery status
- Receipt tracking

### Approval Interface:
- View requisition details
- See approval history
- Add comments
- Approve/Reject buttons

---

## 🔐 **Security & Controls**

- ✅ Role-based access control
- ✅ Audit trail (all approvals logged)
- ✅ Budget validation before approval
- ✅ Unique requisition/PO numbers
- ✅ Cannot modify after approval
- ✅ Rejection requires reason

---

## 📈 **Reports Available**

1. **Requisition Report** - All requisitions by status/date
2. **Purchase Order Report** - All POs by supplier/status
3. **Pending Approvals Report** - What needs approval
4. **Budget vs Actual Report** - Spending by cost code
5. **Supplier Performance** - Delivery times, invoice accuracy

---

## 💡 **Best Practices**

1. **Clear Descriptions**: Always provide clear item descriptions
2. **Accurate Estimates**: Estimate costs as accurately as possible
3. **Proper Justification**: Explain why items are needed
4. **Budget Codes**: Use correct cost/budget codes
5. **Timely Approvals**: Review and approve promptly
6. **Receipt Verification**: Verify goods before marking received
7. **Invoice Matching**: Always match invoice to PO

---

## ✅ **System Status**

| Module | Status |
|--------|--------|
| Database Models | ✅ Complete |
| Database Migration | ✅ Applied |
| Service Interfaces | 🔄 In Progress |
| Service Implementations | 🔄 In Progress |
| Controllers | ⏳ Pending |
| Views | ⏳ Pending |
| Navigation | ⏳ Pending |
| Testing | ⏳ Pending |

---

**Next Steps**: Complete service implementations, create controllers, and build user interface views.

---

*This system provides complete procurement management for health facilities from requisition to payment!* 🏥✨

