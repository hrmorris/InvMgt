# Admin-Only User Registration

## 🔒 Security Policy

**All user accounts must be created by administrators only.**  
Self-registration has been disabled for security and control purposes.

## How It Works

### For First-Time Setup (No Users Exist)

1. **Navigate to the application**:
   ```
   http://localhost:5000
   ```

2. **Click "Initial Setup"** on the login page (only visible when no users exist)

3. **Create First Admin Account**:
   - The system will redirect to `/Account/FirstTimeSetup`
   - Fill in administrator details
   - System automatically assigns "Admin" role
   - Submit to create the first admin account

4. **Login** with your new admin credentials

### For All Subsequent Users

Once the first admin account is created:

1. **Admin Logs In**:
   - Go to http://localhost:5000
   - Login with admin credentials

2. **Navigate to User Management**:
   - Click **Administration** → **User Management**
   - Click **"Create New User"** button

3. **Fill in User Details**:
   - Username (unique)
   - Full Name
   - Email
   - Phone (optional)
   - Department
   - Facility Name
   - Facility Type (Hospital/Outstation)
   - Role (OIC, Supervisor, Finance_Officer, etc.)
   - Status (Active/Inactive/Suspended)
   - Initial Password

4. **Submit** to create the user

5. **Share Credentials**:
   - Provide username and password to the new user
   - User can login at http://localhost:5000/Account/Login

## User Creation Process

```
┌─────────────────────┐
│  Admin Portal       │
│  User Management    │
└──────────┬──────────┘
           │
           ▼
┌─────────────────────┐
│  Create New User    │
│  Form               │
└──────────┬──────────┘
           │
           ▼
┌─────────────────────┐
│  Assign Role &      │
│  Set Password       │
└──────────┬──────────┘
           │
           ▼
┌─────────────────────┐
│  Save to Database   │
│  Log Action         │
└──────────┬──────────┘
           │
           ▼
┌─────────────────────┐
│  Share Credentials  │
│  with New User      │
└─────────────────────┘
```

## Available User Roles

When creating users, admins can assign these roles:

| Role | Purpose | Access Level |
|------|---------|--------------|
| **Admin** | System administrators | Full access |
| **OIC** | Officers In Charge | Create requisitions |
| **Supervisor** | Department supervisors | Approve requisitions (1st level) |
| **Finance_Officer** | Finance & Admin officers | Screen requisitions for budget |
| **Health_Manager** | Health facility managers | Final approval (outstation) |
| **Hospital_Executive** | Hospital executives | Final approval (hospital) |
| **Finance_Manager** | Finance managers | Final approval (hospital) |
| **Procurement_Officer** | Procurement staff | Manage purchase orders |
| **User** | General users | View-only access |

## First-Time Setup Details

### What Happens

1. **Check for Existing Users**:
   - System verifies if any users exist in the database
   - If users exist, redirect to login with error message

2. **Force Admin Role**:
   - First user is automatically assigned "Admin" role
   - Cannot be changed during first-time setup

3. **Create Account**:
   - User details saved to database
   - Password stored (recommend BCrypt hashing for production)
   - Account status set to "Active"

4. **Audit Log**:
   - Action logged as "Created" by "System"

### Code Implementation

```csharp
// GET: Account/FirstTimeSetup
public async Task<IActionResult> FirstTimeSetup()
{
    var users = await _adminService.GetAllUsersAsync();
    if (users.Any())
    {
        TempData["ErrorMessage"] = "System already has users. Contact an administrator.";
        return RedirectToAction(nameof(Login));
    }
    
    return View(new User());
}

// POST: Account/FirstTimeSetup
[HttpPost]
[ValidateAntiForgeryToken]
public async Task<IActionResult> FirstTimeSetup(User user, string password)
{
    var users = await _adminService.GetAllUsersAsync();
    if (users.Any())
    {
        TempData["ErrorMessage"] = "System already has users. Contact an administrator.";
        return RedirectToAction(nameof(Login));
    }

    user.Role = "Admin"; // Force admin role
    user.Status = "Active";
    await _adminService.CreateUserAsync(user, password);
    
    return RedirectToAction(nameof(Login));
}
```

