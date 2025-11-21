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

        // -------------------------
        // User-specific claims
        // -------------------------
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

        public async Task<IActionResult> Details(int? id)
        {
            if (id == null) return NotFound();

            var claim = await _context.Claims
                .Include(c => c.Documents)
                .FirstOrDefaultAsync(m => m.Id == id);

            if (claim == null) return NotFound();

            return View(claim);
        }

        // -------------------------
        // Create/Edit/Delete
        // -------------------------
        public IActionResult Create() => View();

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(AppClaim claim, List<IFormFile>? files)
        {
            claim.UserId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            claim.LecturerName = User.Identity?.Name ?? "Unknown Lecturer";

            var userEmail = User.Identity?.Name;

            if (!string.IsNullOrEmpty(userEmail))
            {
                var lecturer = await _context.Lecturers
                    .FirstOrDefaultAsync(l => l.Email == userEmail);

                if (lecturer != null)
                {
                    claim.HourlyRate = lecturer.DefaultHourlyRate;
                }
            }

            // Get the current user's email safely

            if (!string.IsNullOrEmpty(userEmail))
            {
                var lecturer = await _context.Lecturers
                    .FirstOrDefaultAsync(l => l.Email == userEmail);

                if (lecturer != null)
                {
                    claim.HourlyRate = lecturer.DefaultHourlyRate;
                }
            }

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

                // Notify Coordinator
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

        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null) return NotFound();

            var claim = await _context.Claims.FindAsync(id);
            if (claim == null) return NotFound();

            return View(claim);
        }

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

        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null) return NotFound();

            var claim = await _context.Claims.FirstOrDefaultAsync(m => m.Id == id);

            if (claim == null) return NotFound();

            return View(claim);
        }

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

        // -------------------------
        // Pending Claims by Role
        // -------------------------
        // Redirector: sends user to their role-specific pending claims
        [Authorize]
        public IActionResult PendingClaims(string searchString)
        {
            if (User.IsInRole("Coordinator"))
                return RedirectToAction(nameof(PendingClaimsForCoordinator), new { searchString });

            if (User.IsInRole("Manager"))
                return RedirectToAction(nameof(PendingClaimsForManager), new { searchString });

            if (User.IsInRole("HR"))
                return RedirectToAction(nameof(PendingClaimsForHR), new { searchString });

            TempData["Error"] = "You do not have access to pending claims.";
            return RedirectToAction(nameof(Index));
        }


        // Coordinator: Pending Claims
        [Authorize(Roles = "Coordinator")]
        public async Task<IActionResult> PendingClaimsForCoordinator(string searchString)
        {
            var pendingClaims = await _context.Claims
                .Where(c => c.Status == ClaimStatus.Submitted || c.Status == ClaimStatus.UnderReview)
                .ToListAsync();

            var viewModel = pendingClaims
                .Select(c => new PendingClaimViewModel
                {
                    Claim = c,
                    Verification = _claimVerificationService.VerifyClaim(c)
                });

            if (!string.IsNullOrEmpty(searchString))
                viewModel = viewModel
                    .Where(vm => vm.Claim.LecturerName.Contains(searchString) || vm.Claim.Notes.Contains(searchString));

            ViewData["CurrentFilter"] = searchString;
            return View("PendingClaims", viewModel);
        }


        // Manager: Pending Claims
        [Authorize(Roles = "Manager")]
        public async Task<IActionResult> PendingClaimsForManager(string searchString)
        {
            var pendingClaims = await _context.Claims
                .Where(c => c.Status == ClaimStatus.UnderReview)
                .ToListAsync();

            var viewModel = pendingClaims
                .Select(c => new PendingClaimViewModel
                {
                    Claim = c,
                    Verification = null  // Managers do not need verification
                });

            if (!string.IsNullOrEmpty(searchString))
                viewModel = viewModel
                    .Where(vm => vm.Claim.LecturerName.Contains(searchString) || vm.Claim.Notes.Contains(searchString));

            ViewData["CurrentFilter"] = searchString;
            return View("PendingClaims", viewModel);
        }

        // HR: Pending Claims
        [Authorize(Roles = "HR")]
        public async Task<IActionResult> PendingClaimsForHR(string searchString)
        {
            var pendingClaims = await _context.Claims
                .Where(c => c.Status == ClaimStatus.Approved)
                .ToListAsync();

            var viewModel = pendingClaims
                .Select(c => new PendingClaimViewModel
                {
                    Claim = c,
                    Verification = null // HR does not need verification
                });

            if (!string.IsNullOrEmpty(searchString))
                viewModel = viewModel
                    .Where(vm => vm.Claim.LecturerName.Contains(searchString) || vm.Claim.Notes.Contains(searchString));

            ViewData["CurrentFilter"] = searchString;
            return View("PendingClaims", viewModel);
        }

        // Manager: Approve Claim
        // -------------------------
        [Authorize(Roles = "Manager")]
        public async Task<IActionResult> Approve(int id)
        {
            var claim = await _context.Claims.FindAsync(id);
            if (claim == null) return NotFound();

            claim.Status = ClaimStatus.Approved;
            await _context.SaveChangesAsync();

            TempData["Message"] = $"Claim {claim.Id} approved.";
            return RedirectToAction(nameof(PendingClaimsForManager));
        }

        [Authorize(Roles = "Manager")]
        public async Task<IActionResult> Reject(int id)
        {
            var claim = await _context.Claims.FindAsync(id);
            if (claim == null) return NotFound();

            claim.Status = ClaimStatus.Submitted; // Or whatever “rejected” workflow you want
            await _context.SaveChangesAsync();

            TempData["Message"] = $"Claim {claim.Id} rejected.";
            return RedirectToAction(nameof(PendingClaimsForManager));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Coordinator")]
        public async Task<IActionResult> SetUnderReview(int id, string? searchString)
        {
            var claim = await _context.Claims.FindAsync(id);
            if (claim == null)
            {
                TempData["Error"] = "Claim not found.";
                return RedirectToAction(nameof(PendingClaimsForCoordinator), new { searchString });
            }

            claim.Status = ClaimStatus.UnderReview;
            await _context.SaveChangesAsync();

            var managerId = await _notificationService.GetManagerUserIdAsync();
            if (!string.IsNullOrEmpty(managerId))
                await _notificationService.AddNotificationAsync(managerId, $"Claim #{claim.Id} is now under review.");

            TempData["Message"] = "Claim moved to Under Review.";
            return RedirectToAction(nameof(PendingClaimsForCoordinator), new { searchString });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Coordinator")]
        public async Task<IActionResult> SendBack(int id, string? searchString)
        {
            var claim = await _context.Claims.FindAsync(id);
            if (claim == null)
            {
                TempData["Error"] = "Claim not found.";
                return RedirectToAction(nameof(PendingClaimsForCoordinator), new { searchString });
            }

            claim.Status = ClaimStatus.SentBack;
            await _context.SaveChangesAsync();

            if (!string.IsNullOrEmpty(claim.UserId))
                await _notificationService.AddNotificationAsync(claim.UserId, $"Your claim #{claim.Id} has been sent back.");

            TempData["Message"] = "Claim sent back to Lecturer.";
            return RedirectToAction(nameof(PendingClaimsForCoordinator), new { searchString });
        }





        // -------------------------
        // Helpers
        // -------------------------
        private bool ClaimExists(int id) => _context.Claims.Any(e => e.Id == id);

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
