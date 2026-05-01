using CsvHelper;
using System.Globalization;
using SDPWebApp.Data;
using SDPWebApp.Models;
using Microsoft.EntityFrameworkCore;
using System.Text.RegularExpressions;
using iText.Kernel.Pdf;
using iText.Kernel.Pdf.Canvas.Parser;
using iText.Kernel.Pdf.Canvas.Parser.Listener;
using System.Text;
#pragma warning disable SYSLIB1045

namespace SDPWebApp.Services
{
    public class DocumentProcessingService
    {
        private readonly ApplicationDbContext _context;
        private readonly string _uploadFolder = Path.Combine(Directory.GetCurrentDirectory(), "Uploads");
        private static readonly Regex SupplierRx = new(@"Supplier:\s*(.*)", RegexOptions.Compiled);
        private static readonly Regex NumberRx = new(@"Number:\s*(.*)", RegexOptions.Compiled);
        private static readonly Regex IssueDateRx = new(@"(?:Issue\s*Date|Date):\s*([\d\.-/]+)", RegexOptions.Compiled);
        private static readonly Regex DueDateRx = new(@"(?:Due\s*Date|Pay\s*by|Due):\s*([\d\.-/]+)", RegexOptions.Compiled);
        private static readonly Regex SubtotalRx = new(@"Subtotal\s+([\d,.]+)", RegexOptions.Compiled);
        private static readonly Regex TaxRx = new(@"Tax.*?\s+([\d,.]+)", RegexOptions.Compiled);
        private static readonly Regex TotalRx = new(@"Total\s+([\d,.]+)", RegexOptions.Compiled);
        private static readonly Regex ItemMatchRx = new(@"(.*?)\s+(\d+)\s+([\d,.]+)\s+([\d,.]+)", RegexOptions.Compiled);
        private static readonly Regex TxtInvoiceRx = new(@"Invoice\s+([^\n\r]+)", RegexOptions.Compiled);
        private static readonly Regex TxtTotalRx = new(@"Total:\s*([\d,.]+)", RegexOptions.Compiled);
        private static readonly Regex TxtCurrencyRx = new(@"Total:.*?([A-Z]{3})", RegexOptions.Compiled);
        private static readonly Regex TxtDateRx = new(@"Date:\s*([\d\.-/]+)", RegexOptions.Compiled);
        private static readonly Regex TxtDueDateRx = new(@"Due\s*Date:\s*([\d\.-/]+)", RegexOptions.Compiled);

        public DocumentProcessingService(ApplicationDbContext context)
        {
            _context = context;
            if (!Directory.Exists(_uploadFolder)) Directory.CreateDirectory(_uploadFolder);
        }

        public async Task<Document> UploadAndProcess(IFormFile file)
        {
            var filePath = Path.Combine(_uploadFolder, file.FileName);
            using (var stream = new FileStream(filePath, FileMode.Create))
            {
                await file.CopyToAsync(stream);
            }

            var document = new Document
            {
                FileName = file.FileName,
                FilePath = filePath,
                Status = DocumentStatus.Uploaded,
                UploadedAt = DateTime.UtcNow,
                LineItems = [],
                ValidationIssues = []
            };

            string extension = Path.GetExtension(file.FileName).ToLower();

            try
            {
                if (extension == ".csv") await ProcessCsv(document);
                else if (extension == ".pdf") await ProcessPdf(document);
                else if (extension == ".txt") await ProcessTxt(document);
                else throw new Exception("Unsupported file format");
                RunValidation(document, false);

                if (!string.IsNullOrEmpty(document.DocumentNumber) && document.DocumentNumber != "Unknown")
                {
                    bool exists = await _context.Documents
                        .AnyAsync(d => d.DocumentNumber == document.DocumentNumber);

                    if (exists)
                    {
                        if (File.Exists(filePath)) File.Delete(filePath);
                        throw new InvalidOperationException($"Document number '{document.DocumentNumber}' already exists in the system.");
                    }
                }

                _context.Documents.Add(document);
                await _context.SaveChangesAsync();
                return document;
            }
            catch (Exception)
            {
                if (File.Exists(filePath)) File.Delete(filePath);
                throw;
            }
        }

        private static async Task ProcessCsv(Document doc)
        {
            using var reader = new StreamReader(doc.FilePath);
            using var csv = new CsvReader(reader, CultureInfo.InvariantCulture);

            var records = csv.GetRecords<dynamic>().ToList();
            decimal calculatedTotal = 0;

            foreach (var row in records)
            {
                if (row is not IDictionary<string, object> rowDict) continue;

                rowDict.TryGetValue("desc", out var descVal);
                rowDict.TryGetValue("qty", out var qtyVal);
                rowDict.TryGetValue("price", out var priceVal);

                var item = new DocumentItem
                {
                    Description = descVal?.ToString() ?? "",
                    Quantity = double.TryParse(qtyVal?.ToString(), out var q) ? q : 0,
                    UnitPrice = decimal.TryParse(priceVal?.ToString(), out var p) ? p : 0
                };
                item.LineTotal = (decimal)item.Quantity * item.UnitPrice;
                doc.LineItems.Add(item);
                calculatedTotal += item.LineTotal;
            }

            doc.TotalAmount = calculatedTotal;
            doc.Subtotal = calculatedTotal;
            doc.SupplierName = "CSV Import";
            doc.Currency = "BAM";
            doc.DocumentNumber = Path.GetFileNameWithoutExtension(doc.FileName);
            doc.Type = DocumentType.Invoice;
            doc.IssueDate = DateTime.Now;
        }

