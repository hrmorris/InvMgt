using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace InvoiceManagement.Authorization
{
    /// <summary>
    /// Authorization attribute that checks if the user has any of the required roles
    /// </summary>
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = true)]
    public class AuthorizeRolesAttribute : Attribute, IAuthorizationFilter
    {
        private readonly string[] _roles;

        public AuthorizeRolesAttribute(params string[] roles)
        {
            _roles = roles;
        }

        public void OnAuthorization(AuthorizationFilterContext context)
        {
            var userRole = context.HttpContext.Session.GetString("Role");
            var userId = context.HttpContext.Session.GetInt32("UserId");

            // Check if user is logged in
            if (!userId.HasValue)
            {
                context.Result = new RedirectToActionResult("Login", "Account", null);
                return;
            }

            // Admin users have authority to perform all roles
            if (userRole == "Admin" || userRole == "SystemAdmin")
            {
                return;
            }

            // Check if user has required role
            if (_roles.Length > 0 && !_roles.Contains(userRole ?? ""))
            {
                context.Result = new RedirectToActionResult("AccessDenied", "Account", null);
                return;
            }
        }
    }

    /// <summary>
    /// Authorization attribute that checks if the user has the required permission
    /// </summary>
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = true)]
    public class AuthorizePermissionAttribute : Attribute, IAsyncAuthorizationFilter
    {
        private readonly string[] _permissions;

        public AuthorizePermissionAttribute(params string[] permissions)
        {
            _permissions = permissions;
        }

        public async Task OnAuthorizationAsync(AuthorizationFilterContext context)
        {
            var userIdInt = context.HttpContext.Session.GetInt32("UserId");
            var userRole = context.HttpContext.Session.GetString("Role");

            // Check if user is logged in
            if (!userIdInt.HasValue)
            {
                context.Result = new RedirectToActionResult("Login", "Account", null);
                return;
            }

            // Admin users have authority to perform all roles and have all permissions
            if (userRole == "Admin" || userRole == "SystemAdmin")
            {
                return;
            }

            // Get authorization service from DI
            var authService = context.HttpContext.RequestServices
                .GetService<Services.IAuthorizationService>();

            if (authService == null)
            {
                context.Result = new RedirectToActionResult("Error", "Home", null);
                return;
            }

            bool hasPermission = false;
            foreach (var permission in _permissions)
            {
                if (await authService.HasPermissionAsync(userIdInt.Value, permission))
                {
                    hasPermission = true;
                    break;
                }
            }

            if (!hasPermission)
            {
                context.Result = new RedirectToActionResult("AccessDenied", "Account", null);
                return;
            }
        }
    }

    /// <summary>
    /// Simple authorization attribute that just checks if user is logged in
    /// </summary>
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Method)]
    public class AuthorizeAttribute : Attribute, IAuthorizationFilter
    {
        public void OnAuthorization(AuthorizationFilterContext context)
        {
            var userId = context.HttpContext.Session.GetInt32("UserId");

            if (!userId.HasValue)
            {
                context.Result = new RedirectToActionResult("Login", "Account", null);
            }
        }
    }
}

