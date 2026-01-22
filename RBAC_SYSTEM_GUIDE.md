# Role-Based Access Control (RBAC) System Guide

## Overview
A comprehensive role-based access control system has been implemented for the Invoice Management application. This system provides granular control over user permissions and access to different modules and features.

## Key Components

### 1. Models
- **Role**: Represents a role in the system (e.g., Admin, Finance Officer)
- **Permission**: Represents a specific permission (e.g., ViewInvoices, CreatePayments)
- **UserRole**: Junction table linking users to roles (many-to-many)
- **RolePermission**: Junction table linking roles to permissions (many-to-many)

### 2. Authorization Attributes
- **`[Authorize]`**: Simple check if user is logged in
- **`[AuthorizeRoles(roles...)]`**: Check if user has specific roles
- **`[AuthorizePermission(permissions...)]`**: Check if user has specific permissions

### 3. Authorization Service
Provides methods to:
- Check user permissions
- Check user roles
- Assign/remove roles from users
- Assign/remove permissions from roles
- Initialize default roles and permissions

## Default Roles

### System Admin
- **Full access** to everything
- Can manage all modules
- Can create/edit/delete roles
- Can assign permissions

### Admin
- Administrative access with some restrictions
- Cannot delete system-level settings
- Can manage users and most modules

### Finance Officer
- Manages invoices, payments, and financial reports
- Access to AI import features
- Can view and manage suppliers (except delete)

### Health Manager
- Approves requisitions for outstation facilities
- Views purchase orders and reports
- Cannot create requisitions

### Hospital Executive Officer
- Approves requisitions for hospital units
- Similar permissions to Health Manager

### Finance Manager
- Final approval for financial matters
- Can approve both requisitions and purchase orders
- Views all financial data

### Procurement Officer
- Full access to procurement module
- Manages requisitions, purchase orders, and suppliers
- Views invoices related to procurement

### OIC (Officer in Charge)
- Creates and submits requisitions for facility needs
- Views purchase orders for their requisitions
- Cannot approve requisitions

### Supervisor
- Reviews and forwards requisitions
- Can approve or reject requisitions
- Cannot create requisitions

### User
- Basic read-only access
- Can view dashboards and reports
- No create/edit/delete permissions

## Default Permissions

### Dashboard
- View Dashboard

### Invoices Module
- View Invoices
- Create Invoices
- Edit Invoices
- Delete Invoices
- Download Invoice PDF

### Payments Module
- View Payments
- Create Payments
- Edit Payments
- Delete Payments

### Reports Module
- View Reports
- Generate Reports
- Export Reports

### AI Import Module
- Use AI Import
- Process Documents

### Procurement - Requisitions
- View Requisitions
- Create Requisitions
- Edit Requisitions
- Delete Requisitions
- Submit Requisitions
- Approve Requisitions
- Reject Requisitions

### Procurement - Purchase Orders
- View Purchase Orders
- Create Purchase Orders
- Edit Purchase Orders
- Delete Purchase Orders
- Approve Purchase Orders
- Receive Goods

### Suppliers Module
- View Suppliers
- Create Suppliers
- Edit Suppliers
- Delete Suppliers

### Admin - User Management
- View Users
- Create Users
- Edit Users
- Delete Users
- Assign Roles

### Admin - Role Management
- View Roles
- Create Roles
- Edit Roles
- Delete Roles
- Manage Permissions

### Admin - System Settings
- View Settings
- Edit Settings

### Admin - Audit Logs
- View Audit Logs

## Usage

### Initializing Roles and Permissions

1. Navigate to **Role Management** (Admin menu)
2. Click **"Initialize Default Roles & Permissions"**
3. This will create:
   - 10 default roles
   - 48 default permissions
   - Role-permission mappings

### Assigning Roles to Users

**Method 1: User Management**
1. Go to **Admin → User Management**
2. Edit a user
3. Select roles from the multi-select dropdown
4. Save changes

**Method 2: Role Management**
1. Go to **Role Management**
2. View role details
3. See all users assigned to this role

### Managing Role Permissions

1. Go to **Role Management**
2. Click on a role
3. Click **"Manage Permissions"**
4. Select/deselect permissions by module
5. Save changes

### Creating Custom Roles

