using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WebApplication1.Data;
using WebApplication1.Models;
using WebApplication1.Services;
using AppClaim = WebApplication1.Models.Claim;

namespace WebApplication1.Controllers
{
    public class ClaimsController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly ClaimVerificationService _claimVerificationService;
        private readonly NotificationService _notificationService;
        private readonly UserManager<User> _userManager;

        public ClaimsController(
            ApplicationDbContext context,
            UserManager<User> userManager,
            ClaimVerificationService claimVerificationService,
            NotificationService notificationService)
        {
            _context = context;
            _userManager = userManager;
            _claimVerificationService = claimVerificationService;
            _notificationService = notificationService;
        }

        // GET: Claims for logged-in user
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

        // GET: All claims read-only
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

        // GET: Pending claims (Submitted status)
        public async Task<IActionResult> PendingClaims(string searchString)
        {
            var pendingClaims = await _context.Claims
                .Where(c => c.Status == ClaimStatus.Submitted)
                .ToListAsync();

            var verifiedClaims = new List<(AppClaim claim, VerificationResult result)>();
            foreach (var claim in pendingClaims)
            {
                var result = _claimVerificationService.VerifyClaim(claim);
                verifiedClaims.Add((claim, result));
            }

            if (!string.IsNullOrEmpty(searchString))
            {
                verifiedClaims = verifiedClaims
                    .Where(x => x.claim.LecturerName.Contains(searchString)
                             || x.claim.Notes.Contains(searchString))
                    .ToList();
            }

            ViewData["CurrentFilter"] = searchString;
            return View(verifiedClaims);
        }

