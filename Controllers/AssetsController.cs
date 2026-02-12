using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using InvoiceManagement.Data;

namespace InvoiceManagement.Controllers
{
    /// <summary>
    /// Serves uploaded branding assets (logo, favicon, login background) from the database.
    /// Files are stored in the UploadedAssets table to persist across Cloud Run container restarts.
    /// </summary>
    public class AssetsController : Controller
    {
        private readonly ApplicationDbContext _context;

        public AssetsController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: /Assets/Get/{key}
        // e.g., /Assets/Get/logo, /Assets/Get/favicon, /Assets/Get/login-bg
        [HttpGet]
        [ResponseCache(Duration = 3600, Location = ResponseCacheLocation.Client)]
        public async Task<IActionResult> Get(string id)
        {
            if (string.IsNullOrEmpty(id))
                return NotFound();

            var asset = await _context.UploadedAssets
                .AsNoTracking()
                .FirstOrDefaultAsync(a => a.AssetKey == id);

            if (asset == null || asset.FileContent == null || asset.FileContent.Length == 0)
                return NotFound();

            return File(asset.FileContent, asset.ContentType, asset.FileName);
        }
    }
}
