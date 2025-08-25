using Microsoft.AspNetCore.Mvc;
using WebApplication1.Data;
using WebApplication1.Models;
using Microsoft.EntityFrameworkCore;
using System.IO;

public class DocumentsController : Controller
{
    private readonly ApplicationDbContext _context;
    private readonly IWebHostEnvironment _env;

    public DocumentsController(ApplicationDbContext context, IWebHostEnvironment env)
    {
        _context = context;
        _env = env;
    }

    // Download a supporting document
    public async Task<IActionResult> Download(int id)
    {
        var doc = await _context.SupportingDocuments.FindAsync(id);
        if (doc == null) return NotFound();

        var filePath = Path.Combine(_env.WebRootPath.TrimEnd(Path.DirectorySeparatorChar), doc.FilePath.TrimStart('/'));
        if (!System.IO.File.Exists(filePath)) return NotFound();

        var mimeType = "application/octet-stream"; // generic
        return PhysicalFile(filePath, mimeType, doc.FileName);
    }

    // Delete a supporting document
    public async Task<IActionResult> Delete(int id)
    {
        var doc = await _context.SupportingDocuments.FindAsync(id);
        if (doc == null) return NotFound();

        var filePath = Path.Combine(_env.WebRootPath.TrimEnd(Path.DirectorySeparatorChar), doc.FilePath.TrimStart('/'));
        if (System.IO.File.Exists(filePath))
            System.IO.File.Delete(filePath);

        _context.SupportingDocuments.Remove(doc);
        await _context.SaveChangesAsync();

        // Redirect to the claim's details page
        return RedirectToAction("Details", "Claims", new { id = doc.ClaimId });
    }
}

