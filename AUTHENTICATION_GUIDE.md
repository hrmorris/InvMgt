# Authentication & Login System Guide

## Overview
The Invoice Management System now includes a comprehensive authentication system with user management, role-based access, and session management.

## Features

### 1. **User Authentication**
- **Login/Logout**: Session-based authentication
- **User Registration**: Self-service account creation
- **Password Management**: Simple password system (demo - use BCrypt in production)
- **Session Tracking**: 2-hour session timeout
- **Audit Logging**: All login/logout actions are logged

### 2. **User Roles**
The system supports multiple user roles:
- **Admin**: Full system access and administration
- **OIC**: Officers In Charge at health facilities
- **Supervisor**: Approve requisitions from OICs
- **Finance_Officer**: Screen requisitions for budget/cost compliance
- **Procurement_Officer**: Manage purchase orders
- **Health_Manager**: Final approval for outstation facilities
- **Hospital_Executive**: Final approval for hospital requisitions
- **Finance_Manager**: Final approval for hospital requisitions
- **User**: Basic access

### 3. **User Management (Admin Portal)**
Administrators can:
- Create new users with roles
- Edit user details and status
- View all users by role or facility
- Deactivate or suspend users
- Change user passwords
- View user login history

## Getting Started

### First Time Setup

1. **Navigate to the application**:
   ```
   http://localhost:5000
   ```

2. **Create your first admin account** (one-time only):
   - Click "Initial Setup" on the login page
   - Fill in administrator details
   - Set a password
   - System automatically assigns "Admin" role
   - Submit

3. **Login**:
   - Enter your username and password
   - Click "Sign In"

**Important**: After the first admin is created, all subsequent users must be created through the Admin Portal by administrators.

### Creating Additional Users

**As an Admin:**
1. Go to **Administration → User Management**
2. Click "Create New User"
3. Fill in user details:
   - Username (unique)
   - Full Name
   - Email
   - Department
   - Facility (Hospital/Outstation)
   - Role
4. Set initial password
5. Submit

## Authentication Flow

```
┌─────────────┐
│   Login     │
│   Page      │
└──────┬──────┘
       │
       ▼
┌─────────────────┐
│  Authenticate   │
│  (Session)      │
└──────┬──────────┘
       │
       ▼
┌─────────────────┐
│  Set Session    │
│  Variables:     │
│  - UserId       │
│  - Username     │
│  - FullName     │
│  - Role         │
└──────┬──────────┘
       │
       ▼
┌─────────────────┐
│  Log Action     │
│  (Audit Trail)  │
└──────┬──────────┘
       │
       ▼
┌─────────────────┐
│  Redirect to    │
│  Dashboard      │
└─────────────────┘
```

## Session Management

- **Duration**: 2 hours idle timeout
- **Storage**: In-memory (server-side)
- **Security**: HttpOnly cookies
- **Session Data**:
  - User ID
  - Username
  - Full Name
  - Role

## User Interface Elements

### Navigation Bar
When logged in, the top navigation shows:
- User's full name
- Role badge
- Logout button

When not logged in:
- Login button

### Protected Features
All features are accessible when logged in. The role determines workflow permissions:
- **Requisitions**: OICs create, Supervisors approve
- **Finance Screening**: Finance Officers review budget/costs
- **Final Approval**: Health Managers or Hospital Executives
- **Purchase Orders**: Procurement Officers
- **Admin Portal**: Admins only

## API Endpoints

### Account Controller
- `GET /Account/Login` - Display login page
- `POST /Account/Login` - Authenticate user
- `GET /Account/Logout` - End session
- `GET /Account/Register` - Display registration form
- `POST /Account/Register` - Create new account

### Admin Controller (User Management)
- `GET /Admin/Users` - List all users
- `GET /Admin/CreateUser` - User creation form
- `POST /Admin/CreateUser` - Create user
- `GET /Admin/EditUser/{id}` - Edit user form
- `POST /Admin/EditUser/{id}` - Update user
- `GET /Admin/DeleteUser/{id}` - Delete confirmation
- `POST /Admin/DeleteUser/{id}` - Delete user

## Security Notes

### Current Implementation (Demo)
- Passwords are stored as plain text
- Simple username/password authentication
- Session-based authorization

### Production Recommendations
1. **Password Hashing**:
   ```csharp
   // Install BCrypt.Net-Next
   using BCrypt.Net;
   
   // Hash password
   string hashedPassword = BCrypt.HashPassword(password);
   
   // Verify password
   bool isValid = BCrypt.Verify(password, user.PasswordHash);
   ```

2. **HTTPS**: Always use HTTPS in production
3. **JWT Tokens**: Consider JWT for API authentication
4. **MFA**: Implement multi-factor authentication
5. **Password Policy**: Enforce strong passwords
6. **Rate Limiting**: Prevent brute force attacks
7. **Session Security**: Use secure, HttpOnly cookies

## Audit Logging

All authentication events are logged:
- **Login**: Records user, timestamp, IP
- **Logout**: Records user, timestamp
- **Account Creation**: Records creator, new user details
- **Password Changes**: Records who changed what

View logs at: **Administration → Audit Logs**

## Troubleshooting

### Can't Login
1. Verify username is correct (case-sensitive)
2. Check if account status is "Active"
3. Try registering a new account if first time
4. Contact admin to reset password

### Session Expires Quickly
- Default timeout is 2 hours
- Admins can change this in `Program.cs`:
  ```csharp
  options.IdleTimeout = TimeSpan.FromHours(4);
  ```

### Forgot Password
Currently, admin must reset password via:
1. **Administration → User Management**
2. Select user
3. Edit user
4. Set new password

## Next Steps

1. ✅ Basic authentication working
2. ✅ User management complete
3. ✅ Session management active
4. ✅ Audit logging enabled
5. ⏳ Add password reset via email
6. ⏳ Implement role-based authorization attributes
7. ⏳ Add API authentication (JWT)

---

**Your authentication system is now live!** 🎉

Access it at: http://localhost:5000/Account/Login

