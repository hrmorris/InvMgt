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

            // Use raw SQL for byte array length check (EF Core can't translate .Length on byte[])
            var withContent = await _context.Database
                .SqlQueryRaw<int>("SELECT COUNT(*)::int AS \"Value\" FROM \"ImportedDocuments\" WHERE \"FileContent\" IS NOT NULL AND LENGTH(\"FileContent\") > 0")
                .SingleAsync();
            var emptyContent = totalDocs - withContent;

            // Stats by document type
            var byType = await _context.ImportedDocuments
                .GroupBy(d => d.DocumentType)
                .Select(g => new { Type = g.Key, Count = g.Count() })
                .ToListAsync();

            // Stats by processing status
            var byStatus = await _context.ImportedDocuments
                .GroupBy(d => d.ProcessingStatus)
                .Select(g => new { Status = g.Key, Count = g.Count() })
                .ToListAsync();

            // Count linked vs unlinked
            var linkedToInvoice = await _context.ImportedDocuments.CountAsync(d => d.InvoiceId != null);
            var linkedToPayment = await _context.ImportedDocuments.CountAsync(d => d.PaymentId != null);
            var unlinked = await _context.ImportedDocuments.CountAsync(d => d.InvoiceId == null && d.PaymentId == null);

            // Get ALL document IDs with their original filename and type (without loading content)
            var allDocs = await _context.ImportedDocuments
                .OrderByDescending(d => d.Id)
                .Select(d => new
                {
                    d.Id,
                    d.OriginalFileName,
                    d.FileSize,
                    d.ProcessingStatus,
                    d.DocumentType,
                    d.InvoiceId,
                    d.PaymentId,
                    d.UploadDate,
                    d.ContentType
                })
                .ToListAsync();

            return Json(new
            {
                totalDocuments = totalDocs,
                withContent,
                emptyContent,
                byType,
                byStatus,
                linkedToInvoice,
                linkedToPayment,
                unlinked,
                allDocuments = allDocs
            });
        }
    }
}
