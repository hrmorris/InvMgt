# 🔒 Registration Update - Admin-Only Access

## Important Change

**Self-registration has been disabled for security purposes.**

## What Changed

### Before (REMOVED)
- ❌ Users could register their own accounts at `/Account/Register`
- ❌ "Create Account" button on login page
- ❌ Self-service account creation

### Now (CURRENT)
- ✅ **Admin-only user creation** through Admin Portal
- ✅ **First-time setup** for creating the initial admin account
- ✅ All subsequent users must be created by administrators
- ✅ Enhanced security and access control

## How to Get Started

### If This is a New System (No Users Yet)

1. **Visit the Application**:
   ```
   http://localhost:5000
   ```

2. **Click "Initial Setup"** on the login page

3. **Create First Admin Account**:
   - Fill in your administrator details:
     - Username
     - Password (minimum 6 characters)
     - Full Name
     - Email
     - Phone (optional)
     - Department
     - Facility Name
     - Facility Type (Hospital/Outstation)
   - System automatically assigns **"Admin"** role
   - Click **"Create Administrator Account"**

4. **Login** with your new credentials

5. **Start Creating Users**:
   - Go to **Administration** → **User Management**
   - Create accounts for your team members

### If Users Already Exist

1. **Contact an Administrator** to create an account for you

2. **Administrator will**:
   - Go to Administration → User Management
   - Click "Create New User"
   - Fill in your details
   - Assign appropriate role
   - Set temporary password
   - Provide you with credentials

3. **Login** with provided credentials at:
   ```
   http://localhost:5000/Account/Login
   ```

## User Creation Process (For Admins)

### Step-by-Step Guide

1. **Login as Admin**

2. **Navigate to User Management**:
   - Click **"Administration"** in sidebar
   - Click **"User Management"**

3. **Create New User**:
   - Click **"Create New User"** button
   
4. **Fill in User Details**:
   ```
   Username:     [unique username]
   Full Name:    [employee full name]
   Email:        [work email]
   Phone:        [contact number]
   Department:   [e.g., Procurement, Finance]
   Facility:     [e.g., Main Hospital]
   Facility Type: [Hospital or Outstation]
   Role:         [see available roles below]
   Status:       Active
   Password:     [temporary password]
   ```

5. **Click "Create"**

6. **Share Credentials**:
   - Provide username and password to the new user
   - Recommend they change password on first login (future feature)

## Available Roles

| Role | Purpose | Who Should Have It |
|------|---------|-------------------|
| **Admin** | Full system access, user management | IT staff, system administrators |
| **OIC** | Create requisitions | Officers In Charge at facilities |
| **Supervisor** | Approve requisitions (1st level) | Department supervisors |
| **Finance_Officer** | Screen for budget compliance | Finance & admin staff |
| **Health_Manager** | Final approval (outstation) | Health facility managers |
| **Hospital_Executive** | Final approval (hospital) | Hospital executives |
| **Finance_Manager** | Final approval (hospital) | Finance department managers |
| **Procurement_Officer** | Manage purchase orders | Procurement department staff |
| **User** | View-only access | General staff, viewers |

## Security Benefits

### Why This Change?

1. **Controlled Access**:
   - Verify employee identity before account creation
   - Prevent unauthorized access to sensitive financial data

2. **Proper Role Assignment**:
   - Ensure correct permissions from the start
   - Prevent accidental privilege escalation

3. **Audit Trail**:
   - Track who created each account
   - Maintain accountability

4. **Data Integrity**:
   - Correct organizational structure
   - Proper department and facility assignment

5. **Compliance**:
   - Meet organizational security policies
   - Satisfy audit requirements

## Frequently Asked Questions

### Q: I need an account. What do I do?

**A**: Contact your system administrator or IT department. They will create an account for you with the appropriate access level.

### Q: I forgot my password. How do I reset it?

**A**: Contact your system administrator. They can reset your password through the User Management portal.

### Q: Can I change my own password?

**A**: Password change functionality is coming in a future update. For now, contact your administrator.

### Q: What if no one has admin access?

**A**: If the system is completely new with no users, anyone can use the "Initial Setup" link to create the first admin account. After that, only admins can create users.

### Q: How do I know if I'm an admin?

**A**: After logging in, you'll see your role badge next to your name in the top navigation bar. If it says "Admin", you have full access.

### Q: Can admins create other admins?

**A**: Yes! Admins can create users with any role, including additional admin accounts.

### Q: What happens to inactive users?

**A**: Users with "Inactive" status cannot login. Admins can reactivate them at any time through User Management.

## Technical Details

### First-Time Setup Security

```csharp
public async Task<IActionResult> FirstTimeSetup()
{
    // Check if any users exist
    var users = await _adminService.GetAllUsersAsync();
    if (users.Any())
    {
        // Block access if users already exist
        return RedirectToAction(nameof(Login));
    }
    
    return View(new User());
}
```

### Features

- ✅ **One-time only**: FirstTimeSetup only works when database has zero users
- ✅ **Auto-Admin**: First user automatically gets Admin role
- ✅ **Secure**: Cannot be accessed once any user exists
- ✅ **Audited**: All user creation is logged

## URLs Reference

| Purpose | URL |
|---------|-----|
| **Login** | http://localhost:5000/Account/Login |
| **First-Time Setup** | http://localhost:5000/Account/FirstTimeSetup |
| **Admin Dashboard** | http://localhost:5000/Admin |
| **User Management** | http://localhost:5000/Admin/Users |
| **Create User** | http://localhost:5000/Admin/CreateUser |

## Migration Notes

### If You Had Users with Self-Registration

All existing users remain intact. This change only affects:
- New user creation going forward
- The registration page has been removed
- FirstTimeSetup page added for initial admin

### No Database Changes Required

This is a UI and controller-level change. No database migrations needed.

## Summary

✅ **Self-registration removed**  
✅ **Admin-only user creation**  
✅ **First-time setup for initial admin**  
✅ **Enhanced security**  
✅ **Better access control**  
✅ **Complete audit trail**  

---

## Quick Actions

### For New Systems
**Create your first admin**: http://localhost:5000/Account/FirstTimeSetup

### For Existing Systems
**Login**: http://localhost:5000/Account/Login  
**Admin Portal**: http://localhost:5000/Admin/Users (admins only)

### For Users
**Contact your administrator** to request an account

---

**Your system is now more secure with admin-controlled access!** 🔒

