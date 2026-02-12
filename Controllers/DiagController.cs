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

            // Get sample of last 10 docs metadata
            var sampleDocs = await _context.ImportedDocuments
                .OrderByDescending(d => d.Id)
                .Take(10)
                .Select(d => new
                {
                    d.Id,
                    d.OriginalFileName,
                    d.FileSize,
                    d.ProcessingStatus,
                    d.DocumentType,
                    d.InvoiceId,
                    d.UploadDate
                })
                .ToListAsync();

            // Check content size for each sample doc via raw SQL
            var sampleWithContent = new List<object>();
            foreach (var doc in sampleDocs)
            {
                var contentLen = await _context.Database
                    .SqlQueryRaw<int>($"SELECT COALESCE(LENGTH(\"FileContent\"), 0)::int AS \"Value\" FROM \"ImportedDocuments\" WHERE \"Id\" = {doc.Id}")
                    .SingleOrDefaultAsync();
                sampleWithContent.Add(new
                {
                    doc.Id,
                    doc.OriginalFileName,
                    doc.FileSize,
                    ContentLength = contentLen,
                    HasContent = contentLen > 0,
                    doc.ProcessingStatus,
                    doc.DocumentType,
                    doc.InvoiceId,
                    doc.UploadDate
                });
            }

            return Json(new
            {
                totalDocuments = totalDocs,
                withContent,
                emptyContent,
                sampleDocuments = sampleWithContent
            });
        }
    }
}
