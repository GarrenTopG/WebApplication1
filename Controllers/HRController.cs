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

namespace WebApplication1.Controllers
{
    [Authorize(Roles = "HR")]
    public class HRController : Controller
    {
        private readonly ApplicationDbContext _context;

        public HRController(ApplicationDbContext context) => _context = context;

        public IActionResult Reports() => View();

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> GenerateReport(DateTime startDate, DateTime endDate, string format)
        {
            endDate = endDate.Date.AddDays(1).AddTicks(-1);

            var claims = await _context.Claims
                .Where(c => c.Status == ClaimStatus.Approved &&
                            c.SubmittedAt >= startDate &&
                            c.SubmittedAt <= endDate)
                .ToListAsync();

            return format switch
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

        private IActionResult GenerateReportPDF(System.Collections.Generic.List<Claim> claims)
        {
            var pdfStream = new System.IO.MemoryStream();

            Document.Create(container =>
            {
                container.Page(page =>
                {
                    page.Margin(20);
                    page.Header().Text("Approved Claims Report").SemiBold().FontSize(20);
                    page.Content().Table(table =>
                    {
                        table.ColumnsDefinition(columns =>
                        {
                            columns.RelativeColumn();
                            columns.ConstantColumn(60);
                            columns.ConstantColumn(60);
                            columns.ConstantColumn(60);
                            columns.ConstantColumn(80);
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
                            table.Cell().Text(c.LecturerName);
                            table.Cell().Text(c.Id.ToString());
                            table.Cell().Text(c.HoursWorked.ToString("0.##"));
                            table.Cell().Text(c.HourlyRate.ToString("C"));
                            table.Cell().Text(c.TotalAmount.ToString("C"));
                        }
                    });
                });
            }).GeneratePdf(pdfStream);

            pdfStream.Position = 0;
            return File(pdfStream.ToArray(), "application/pdf",
                $"ApprovedClaims_{DateTime.Now:yyyyMMdd}.pdf");
        }
    }
}

