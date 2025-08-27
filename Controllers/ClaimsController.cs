using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using WebApplication1.Data;
using WebApplication1.Models;

namespace WebApplication1.Controllers
{
    public class ClaimsController : Controller //controls how claims are handled
    {
        private readonly ApplicationDbContext _context;

        public ClaimsController(ApplicationDbContext context)
        {
            _context = context;//This _context variable is used everywhere to fetch, add, update, and delete claims.
        }
        //Connects the controller to the database through ApplicationDbContext.

        // GET: Claims
        // Shows all claims in a table.
        //If the user types something in the search bar, it filters by LecturerName or Notes.
        public async Task<IActionResult> Index(string searchString)
        {
            var claims = from c in _context.Claims
                         select c;

            if (!string.IsNullOrEmpty(searchString))
            {
                claims = claims.Where(c => c.LecturerName.Contains(searchString)
                                       || c.Notes.Contains(searchString));
            }

            ViewData["CurrentFilter"] = searchString;

            return View(await claims.ToListAsync());
        }


        // GET: Claims/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null || _context.Claims == null)
            {
                return NotFound();
            }

            var claim = await _context.Claims
                .Include(c => c.Documents)   // load supporting documents
                .FirstOrDefaultAsync(m => m.ClaimId == id);

            if (claim == null)
            {
                return NotFound();
            }

            return View(claim);
        }

        // GET: Claims/PendingClaims

        //Shows only claims waiting for approval
        //Supports searching
        public async Task<IActionResult> PendingClaims(string searchString)
        {
            var pendingClaims = from c in _context.Claims
                        where c.Status == ClaimStatus.Pending
                        select c;

            if (!string.IsNullOrEmpty(searchString))
            {
                pendingClaims = pendingClaims.Where(c => c.LecturerName.Contains(searchString) 
                                              || c.Notes.Contains(searchString));
            }

    ViewData["CurrentFilter"] = searchString;

            return View(await pendingClaims.ToListAsync());
        }


        // GET: Claims/Edit/5
        //Loads the existing claim into a form so the user can edit.
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var claim = await _context.Claims.FindAsync(id);
            if (claim == null)
            {
                return NotFound();
            }
            return View(claim);
        }

        // POST: Claims/Edit/5
        // Updates claim info in the database.
        // Handles file uploads (saves them in wwwroot/uploads).
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, Claim claim, List<IFormFile>? files)
        {
            if (id != claim.ClaimId)
            {
                TempData["Error"] = "Claim ID mismatch.";
                return RedirectToAction(nameof(Index));
            }

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(claim);
                    await _context.SaveChangesAsync();

                    // handle new uploaded files (optional)
                    if (files != null && files.Count > 0)
                    {
                        var uploadsFolder = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot/uploads");
                        if (!Directory.Exists(uploadsFolder))
                        {
                            Directory.CreateDirectory(uploadsFolder);
                        }

                        foreach (var file in files)
                        {
                            if (file.Length > 0)
                            {
                                var uniqueFileName = Guid.NewGuid().ToString() + "_" + file.FileName;
                                var filePath = Path.Combine(uploadsFolder, uniqueFileName);

                                using (var stream = new FileStream(filePath, FileMode.Create))
                                {
                                    await file.CopyToAsync(stream);
                                }

                                var document = new SupportingDocument
                                {
                                    ClaimId = claim.ClaimId,
                                    FileName = file.FileName,
                                    FilePath = "/uploads/" + uniqueFileName,
                                    UploadedAt = DateTime.UtcNow
                                };

                                _context.SupportingDocuments.Add(document);
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
                    if (!ClaimExists(claim.ClaimId))
                    {
                        TempData["Error"] = "Claim no longer exists.";
                        return RedirectToAction(nameof(Index));
                    }
                    else
                    {
                        throw;
                    }
                }
                return RedirectToAction(nameof(Index));
            }

            TempData["Error"] = "Failed to update claim. Please check the form and try again.";
            return View(claim);
        }



        // GET: Claims/Create
        // Shows a blank form for a new claim.
        public IActionResult Create()
        {
            return View();
        }

        // POST: Claims/Create
        // Saves the new claim to the database.
        // Also saves supporting documents if uploaded.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Claim claim, List<IFormFile>? files)
        {
            if (ModelState.IsValid)
            {
                _context.Add(claim);
                await _context.SaveChangesAsync();

                // handle supporting documents
                if (files != null && files.Count > 0)
                {
                    var uploadsFolder = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot/uploads");

                    if (!Directory.Exists(uploadsFolder))
                    {
                        Directory.CreateDirectory(uploadsFolder); // auto-create if missing
                    }

                    foreach (var file in files)
                    {
                        if (file.Length > 0)
                        {
                            var uniqueFileName = Guid.NewGuid().ToString() + "_" + file.FileName;
                            var filePath = Path.Combine(uploadsFolder, uniqueFileName);

                            using (var stream = new FileStream(filePath, FileMode.Create))
                            {
                                await file.CopyToAsync(stream);
                            }

                            var document = new SupportingDocument
                            {
                                ClaimId = claim.ClaimId,
                                FileName = file.FileName,
                                FilePath = "/uploads/" + uniqueFileName,
                                UploadedAt = DateTime.UtcNow
                            };

                            _context.SupportingDocuments.Add(document);
                        }
                    }

                    await _context.SaveChangesAsync();
                    TempData["Message"] = "Claim created successfully with supporting documents.";
                }
                else
                {
                    TempData["Message"] = "Claim created successfully.";
                }

                return RedirectToAction(nameof(Index));
            }

            TempData["Error"] = "Failed to create claim. Please check the form and try again.";
            return View(claim);
        }




        // GET: Claims/Delete/5
        // Shows a confirmation page before deleting a claim.
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var claim = await _context.Claims
                .FirstOrDefaultAsync(m => m.ClaimId == id);
            if (claim == null)
            {
                return NotFound();
            }

            return View(claim);
        }

        // POST: Claims/Delete/5
        //Actually deletes the claim from the database.
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
            return _context.Claims.Any(e => e.ClaimId == id);
        }
    }
}
