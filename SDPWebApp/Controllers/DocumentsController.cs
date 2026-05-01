using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SDPWebApp.Data;
using SDPWebApp.Services;
using SDPWebApp.Models;

namespace SDPWebApp.Controllers
{
    public class DocumentsController(ApplicationDbContext context, DocumentProcessingService processingService) : Controller
    {
        private readonly ApplicationDbContext _context = context;
        private readonly DocumentProcessingService _processingService = processingService;

        public async Task<IActionResult> Index()
        {
            var documents = await _context.Documents
                .OrderByDescending(d => d.UploadedAt)
                .ToListAsync();

            ViewBag.TotalCount = documents.Count;
            ViewBag.UploadedCount = documents.Count(d => d.Status == DocumentStatus.Uploaded);
            ViewBag.ValidatedCount = documents.Count(d => d.Status == DocumentStatus.Validated);
            ViewBag.ReviewCount = documents.Count(d => d.Status == DocumentStatus.NeedsReview);
            ViewBag.RejectedCount = documents.Count(d => d.Status == DocumentStatus.Rejected);

            return View(documents);
        }

        [HttpPost]
        public async Task<IActionResult> Upload(IFormFile file)
        {
            if (file == null || file.Length == 0)
            {
                TempData["Error"] = "Please select the correct docuument.";
                return RedirectToAction(nameof(Index));
            }
            try
            {
                await _processingService.UploadAndProcess(file);
                TempData["Success"] = "Document successfully uploaded..";
            }
            catch (InvalidOperationException ex)
            {
                TempData["Error"] = ex.Message;
            }
            catch (Exception ex)
            {
                TempData["Error"] = "An error occurred: " + ex.Message;
            }

            return RedirectToAction(nameof(Index));
        }

        public async Task<IActionResult> Details(int id)
        {
            var document = await _context.Documents
                .Include(d => d.LineItems)
                .Include(d => d.ValidationIssues)
                .FirstOrDefaultAsync(m => m.Id == id);

            if (document == null) return NotFound();

            return View(document);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, Document updatedDoc, List<DocumentItem> NewItems)
        {
            var document = await _context.Documents
                .Include(d => d.LineItems)
                .Include(d => d.ValidationIssues)
                .FirstOrDefaultAsync(m => m.Id == id);

            if (document == null) return NotFound();
            document.SupplierName = updatedDoc.SupplierName;
            document.DocumentNumber = updatedDoc.DocumentNumber;
            document.TotalAmount = updatedDoc.TotalAmount;
            document.TaxAmount = updatedDoc.TaxAmount;
            document.Subtotal = updatedDoc.TotalAmount - updatedDoc.TaxAmount;
            document.Currency = updatedDoc.Currency;
            document.IssueDate = updatedDoc.IssueDate;
            document.DueDate = updatedDoc.DueDate;
            if (NewItems != null)
            {
                foreach (var item in NewItems)
                {
                    if (!string.IsNullOrWhiteSpace(item.Description))
                    {
                        item.DocumentId = document.Id;
                        item.LineTotal = (decimal)item.Quantity * item.UnitPrice;
                        document.LineItems.Add(item);
                    }
                }
            }
            _context.ValidationIssues.RemoveRange(document.ValidationIssues);
            _processingService.RunValidation(document, true);

            await _context.SaveChangesAsync();

            if (document.Status == DocumentStatus.Validated)
            {
                TempData["Success"] = "Document successfully validated and saved.";
                return RedirectToAction(nameof(Index));
            }
            else
            {
                TempData["Error"] = "Validation failed: " + string.Join(" | ", document.ValidationIssues.Select(v => v.ErrorMessage));
                return RedirectToAction(nameof(Details), new { id = document.Id });
            }
        }

        [HttpPost]
        public async Task<IActionResult> UpdateStatus(int id, string status)
        {
            var document = await _context.Documents
                .Include(d => d.LineItems)
                .Include(d => d.ValidationIssues)
                .FirstOrDefaultAsync(m => m.Id == id);

            if (document == null) return NotFound();

            if (Enum.TryParse<DocumentStatus>(status, out var newStatus))
            {
                if (newStatus == DocumentStatus.Validated)
                {
                    _processingService.RunValidation(document, true);

                    if (document.Status != DocumentStatus.Validated)
                    {
                        TempData["Error"] = "Validation failed. Check the document information.";
                        return RedirectToAction(nameof(Details), new { id });
                    }
                }
                else
                {
                    document.Status = newStatus;
                }

                await _context.SaveChangesAsync();
                TempData["Success"] = $"Status changed to {document.Status}.";
            }

            return RedirectToAction(nameof(Index));
        }
    }
}