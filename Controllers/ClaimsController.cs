using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WebApplication1.Data;
using WebApplication1.Models;
using AppClaim = WebApplication1.Models.Claim;

namespace WebApplication1.Controllers
{
    public class ClaimsController : Controller
    {
        private readonly ApplicationDbContext _context;

        public ClaimsController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: Claims
        public async Task<IActionResult> Index(string searchString)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var claims = _context.Claims.Where(c => c.UserId == userId);

            if (!string.IsNullOrEmpty(searchString))
            {
                claims = claims.Where(c => c.LecturerName.Contains(searchString)
                                       || c.Notes.Contains(searchString));
            }

            ViewData["CurrentFilter"] = searchString;
            return View(await claims.ToListAsync());
        }

        // GET: Claims/ReadOnlyIndex
        public async Task<IActionResult> ReadOnlyIndex(string searchString)
        {
            var claims = _context.Claims.AsQueryable();

            if (!string.IsNullOrEmpty(searchString))
            {
                claims = claims.Where(c => c.LecturerName.Contains(searchString)
                                       || c.Notes.Contains(searchString));
            }

            ViewData["CurrentFilter"] = searchString;
            return View(await claims.ToListAsync());
        }

        // GET: Claims/PendingClaims
        public async Task<IActionResult> PendingClaims(string searchString)
        {
            var pendingClaims = _context.Claims.Where(c => c.Status == ClaimStatus.Pending);

            if (!string.IsNullOrEmpty(searchString))
            {
                pendingClaims = pendingClaims.Where(c => c.LecturerName.Contains(searchString)
                                                      || c.Notes.Contains(searchString));
            }

            ViewData["CurrentFilter"] = searchString;
            return View(await pendingClaims.ToListAsync());
        }

        // GET: Claims/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null) return NotFound();

            var claim = await _context.Claims
                .Include(c => c.Documents)
                .FirstOrDefaultAsync(m => m.Id == id);

            if (claim == null) return NotFound();

            return View(claim);
        }

        // GET: Claims/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null) return NotFound();

            var claim = await _context.Claims.FindAsync(id);
            if (claim == null) return NotFound();

            return View(claim);
        }

        // POST: Claims/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, AppClaim claim, List<IFormFile>? files)
        {
            if (id != claim.Id)
            {
                TempData["Error"] = "Claim ID mismatch.";
                return RedirectToAction(nameof(Index));
            }

            // --- Auto-calculate TotalAmount ---
            claim.TotalAmount = claim.HoursWorked * claim.HourlyRate;

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(claim);
                    await _context.SaveChangesAsync();

                    if (files != null && files.Count > 0)
                    {
                        var uploadsFolder = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot/uploads");
                        Directory.CreateDirectory(uploadsFolder);

                        foreach (var file in files)
                        {
                            if (file.Length > 0)
                            {
                                var uniqueFileName = Guid.NewGuid() + "_" + file.FileName;
                                var filePath = Path.Combine(uploadsFolder, uniqueFileName);

                                using var stream = new FileStream(filePath, FileMode.Create);
                                await file.CopyToAsync(stream);

                                _context.SupportingDocuments.Add(new SupportingDocument
                                {
                                    ClaimId = claim.Id,
                                    FileName = file.FileName,
                                    FilePath = "/uploads/" + uniqueFileName,
                                    UploadedAt = DateTime.UtcNow
                                });
                            }
                        }

                        await _context.SaveChangesAsync();
                        TempData["Message"] = "Claim updated successfully with new documents.";
                    }
                    else
                    {
                        TempData["Message"] = "Claim updated successfully.";
                    }
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!ClaimExists(claim.Id))
                        TempData["Error"] = "Claim no longer exists.";
                    else
                        throw;
                }

                return RedirectToAction(nameof(Index));
            }

            TempData["Error"] = "Failed to update claim. Please check the form and try again.";
            return View(claim);
        }


        // GET: Claims/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null) return NotFound();

            var claim = await _context.Claims.FirstOrDefaultAsync(m => m.Id == id);

            if (claim == null) return NotFound();

            return View(claim);
        }

        // POST: Claims/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var claim = await _context.Claims.FindAsync(id);
            if (claim != null)
            {
                _context.Claims.Remove(claim);
                await _context.SaveChangesAsync();
                TempData["Message"] = "Claim deleted successfully.";
            }
            else
            {
                TempData["Error"] = "Claim not found. It may have already been deleted.";
            }

            return RedirectToAction(nameof(Index));
        }

        // GET: Claims/Create
        public IActionResult Create()
        {
            return View();
        }

        // POST: Claims/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(AppClaim claim, List<IFormFile>? files)
        {
            // Auto-fill user info
            claim.UserId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            claim.LecturerName = User.Identity?.Name ?? "Unknown Lecturer";

            if (claim.SubmittedAt == default)
                claim.SubmittedAt = DateTime.UtcNow;

            if (claim.Status == default)
                claim.Status = ClaimStatus.Pending;

            // Auto-calculate TotalAmount
            claim.TotalAmount = claim.HoursWorked * claim.HourlyRate;

            if (ModelState.IsValid)
            {
                _context.Add(claim);
                await _context.SaveChangesAsync();

                // Handle file uploads (existing code)
                if (files != null && files.Count > 0)
                {
                    var uploadsFolder = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot/uploads");
                    Directory.CreateDirectory(uploadsFolder);

                    foreach (var file in files)
                    {
                        if (file.Length > 0)
                        {
                            var uniqueFileName = Guid.NewGuid() + "_" + file.FileName;
                            var filePath = Path.Combine(uploadsFolder, uniqueFileName);

                            using var stream = new FileStream(filePath, FileMode.Create);
                            await file.CopyToAsync(stream);

                            _context.SupportingDocuments.Add(new SupportingDocument
                            {
                                ClaimId = claim.Id,
                                FileName = file.FileName,
                                FilePath = "/uploads/" + uniqueFileName,
                                UploadedAt = DateTime.UtcNow
                            });
                        }
                    }

                    await _context.SaveChangesAsync();
                }

                TempData["Message"] = "Claim created successfully.";
                return RedirectToAction(nameof(Index));
            }

            var errors = ModelState.Values.SelectMany(v => v.Errors)
                                          .Select(e => e.ErrorMessage);
            TempData["Error"] = "Validation failed: " + string.Join("; ", errors);
            return View(claim);
        }


        // Approve a claim
        public async Task<IActionResult> Approve(int id)
        {
            var claim = await _context.Claims.FindAsync(id);
            if (claim == null)
            {
                TempData["Error"] = "Claim not found.";
                return RedirectToAction("PendingClaims");
            }

            claim.Status = ClaimStatus.Approved;
            await _context.SaveChangesAsync();

            TempData["Message"] = "Claim approved successfully.";
            return RedirectToAction("PendingClaims");
        }

        // Reject a claim
        public async Task<IActionResult> Reject(int id)
        {
            var claim = await _context.Claims.FindAsync(id);
            if (claim == null)
            {
                TempData["Error"] = "Claim not found.";
                return RedirectToAction("PendingClaims");
            }

            claim.Status = ClaimStatus.Rejected;
            await _context.SaveChangesAsync();

            TempData["Message"] = "Claim rejected successfully.";
            return RedirectToAction("PendingClaims");
        }

        private bool ClaimExists(int id)
        {
            return _context.Claims.Any(e => e.Id == id);
        }
    }
}

