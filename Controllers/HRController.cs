using System;
using System.Linq;
using System.Threading.Tasks;
using ClosedXML.Excel;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using QuestPDF.Fluent;
using QuestPDF.Infrastructure;
using WebApplication1.Data;
using WebApplication1.Models;
using QuestPDF.Helpers;


namespace WebApplication1.Controllers
{
    [Authorize(Roles = "HR")]
    public class HRController : Controller
    {
        private readonly ApplicationDbContext _context;

        public HRController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: HR Dashboard / Report Page
        public IActionResult Reports()
        {
            return View();
        }

        // POST: Generate Report
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> GenerateReport(DateTime startDate, DateTime endDate, string format)
        {
            // Include the full day for endDate
            endDate = endDate.Date.AddDays(1).AddTicks(-1);

            // 1️⃣ Query approved claims in the date range
            var claims = await _context.Claims
                .Where(c => c.Status == ClaimStatus.Approved
                            && c.SubmittedAt >= startDate
                            && c.SubmittedAt <= endDate)
                .ToListAsync();

            // 2️⃣ Generate output based on requested format
            return format.ToLower() switch
            {
                "excel" => GenerateReportExcel(claims),
                "pdf" => GenerateReportPDF(claims),
                _ => RedirectToAction(nameof(Reports))
            };
        }

        private IActionResult GenerateReportExcel(System.Collections.Generic.List<Claim> claims)
        {
            using var workbook = new XLWorkbook();
            var ws = workbook.Worksheets.Add("Approved Claims");

            // Header row
            ws.Cell(1, 1).Value = "Lecturer";
            ws.Cell(1, 2).Value = "Claim ID";
            ws.Cell(1, 3).Value = "Hours Worked";
            ws.Cell(1, 4).Value = "Hourly Rate";
            ws.Cell(1, 5).Value = "Total Amount";

            int row = 2;
            foreach (var c in claims)
            {
                ws.Cell(row, 1).Value = c.LecturerName;
                ws.Cell(row, 2).Value = c.Id;
                ws.Cell(row, 3).Value = c.HoursWorked;
                ws.Cell(row, 4).Value = c.HourlyRate;
                ws.Cell(row, 5).Value = c.TotalAmount;
                row++;
            }

            using var stream = new System.IO.MemoryStream();
            workbook.SaveAs(stream);
            stream.Position = 0;

            return File(stream.ToArray(),
                        "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                        $"ApprovedClaims_{DateTime.Now:yyyyMMdd}.xlsx");
        }

        public IActionResult GenerateReportPDF(List<Claim> claims)
        {
            // Add this line
            QuestPDF.Settings.License = LicenseType.Community;

            if (claims == null || !claims.Any())
            {
                TempData["Error"] = "No approved claims found for the selected period.";
                return RedirectToAction(nameof(Reports));
            }

            try
            {
                using var pdfStream = new MemoryStream();

                Document.Create(container =>
                {
                    container.Page(page =>
                    {
                        page.Margin(20);

                        page.Header().Text("Approved Claims Report")
                            .SemiBold()
                            .FontSize(20)
                            .FontColor(Colors.Black);

                        page.Content().Table(table =>
                        {
                            table.ColumnsDefinition(columns =>
                            {
                                columns.RelativeColumn();      // Lecturer
                                columns.ConstantColumn(60);   // Claim ID
                                columns.ConstantColumn(60);   // Hours
                                columns.ConstantColumn(60);   // Rate
                                columns.ConstantColumn(80);   // Total
                            });

                            table.Header(header =>
                            {
                                header.Cell().Text("Lecturer");
                                header.Cell().Text("Claim ID");
                                header.Cell().Text("Hours");
                                header.Cell().Text("Rate");
                                header.Cell().Text("Total");
                            });

                            foreach (var c in claims)
                            {
                                table.Cell().Text(c.LecturerName ?? "N/A");
                                table.Cell().Text(c.Id.ToString());
                                table.Cell().Text(c.HoursWorked.ToString("0.##"));
                                table.Cell().Text(c.HourlyRate.ToString("C"));
                                table.Cell().Text(c.TotalAmount.ToString("C"));
                            }
                        });
                    });
                }).GeneratePdf(pdfStream);

                pdfStream.Position = 0;

                return File(
                    pdfStream.ToArray(),
                    "application/pdf",
                    $"ApprovedClaims_{DateTime.Now:yyyyMMdd}.pdf"
                );
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
                TempData["Error"] = "Failed to generate PDF. Please contact admin.";
                return RedirectToAction(nameof(Reports));
            }
        }


    }
}