1. Go to **Role Management → Create New Role**
2. Fill in:
   - **Name**: Internal identifier (e.g., `AccountsManager`)
   - **Display Name**: User-friendly name (e.g., "Accounts Manager")
   - **Description**: What this role does
3. Click **Create**
4. Assign permissions to the new role

### Using Authorization in Controllers

```csharp
// Require any logged-in user
[Authorize]
public class SomeController : Controller
{
}

// Require specific roles
[AuthorizeRoles(Roles.Admin, Roles.FinanceOfficer)]
public IActionResult Index()
{
}

// Require specific permission
[AuthorizePermission(Permissions.CreateInvoices)]
public IActionResult Create()
{
}
```

### Checking Permissions in Code

```csharp
// Inject authorization service
private readonly IAuthorizationService _authorizationService;

// Check if user has permission
bool canEdit = await _authorizationService.HasPermissionAsync(userId, Permissions.EditInvoices);

// Check if user has any of multiple permissions
bool canManage = await _authorizationService.HasAnyPermissionAsync(userId, 
    Permissions.EditInvoices, Permissions.DeleteInvoices);

// Check if user has specific role
bool isAdmin = await _authorizationService.HasRoleAsync(userId, Roles.Admin);
```

### Hiding UI Elements Based on Permissions

In Views:
```csharp
@inject InvoiceManagement.Services.IAuthorizationService AuthService

@{
    var userId = Context.Session.GetInt32("UserId");
    var canCreate = userId.HasValue && await AuthService.HasPermissionAsync(userId.Value, Permissions.CreateInvoices);
}

@if (canCreate)
{
    <a asp-action="Create" class="btn btn-primary">Create Invoice</a>
}
```

## Security Considerations

### Session-Based Authentication
- User credentials stored in session
- Session timeout: 2 hours (configurable)
- Secure cookies with HttpOnly flag

### Permission Checks
- Performed on every request via authorization filters
- Database queries optimized with eager loading
- Permissions cached per request

### Audit Trail
- All role assignments logged
- All permission changes logged
- User actions tracked in audit log

### Best Practices
1. **Principle of Least Privilege**: Give users only the permissions they need
2. **Regular Reviews**: Periodically review user roles and permissions
3. **Role Hierarchy**: Use roles hierarchically (specific roles have fewer permissions)
4. **Custom Roles**: Create custom roles for specific business needs
5. **Test Permissions**: Test new roles thoroughly before assigning to users

## Troubleshooting

### User Can't Access a Module
1. Check if user is logged in
2. Verify user has assigned roles: Admin → User Management
3. Check role has required permissions: Role Management → Role Details
4. Verify permission is active
5. Check browser console for errors

### Permission Check Not Working
1. Verify `IAuthorizationService` is registered in `Program.cs`
2. Check authorization attribute syntax
3. Ensure user ID is correctly stored in session
4. Review audit logs for access attempts

### Can't Delete a Role
- Roles assigned to users cannot be deleted
- Remove role from all users first
- Then delete the role

### After Adding New Permission
1. Add permission constant to `Permissions` class
2. Create permission in database (or reinitialize)
3. Assign permission to appropriate roles
4. Apply authorization attribute to controller/action

## Database Schema

```
Users
  ↓ (1:M)
UserRoles (Join Table)
  ↓ (M:1)
Roles
  ↓ (1:M)
RolePermissions (Join Table)
  ↓ (M:1)
Permissions
```

## Migration Required

After implementing RBAC, you need to create and apply a migration:

```bash
dotnet ef migrations add AddRoleBasedAccessControl
dotnet ef database update
```

This will create the following tables:
- `Roles`
- `Permissions`
- `UserRoles`
- `RolePermissions`

## Future Enhancements

Planned improvements:
- **Permission Groups**: Group related permissions
- **Dynamic Permissions**: Create permissions from UI
- **Role Templates**: Pre-configured role templates for common use cases
- **Permission Inheritance**: Child roles inherit parent permissions
- **Time-Based Permissions**: Temporary access grants
- **IP-Based Restrictions**: Limit access by IP address
- **Two-Factor Authentication**: Additional security layer

## Support

For assistance with RBAC system:
1. Review this guide
2. Check audit logs for access denials
3. Verify role and permission assignments
4. Contact system administrator

---

**Last Updated:** November 8, 2025  
**Version:** 1.0  
**Module:** Role-Based Access Control