        // GET: Details
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null) return NotFound();

            var claim = await _context.Claims
                .Include(c => c.Documents)
                .FirstOrDefaultAsync(m => m.Id == id);

            if (claim == null) return NotFound();

            return View(claim);
        }

        // GET: Create claim
        public IActionResult Create() => View();

        // POST: Claims/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(AppClaim claim, List<IFormFile>? files)
        {
            claim.UserId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            claim.LecturerName = User.Identity?.Name ?? "Unknown Lecturer";

            if (claim.SubmittedAt == default)
                claim.SubmittedAt = DateTime.UtcNow;

            if (claim.Status == default)
                claim.Status = ClaimStatus.Submitted;

            claim.TotalAmount = claim.HoursWorked * claim.HourlyRate;

            if (ModelState.IsValid)
            {
                _context.Add(claim);
                await _context.SaveChangesAsync();

                await HandleFileUploads(claim, files);

                // 🔔 Notify Coordinator
                var coordinatorId = await _notificationService.GetCoordinatorUserIdAsync();
                if (!string.IsNullOrEmpty(coordinatorId))
                {
                    await _notificationService.AddNotificationAsync(
                        coordinatorId,
                        $"New claim submitted by {claim.LecturerName}."
                    );
                }

                TempData["Message"] = "Claim created successfully.";
                return RedirectToAction(nameof(Index));
            }

            TempData["Error"] = "Validation failed: " +
                string.Join("; ", ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage));
            return View(claim);
        }

        // GET: Edit
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null) return NotFound();

            var claim = await _context.Claims.FindAsync(id);
            if (claim == null) return NotFound();

            return View(claim);
        }

        // POST: Edit
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, AppClaim claim, List<IFormFile>? files)
        {
            if (id != claim.Id)
            {
                TempData["Error"] = "Claim ID mismatch.";
                return RedirectToAction(nameof(Index));
            }

            claim.TotalAmount = claim.HoursWorked * claim.HourlyRate;

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(claim);
                    await _context.SaveChangesAsync();

                    await HandleFileUploads(claim, files);

                    TempData["Message"] = "Claim updated successfully.";
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

        // GET: Delete
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null) return NotFound();

            var claim = await _context.Claims.FirstOrDefaultAsync(m => m.Id == id);

            if (claim == null) return NotFound();

            return View(claim);
        }

        // POST: Delete
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

        // --- Workflow Actions ---

        // Coordinator: Set Under Review
        [Authorize(Roles = "Coordinator")]
        public async Task<IActionResult> SetUnderReview(int id)
        {
            var claim = await _context.Claims.FindAsync(id);
            if (claim == null) return NotFound();

            if (claim.Status != ClaimStatus.Submitted)
            {
                TempData["Error"] = "Only submitted claims can be set to Under Review.";
                return RedirectToAction("PendingClaims");
            }

            claim.Status = ClaimStatus.UnderReview;
            await _context.SaveChangesAsync();

            TempData["Message"] = "Claim set to Under Review.";
            return RedirectToAction("PendingClaims");
        }

        // Coordinator: Send Back
        [Authorize(Roles = "Coordinator")]
        public async Task<IActionResult> SendBack(int id)
        {
            var claim = await _context.Claims.FindAsync(id);
            if (claim == null) return NotFound();

            if (claim.Status != ClaimStatus.Submitted && claim.Status != ClaimStatus.UnderReview)
            {
                TempData["Error"] = "Only submitted or under review claims can be sent back.";
                return RedirectToAction("PendingClaims");
            }

            claim.Status = ClaimStatus.SentBack;
            await _context.SaveChangesAsync();

            // 🔔 Notify Lecturer
            await _notificationService.AddNotificationAsync(
                claim.UserId,
                $"Your claim '{claim.Id}' has been sent back for corrections."
            );

            TempData["Message"] = "Claim sent back to lecturer.";
            return RedirectToAction("PendingClaims");
        }

        // Manager: Approve
        [Authorize(Roles = "Manager")]
        public async Task<IActionResult> Approve(int id)
        {
            var claim = await _context.Claims.FindAsync(id);
            if (claim == null) return NotFound();

            if (claim.Status != ClaimStatus.UnderReview)
            {
                TempData["Error"] = "Only claims under review can be approved.";
                return RedirectToAction("PendingClaims");
            }

            claim.Status = ClaimStatus.Approved;
            await _context.SaveChangesAsync();

            // 🔔 Notify HR
            var hrId = await _notificationService.GetHRUserIdAsync();
            if (!string.IsNullOrEmpty(hrId))
            {
                await _notificationService.AddNotificationAsync(
                    hrId,
                    $"Claim '{claim.Id}' has been approved and is ready for processing."
                );
            }

            TempData["Message"] = "Claim approved.";
            return RedirectToAction("PendingClaims");
        }

        // Manager: Reject
        [Authorize(Roles = "Manager")]
        public async Task<IActionResult> Reject(int id)
        {
            var claim = await _context.Claims.FindAsync(id);
            if (claim == null) return NotFound();

            if (claim.Status != ClaimStatus.UnderReview)
            {
                TempData["Error"] = "Only claims under review can be rejected.";
                return RedirectToAction("PendingClaims");
            }

            claim.Status = ClaimStatus.Rejected;
            await _context.SaveChangesAsync();

            // 🔔 Notify Lecturer
            await _notificationService.AddNotificationAsync(
                claim.UserId,
                $"Your claim '{claim.Id}' has been rejected by the Manager."
            );

            TempData["Message"] = "Claim rejected.";
            return RedirectToAction("PendingClaims");
        }

        // HR: Mark Processed
        [Authorize(Roles = "HR")]
        public async Task<IActionResult> MarkProcessed(int id)
        {
            var claim = await _context.Claims.FindAsync(id);
            if (claim == null) return NotFound();

            if (claim.Status != ClaimStatus.Approved)
            {
                TempData["Error"] = "Only approved claims can be processed.";
                return RedirectToAction("PendingClaims");
            }

            claim.Status = ClaimStatus.Processed;
            await _context.SaveChangesAsync();

            // 🔔 Notify Lecturer
            await _notificationService.AddNotificationAsync(
                claim.UserId,
                $"Your claim '{claim.Id}' has been processed by HR."
            );

            TempData["Message"] = "Claim marked as processed.";
            return RedirectToAction("PendingClaims");
        }

        // Helper: Check if claim exists
        private bool ClaimExists(int id) => _context.Claims.Any(e => e.Id == id);

        // Helper: Handle file uploads
        private async Task HandleFileUploads(AppClaim claim, List<IFormFile>? files)
        {
            if (files == null || files.Count == 0) return;

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
    }
}

