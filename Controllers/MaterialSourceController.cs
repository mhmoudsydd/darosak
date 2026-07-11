using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using darsakApi.Data;
using darsakApi.Models;

namespace darsakApi.Controllers
{
    [Route("api")]
    [ApiController]
    public class MaterialSourceController(AppDbContext context, IWebHostEnvironment env) : ControllerBase
    {
        private readonly AppDbContext _context = context;
        private readonly IWebHostEnvironment _env = env;

        // ────────────────────────────────────────────────────────────────
        // POST api/material-sources/upload-pdf
        // Content-Type: multipart/form-data
        // Fields: Name (string), MaterialId (int), File (PDF file)
        // ────────────────────────────────────────────────────────────────
        [Authorize]
        [HttpPost("material-sources/upload-pdf")]
        public async Task<IActionResult> UploadPdf(
            [FromForm] string name,
            [FromForm] int materialId,
            IFormFile file)
        {
            // 1. Validate inputs
            if (string.IsNullOrWhiteSpace(name))
                return BadRequest(new { message = "حقل اسم المصدر مطلوب." });

            if (materialId <= 0)
                return BadRequest(new { message = "معرّف المادة غير صحيح." });

            if (file == null || file.Length == 0)
                return BadRequest(new { message = "لم يتم إرسال أي ملف." });

            // 2. Allow only PDF files
            var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
            if (extension != ".pdf" || file.ContentType != "application/pdf")
                return BadRequest(new { message = "نوع الملف غير مدعوم. يُسمح فقط بملفات PDF." });

            // 3. Limit file size to 20 MB
            const long maxBytes = 20 * 1024 * 1024;
            if (file.Length > maxBytes)
                return BadRequest(new { message = "حجم الملف يتجاوز الحد الأقصى المسموح به (20 ميغابايت)." });

            // 4. Check the related Material exists
            var material = await _context.Materials.FindAsync(materialId);
            if (material == null)
                return NotFound(new { message = "المادة المرتبطة غير موجودة." });

            // 5. Save the file to wwwroot/uploads/pdfs/
            var webRoot = _env.WebRootPath ?? Path.Combine(_env.ContentRootPath, "wwwroot");
            var uploadsFolder = Path.Combine(webRoot, "uploads", "pdfs");
            Directory.CreateDirectory(uploadsFolder); // no-op if already exists

            var uniqueFileName = $"{Guid.NewGuid()}{extension}";
            var fullPath = Path.Combine(uploadsFolder, uniqueFileName);

            await using (var stream = new FileStream(fullPath, FileMode.Create))
                await file.CopyToAsync(stream);

            // 6. Relative path stored in DB — accessible via: GET /uploads/pdfs/<file>
            var relativePath = $"uploads/pdfs/{uniqueFileName}";

            // 7. Persist the MaterialSource record
            var source = new MaterialSource
            {
                Name = name,
                SourceType = SourceType.Pdf,
                PdfPath = relativePath,
                MaterialId = materialId
            };

            _context.MaterialSources.Add(source);
            await _context.SaveChangesAsync();

            // 8. Build the public URL the Flutter app can use to download/display the PDF
            var publicUrl = $"{Request.Scheme}://{Request.Host}/{relativePath}";

            return Ok(new
            {
                message = "تم رفع الملف بنجاح",
                source = new
                {
                    source.Id,
                    source.Name,
                    source.MaterialId,
                    source.PdfPath,
                    pdfUrl = publicUrl
                }
            });
        }

        // ────────────────────────────────────────────────────────────────
        // GET api/material-sources/{id}
        // Returns a single source with its public download URL
        // ────────────────────────────────────────────────────────────────
        [Authorize]
        [HttpGet("material-sources/{id}")]
        public async Task<IActionResult> GetSource(int id)
        {
            var source = await _context.MaterialSources
                .Include(s => s.Material)
                .FirstOrDefaultAsync(s => s.Id == id);

            if (source == null)
                return NotFound(new { message = "المصدر غير موجود." });

            var pdfUrl = source.SourceType == SourceType.Pdf && source.PdfPath != null
                ? $"{Request.Scheme}://{Request.Host}/{source.PdfPath}"
                : null;

            return Ok(new
            {
                message = "تم جلب المصدر بنجاح",
                source = new
                {
                    source.Id,
                    source.Name,
                    source.SourceType,
                    source.Url,
                    source.PdfPath,
                    pdfUrl,
                    source.MaterialId
                }
            });
        }

        // ────────────────────────────────────────────────────────────────
        // GET api/material-sources?materialId=1
        // Returns all sources for a given material
        // ────────────────────────────────────────────────────────────────
        [Authorize]
        [HttpGet("material-sources")]
        public async Task<IActionResult> GetSources([FromQuery] int materialId)
        {
            var sources = await _context.MaterialSources
                .Where(s => s.MaterialId == materialId)
                .ToListAsync();

            var result = sources.Select(s => new
            {
                s.Id,
                s.Name,
                s.SourceType,
                s.Url,
                s.PdfPath,
                pdfUrl = s.SourceType == SourceType.Pdf && s.PdfPath != null
                    ? $"{Request.Scheme}://{Request.Host}/{s.PdfPath}"
                    : null,
                s.MaterialId
            });

            return Ok(new
            {
                message = "تم جلب المصادر بنجاح",
                total = sources.Count,
                sources = result
            });
        }

        // ────────────────────────────────────────────────────────────────
        // DELETE api/material-sources/{id}
        // Deletes the DB record and the physical PDF file
        // ────────────────────────────────────────────────────────────────
        [Authorize]
        [HttpDelete("material-sources/{id}")]
        public async Task<IActionResult> DeleteSource(int id)
        {
            var source = await _context.MaterialSources.FindAsync(id);
            if (source == null)
                return NotFound(new { message = "المصدر غير موجود." });

            // Delete the physical file if it exists
            if (source.SourceType == SourceType.Pdf && source.PdfPath != null)
            {
                var webRoot = _env.WebRootPath ?? Path.Combine(_env.ContentRootPath, "wwwroot");
                var fullPath = Path.Combine(webRoot, source.PdfPath.Replace("/", Path.DirectorySeparatorChar.ToString()));
                if (System.IO.File.Exists(fullPath))
                    System.IO.File.Delete(fullPath);
            }

            _context.MaterialSources.Remove(source);
            await _context.SaveChangesAsync();

            return Ok(new { message = "تم حذف المصدر بنجاح." });
        }
    }
}