## URLs

- **Login**: http://localhost:5000/Account/Login
- **First-Time Setup**: http://localhost:5000/Account/FirstTimeSetup
- **User Management** (Admin): http://localhost:5000/Admin/Users
- **Create User** (Admin): http://localhost:5000/Admin/CreateUser

## Security Benefits

### Why Admin-Only Registration?

1. **Controlled Access**:
   - Admins verify user identity before creating accounts
   - Prevents unauthorized access

2. **Proper Role Assignment**:
   - Ensures users get correct permissions
   - Prevents privilege escalation

3. **Audit Trail**:
   - All user creation actions logged
   - Track who created each account

4. **User Verification**:
   - Admins verify employee details
   - Ensure user belongs to organization

5. **Data Integrity**:
   - Correct department and facility assignment
   - Maintains organizational structure

## Password Management

### Initial Password

Admins set the initial password when creating a user:
- Provide temporary password to new user
- User should change on first login (future enhancement)

### Password Reset

If a user forgets their password:
1. User contacts administrator
2. Admin goes to User Management
3. Admin edits the user
4. Admin sets a new temporary password
5. Admin shares new password with user

### Future Enhancements

```csharp
// Recommended: Add password change on first login
public bool MustChangePassword { get; set; } = true;

// Recommended: Add password reset via email
public string? PasswordResetToken { get; set; }
public DateTime? PasswordResetExpiry { get; set; }
```

## User Status Management

Admins can set user status:

- **Active**: User can login normally
- **Inactive**: User cannot login (account disabled)
- **Suspended**: Temporarily blocked (pending investigation)

To change user status:
1. Go to **Administration** → **User Management**
2. Click **Edit** on the user
3. Change **Status** field
4. Save changes

## Troubleshooting

### "System already has users" Error

**Issue**: Trying to access FirstTimeSetup when users exist

**Solution**: This is expected behavior. Contact an existing administrator to create your account.

### Can't Create First Admin

**Issue**: Database or migration issues

**Solution**:
```bash
cd /Users/hectormorris/Library/CloudStorage/OneDrive-Personal/Apps/InvMgt
dotnet ef database update
```

### Username Already Exists

**Issue**: Trying to create user with duplicate username

**Solution**: Choose a different unique username

### Password Not Working

**Issue**: User can't login with provided credentials

**Solution**: Admin should reset password in User Management

## Best Practices

### For Administrators

1. **Verify Identity**: Always verify user identity before creating accounts
2. **Use Strong Passwords**: Set strong initial passwords
3. **Assign Correct Roles**: Give users only necessary permissions
4. **Regular Audits**: Review user list regularly
5. **Deactivate Inactive Users**: Set status to "Inactive" for users who leave

### For New Users

1. **Change Password**: Change initial password after first login (when feature is added)
2. **Keep Credentials Secure**: Don't share username/password
3. **Report Issues**: Contact admin if you can't login
4. **Update Profile**: Keep contact information current

## Admin Portal Access

Only users with **Admin** role can access:

- **User Management**: Create, edit, delete users
- **Supplier Management**: Manage supplier master data
- **Audit Logs**: View all system activity
- **System Settings**: Configure system parameters
- **Dashboard**: View system statistics

All other roles have restricted access based on their assigned role.

## Summary

✅ **Self-registration disabled**  
✅ **Admin-only user creation**  
✅ **First-time setup for initial admin**  
✅ **Role-based access control**  
✅ **Complete audit trail**  
✅ **User status management**  

---

**Your system is now secured with admin-only user registration!** 🔒

To create your first admin account, visit: http://localhost:5000/Account/FirstTimeSetup

