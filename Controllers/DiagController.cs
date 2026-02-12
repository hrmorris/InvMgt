using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using InvoiceManagement.Data;

namespace InvoiceManagement.Controllers
{
    /// <summary>
    /// Temporary diagnostic controller - REMOVE AFTER USE
    /// </summary>
    public class DiagController : Controller
    {
        private readonly ApplicationDbContext _context;

        public DiagController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: Diag/Docs?key=check2026
        [HttpGet]
        public async Task<IActionResult> Docs(string key)
        {
            if (key != "check2026") return NotFound();

            var totalDocs = await _context.ImportedDocuments.CountAsync();
            var withContent = await _context.ImportedDocuments.CountAsync(d => d.FileContent != null && d.FileContent.Length > 0);
            var emptyContent = totalDocs - withContent;

            // Get sample of last 10 docs
            var sampleDocs = await _context.ImportedDocuments
                .OrderByDescending(d => d.Id)
                .Take(10)
                .Select(d => new
                {
                    d.Id,
                    d.OriginalFileName,
                    d.FileSize,
                    ContentLength = d.FileContent != null ? d.FileContent.Length : 0,
                    HasContent = d.FileContent != null && d.FileContent.Length > 0,
                    d.ProcessingStatus,
                    d.DocumentType,
                    d.InvoiceId,
                    d.UploadDate
                })
                .ToListAsync();

            return Json(new
            {
                totalDocuments = totalDocs,
                withContent,
                emptyContent,
                sampleDocuments = sampleDocs
            });
        }
    }
}
