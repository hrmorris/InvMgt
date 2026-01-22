using Microsoft.AspNetCore.Mvc;
using InvoiceManagement.Models;
using InvoiceManagement.Services;
using InvoiceManagement.Authorization;
using Microsoft.EntityFrameworkCore;
using InvoiceManagement.Data;

namespace InvoiceManagement.Controllers
{
    [AuthorizeRoles(Roles.SystemAdmin, Roles.Admin)]
    public class RoleController : Controller
    {
        private readonly IAuthorizationService _authorizationService;
        private readonly ApplicationDbContext _context;
        private readonly IAdminService _adminService;

        public RoleController(
            IAuthorizationService authorizationService,
            ApplicationDbContext context,
            IAdminService adminService)
        {
            _authorizationService = authorizationService;
            _context = context;
            _adminService = adminService;
        }

        // GET: Role/Index
        public async Task<IActionResult> Index()
        {
            var roles = await _context.Roles
                .Include(r => r.RolePermissions)
                .ThenInclude(rp => rp.Permission)
                .Include(r => r.UserRoles)
                .OrderBy(r => r.Name)
                .ToListAsync();

            return View(roles);
        }

        // GET: Role/Details/5
        public async Task<IActionResult> Details(int id)
        {
            var role = await _context.Roles
                .Include(r => r.RolePermissions)
                .ThenInclude(rp => rp.Permission)
                .Include(r => r.UserRoles)
                .ThenInclude(ur => ur.User)
                .FirstOrDefaultAsync(r => r.Id == id);

            if (role == null)
            {
                return NotFound();
            }

            return View(role);
        }

        // GET: Role/Create
        public IActionResult Create()
        {
            return View(new Role());
        }

        // POST: Role/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Role role)
        {
            if (ModelState.IsValid)
            {
                role.CreatedDate = DateTime.Now;
                _context.Roles.Add(role);
                await _context.SaveChangesAsync();

                var username = HttpContext.Session.GetString("Username");
                await _adminService.LogActionAsync(null, username ?? "Admin", "Create", "Role", role.Id, $"Created role: {role.Name}");

                TempData["SuccessMessage"] = $"Role '{role.DisplayName}' created successfully!";
                return RedirectToAction(nameof(Index));
            }

            return View(role);
        }

        // GET: Role/Edit/5
        public async Task<IActionResult> Edit(int id)
        {
            var role = await _context.Roles.FindAsync(id);
            if (role == null)
            {
                return NotFound();
            }

            return View(role);
        }

        // POST: Role/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, Role role)
        {
            if (id != role.Id)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(role);
                    await _context.SaveChangesAsync();

                    var username = HttpContext.Session.GetString("Username");
                    await _adminService.LogActionAsync(null, username ?? "Admin", "Update", "Role", role.Id, $"Updated role: {role.Name}");

                    TempData["SuccessMessage"] = $"Role '{role.DisplayName}' updated successfully!";
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!await RoleExists(role.Id))
                    {
                        return NotFound();
                    }
                    throw;
                }

                return RedirectToAction(nameof(Index));
            }

            return View(role);
        }

        // GET: Role/Delete/5
        public async Task<IActionResult> Delete(int id)
        {
            var role = await _context.Roles
                .Include(r => r.UserRoles)
                .FirstOrDefaultAsync(r => r.Id == id);

            if (role == null)
            {
                return NotFound();
            }

            return View(role);
        }

        // POST: Role/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var role = await _context.Roles
                .Include(r => r.UserRoles)
                .FirstOrDefaultAsync(r => r.Id == id);

            if (role == null)
            {
                return NotFound();
            }

            // Check if role is assigned to any users
            if (role.UserRoles.Any())
            {
                TempData["ErrorMessage"] = $"Cannot delete role '{role.DisplayName}' because it is assigned to {role.UserRoles.Count} user(s).";
                return RedirectToAction(nameof(Index));
            }

            var roleName = role.DisplayName;
            _context.Roles.Remove(role);
            await _context.SaveChangesAsync();

            var username = HttpContext.Session.GetString("Username");
            await _adminService.LogActionAsync(null, username ?? "Admin", "Delete", "Role", id, $"Deleted role: {roleName}");

            TempData["SuccessMessage"] = $"Role '{roleName}' deleted successfully!";
            return RedirectToAction(nameof(Index));
        }

        // GET: Role/ManagePermissions/5
        public async Task<IActionResult> ManagePermissions(int id)
        {
            var role = await _context.Roles
                .Include(r => r.RolePermissions)
                .ThenInclude(rp => rp.Permission)
                .FirstOrDefaultAsync(r => r.Id == id);

            if (role == null)
            {
                return NotFound();
            }

            var allPermissions = await _context.Permissions
                .Where(p => p.IsActive)
                .OrderBy(p => p.Module)
                .ThenBy(p => p.DisplayName)
                .ToListAsync();

            var assignedPermissionIds = role.RolePermissions.Select(rp => rp.PermissionId).ToList();

            ViewBag.Role = role;
            ViewBag.AllPermissions = allPermissions;
            ViewBag.AssignedPermissionIds = assignedPermissionIds;

            return View();
        }

        // POST: Role/UpdatePermissions/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdatePermissions(int id, List<int> permissionIds)
        {
            var role = await _context.Roles
                .Include(r => r.RolePermissions)
                .FirstOrDefaultAsync(r => r.Id == id);

            if (role == null)
            {
                return NotFound();
            }

            // Remove all existing permissions
            _context.RolePermissions.RemoveRange(role.RolePermissions);

            // Add selected permissions
            if (permissionIds != null && permissionIds.Any())
            {
                foreach (var permissionId in permissionIds)
                {
                    _context.RolePermissions.Add(new RolePermission
                    {
                        RoleId = id,
                        PermissionId = permissionId,
                        AssignedDate = DateTime.Now
                    });
                }
            }

            await _context.SaveChangesAsync();

            var username = HttpContext.Session.GetString("Username");
            await _adminService.LogActionAsync(null, username ?? "Admin", "Update", "RolePermissions", id, 
                $"Updated permissions for role: {role.Name}. Assigned {permissionIds?.Count ?? 0} permissions");

            TempData["SuccessMessage"] = $"Permissions updated successfully for role '{role.DisplayName}'!";
            return RedirectToAction(nameof(Details), new { id });
        }

        // GET: Role/InitializeDefaults
        public IActionResult InitializeDefaults()
        {
            return View();
        }

        // POST: Role/InitializeDefaults
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> InitializeDefaultsConfirmed()
        {
            try
            {
                await _authorizationService.InitializeRolesAndPermissionsAsync();
                
                var username = HttpContext.Session.GetString("Username");
                await _adminService.LogActionAsync(null, username ?? "Admin", "Initialize", "Roles", null, "Initialized default roles and permissions");

                TempData["SuccessMessage"] = "Default roles and permissions initialized successfully!";
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = $"Error initializing roles: {ex.Message}";
            }

            return RedirectToAction(nameof(Index));
        }

        private async Task<bool> RoleExists(int id)
        {
            return await _context.Roles.AnyAsync(e => e.Id == id);
        }
    }
}