        private static async Task ProcessPdf(Document doc)
        {
            string text = ExtractTextFromPdf(doc.FilePath);
            if (string.IsNullOrWhiteSpace(text)) return;

            doc.Type = text.Contains("Purchase Order", StringComparison.OrdinalIgnoreCase)
                       ? DocumentType.PurchaseOrder : DocumentType.Invoice;

            doc.SupplierName = SupplierRx.Match(text).Groups[1].Value.Trim();
            doc.DocumentNumber = NumberRx.Match(text).Groups[1].Value.Trim();

            if (DateTime.TryParse(IssueDateRx.Match(text).Groups[1].Value, out DateTime iDate))
                doc.IssueDate = iDate;
            if (DateTime.TryParse(DueDateRx.Match(text).Groups[1].Value, out DateTime dDate))
                doc.DueDate = dDate;

            doc.Subtotal = ParseDecimal(SubtotalRx.Match(text).Groups[1].Value);
            doc.TaxAmount = ParseDecimal(TaxRx.Match(text).Groups[1].Value);
            doc.TotalAmount = ParseDecimal(TotalRx.Match(text).Groups[1].Value);
            doc.Currency = "USD";

            var lines = text.Split('\n');
            foreach (var line in lines)
            {
                var itemMatch = ItemMatchRx.Match(line);
                if (itemMatch.Success && !line.Contains("Description") && !line.Contains("Total"))
                {
                    doc.LineItems.Add(new DocumentItem
                    {
                        Description = itemMatch.Groups[1].Value.Trim(),
                        Quantity = double.Parse(itemMatch.Groups[2].Value),
                        UnitPrice = ParseDecimal(itemMatch.Groups[3].Value),
                        LineTotal = ParseDecimal(itemMatch.Groups[4].Value)
                    });
                }
            }
        }

        private static async Task ProcessTxt(Document doc)
        {
            string text = await File.ReadAllTextAsync(doc.FilePath);
            if (string.IsNullOrWhiteSpace(text)) return;

            doc.DocumentNumber = TxtInvoiceRx.Match(text).Groups[1].Value.Trim();
            doc.TotalAmount = ParseDecimal(TxtTotalRx.Match(text).Groups[1].Value);
            doc.Currency = TxtCurrencyRx.Match(text).Groups[1].Value ?? "BAM";

            if (DateTime.TryParse(TxtDateRx.Match(text).Groups[1].Value, out DateTime iDate)) doc.IssueDate = iDate;
            if (DateTime.TryParse(TxtDueDateRx.Match(text).Groups[1].Value, out DateTime dDate)) doc.DueDate = dDate;

            doc.SupplierName = "TXT Import";
            doc.Type = DocumentType.Invoice;
            doc.Subtotal = doc.TotalAmount;
        }

        public void RunValidation(Document doc, bool isManualEntry = false)
        {
            doc.ValidationIssues.Clear();

            if (doc.LineItems == null || doc.LineItems.Count == 0)
            {
                AddIssue(doc, "The document does not contain any items.", "Missing Items");
            }
            else
            {
                if (doc.LineItems.Any(i => string.IsNullOrWhiteSpace(i.Description)))
                    AddIssue(doc, "One or more items have no description.", "Invalid Items");

                decimal realItemsSum = doc.LineItems.Sum(item => item.LineTotal);
                if (Math.Abs(realItemsSum - doc.Subtotal) > 0.01m)
                    AddIssue(doc, $"The sum ({realItemsSum:F2}) does not match ({doc.Subtotal:F2}).", "Calculation Error");
            }

            if (Math.Abs((doc.Subtotal + doc.TaxAmount) - doc.TotalAmount) > 0.01m)
                AddIssue(doc, $"Mathematics: {doc.Subtotal:F2} + {doc.TaxAmount:F2} != {doc.TotalAmount:F2}", "Incorrect Total");

            if (!doc.IssueDate.HasValue)
                AddIssue(doc, "The issue date of the document is missing.", "Missing Date");

            if (string.IsNullOrWhiteSpace(doc.SupplierName) || (doc.SupplierName?.Contains("Unknown") ?? false))
                AddIssue(doc, "The supplier name is missing.", "Missing Supplier");

            if (string.IsNullOrWhiteSpace(doc.DocumentNumber) || doc.DocumentNumber == "Unknown")
                AddIssue(doc, "The document number is missing.", "Missing Field");

            if (doc.ValidationIssues.Count > 0)
            {
                doc.Status = DocumentStatus.NeedsReview;
            }
            else
            {
                doc.Status = isManualEntry ? DocumentStatus.Validated : DocumentStatus.Uploaded;
            }
        }

        private static string ExtractTextFromPdf(string filePath)
        {
            var text = new StringBuilder();
            using var pdfReader = new PdfReader(filePath);
            using var pdfDoc = new PdfDocument(pdfReader);
            for (int i = 1; i <= pdfDoc.GetNumberOfPages(); i++)
            {
                text.AppendLine(PdfTextExtractor.GetTextFromPage(pdfDoc.GetPage(i), new LocationTextExtractionStrategy()));
            }
            return text.ToString();
        }

        private static decimal ParseDecimal(string value)
        {
            if (string.IsNullOrEmpty(value)) return 0;
            value = value.Replace(",", "");
            decimal.TryParse(value, NumberStyles.Any, CultureInfo.InvariantCulture, out decimal result);
            return result;
        }

        private static void AddIssue(Document doc, string message, string type)
        {
            doc.ValidationIssues.Add(new ValidationIssue
            {
                ErrorMessage = message,
                IssueType = type,
                CreatedAt = DateTime.UtcNow,
                DocumentId = doc.Id
            });
        }
    }
}