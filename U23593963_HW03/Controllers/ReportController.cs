using U23593963_HW03.Models;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Web;
using System.Web.Mvc;

namespace U23593963_HW03.Controllers
{
    public class ReportController : Controller
    {
        private BikeStoresEntities db = new BikeStoresEntities();

        // GET: Report
        public async Task<ActionResult> Index(string reportType = "CurrentSales")
        {
            var model = new ReportViewModel
            {
                ReportTypes = new List<SelectListItem>
                {
                    new SelectListItem { Value = "CurrentSales", Text = "Current Sales Report" },
                    new SelectListItem { Value = "StockItems", Text = "Stock Items Report" },
                    new SelectListItem { Value = "PopularProducts", Text = "Popular Products Report" },
                    new SelectListItem { Value = "CustomerRanking", Text = "Customer Performance Ranking" },
                    new SelectListItem { Value = "StaffRanking", Text = "Staff Performance Ranking" },
                    new SelectListItem { Value = "StoreRanking", Text = "Store Performance Ranking" }
                },
                FileTypes = new List<SelectListItem>
                {
                    new SelectListItem { Value = "pdf", Text = "PDF Document" },
                    new SelectListItem { Value = "txt", Text = "Text File" },
                    new SelectListItem { Value = "csv", Text = "CSV File" }
                },
                SelectedReportType = reportType,
                SavedReports = Session["SavedReports"] as List<SavedReport> ?? new List<SavedReport>()
            };

            await LoadReportData(model, reportType);

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> GenerateReport(string reportType)
        {
            var result = await Index(reportType);
            return View("Index", ((ViewResult)result).Model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult SaveReport(string fileName, string fileType, string description, string chartData)
        {
            try
            {
                if (string.IsNullOrEmpty(fileName))
                {
                    TempData["Message"] = "Please enter a filename";
                    TempData["MessageType"] = "error";
                    return RedirectToAction("Index");
                }

                // Generate file content based on type
                byte[] fileContent = GenerateFileContent(fileType, chartData, description);
                string fileExtension = GetFileExtension(fileType);
                string fullFileName = fileName + fileExtension;

                var savedReport = new SavedReport
                {
                    FileName = fullFileName,
                    FileType = fileType,
                    Description = description,
                    CreatedDate = DateTime.Now,
                    FileSize = FormatFileSize(fileContent.Length),
                    FileContent = fileContent
                };

                var savedReports = Session["SavedReports"] as List<SavedReport> ?? new List<SavedReport>();
                savedReports.Add(savedReport);
                Session["SavedReports"] = savedReports;

                TempData["Message"] = $"Report '{savedReport.FileName}' saved successfully!";
                TempData["MessageType"] = "success";
            }
            catch (Exception ex)
            {
                TempData["Message"] = "Error saving report: " + ex.Message;
                TempData["MessageType"] = "error";
            }

            return RedirectToAction("Index");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult DeleteReport(string fileName)
        {
            try
            {
                var savedReports = Session["SavedReports"] as List<SavedReport> ?? new List<SavedReport>();
                var reportToRemove = savedReports.FirstOrDefault(r => r.FileName == fileName);
                if (reportToRemove != null)
                {
                    savedReports.Remove(reportToRemove);
                    Session["SavedReports"] = savedReports;
                    TempData["Message"] = $"Report '{fileName}' deleted successfully!";
                    TempData["MessageType"] = "success";
                }
                else
                {
                    TempData["Message"] = $"Report '{fileName}' not found.";
                    TempData["MessageType"] = "error";
                }
            }
            catch (Exception ex)
            {
                TempData["Message"] = "Error deleting report: " + ex.Message;
                TempData["MessageType"] = "error";
            }

            return RedirectToAction("Index");
        }

        public ActionResult DownloadReport(string fileName)
        {
            var savedReports = Session["SavedReports"] as List<SavedReport> ?? new List<SavedReport>();
            var report = savedReports.FirstOrDefault(r => r.FileName == fileName);

            if (report == null || report.FileContent == null)
            {
                TempData["Message"] = "Report not found";
                TempData["MessageType"] = "error";
                return RedirectToAction("Index");
            }

            string contentType = GetContentType(report.FileType);
            return File(report.FileContent, contentType, report.FileName);
        }

        private async Task LoadReportData(ReportViewModel model, string reportType)
        {
            switch (reportType)
            {
                case "CurrentSales":
                    model.ReportTitle = "Current Sales Report";
                    model.SalesByMonth = await GetCurrentSalesReport();
                    model.SalesRecords = await GetSalesRecords();
                    model.ChartType = "line"; // Trend over time
                    break;
                case "StockItems":
                    model.ReportTitle = "Stock Items Report";
                    model.StockStatus = await GetStockItemsReport();
                    model.StockRecords = await GetStockRecords();
                    model.ChartType = "horizontalBar"; // Quantity comparison
                    break;
                case "PopularProducts":
                    model.ReportTitle = "Popular Products Report";
                    model.TopProducts = await GetPopularProductsReport();
                    model.ChartType = "bar"; // Ranking comparison
                    break;
                case "CustomerRanking":
                    model.ReportTitle = "Customer Performance Ranking";
                    model.CustomerPerformance = await GetCustomerRankingReport();
                    model.CustomerRecords = await GetCustomerRecords();
                    model.ChartType = "bar"; // Performance ranking
                    break;
                case "StaffRanking":
                    model.ReportTitle = "Staff Performance Ranking";
                    model.StaffPerformance = await GetStaffRankingReport();
                    model.ChartType = "bar"; // Performance comparison
                    break;
                case "StoreRanking":
                    model.ReportTitle = "Store Performance Ranking";
                    model.StorePerformance = await GetStoreRankingReport();
                    model.ChartType = "pie"; // Market share distribution
                    break;
                default:
                    model.ReportTitle = "Sales Report";
                    model.SalesByMonth = await GetCurrentSalesReport();
                    model.SalesRecords = await GetSalesRecords();
                    model.ChartType = "line";
                    break;
            }
        }

        private byte[] GenerateFileContent(string fileType, string chartDataJson, string description)
        {
            var chartData = JsonConvert.DeserializeObject<dynamic>(chartDataJson ?? "{}");
            string content = "";

            switch (fileType.ToLower())
            {
                case "txt":
                    content = GenerateTextContent(chartData, description);
                    break;
                case "csv":
                    content = GenerateCsvContent(chartData, description);
                    break;
                case "pdf":
                default:
                    // For PDF, we'll use the existing JavaScript method
                    content = "PDF content would be generated here";
                    break;
            }

            return System.Text.Encoding.UTF8.GetBytes(content);
        }

        private string GenerateTextContent(dynamic chartData, string description)
        {
            string content = $"REPORT: {chartData?.title ?? "Untitled Report"}\n";
            content += $"Generated: {DateTime.Now:yyyy-MM-dd HH:mm}\n";
            content += $"Description: {description ?? "No description provided"}\n\n";
            content += "DATA:\n";

            if (chartData?.labels != null && chartData?.values != null)
            {
                for (int i = 0; i < chartData.labels.Count; i++)
                {
                    content += $"{chartData.labels[i]}: {chartData.values[i]}\n";
                }
            }

            return content;
        }

        private string GenerateCsvContent(dynamic chartData, string description)
        {
            string content = $"Report,{chartData?.title ?? "Untitled Report"}\n";
            content += $"Generated,{DateTime.Now:yyyy-MM-dd HH:mm}\n";
            content += $"Description,{description ?? "No description provided"}\n";
            content += "Label,Value\n";

            if (chartData?.labels != null && chartData?.values != null)
            {
                for (int i = 0; i < chartData.labels.Count; i++)
                {
                    content += $"{chartData.labels[i]},{chartData.values[i]}\n";
                }
            }

            return content;
        }

        private string GetFileExtension(string fileType)
        {
            switch (fileType.ToLower())
            {
                case "pdf": return ".pdf";
                case "txt": return ".txt";
                case "csv": return ".csv";
                default: return ".pdf";
            }
        }

        private string GetContentType(string fileType)
        {
            switch (fileType.ToLower())
            {
                case "pdf": return "application/pdf";
                case "txt": return "text/plain";
                case "csv": return "text/csv";
                default: return "application/octet-stream";
            }
        }

        private string FormatFileSize(long bytes)
        {
            string[] sizes = { "B", "KB", "MB", "GB" };
            int order = 0;
            while (bytes >= 1024 && order < sizes.Length - 1)
            {
                order++;
                bytes = bytes / 1024;
            }
            return $"{bytes:0.##} {sizes[order]}";
        }

        // Data loading methods
        private async Task<List<DataPoint>> GetCurrentSalesReport()
        {
            var salesData = await db.orders
                .Where(o => o.order_date.Year == DateTime.Now.Year)
                .GroupBy(o => new { Month = o.order_date.Month })
                .Select(g => new
                {
                    Month = g.Key.Month,
                    TotalSales = g.Sum(o => o.order_items.Sum(oi => oi.quantity * oi.list_price * (1 - oi.discount)))
                })
                .OrderBy(x => x.Month)
                .ToListAsync();

            return salesData.Select(s => new DataPoint(
                (double)s.TotalSales,
                new DateTime(DateTime.Now.Year, s.Month, 1).ToString("MMM yyyy")
            )).ToList();
        }

        private async Task<List<SalesRecord>> GetSalesRecords()
        {
            var sales = await db.orders
                .Include(o => o.customers)
                .Include(o => o.staffs)
                .Include(o => o.order_items.Select(oi => oi.products))
                .Where(o => o.order_date.Year == DateTime.Now.Year)
                .OrderByDescending(o => o.order_date)
                .Take(20)
                .Select(o => new SalesRecord
                {
                    CustomerName = o.customers.first_name + " " + o.customers.last_name,
                    ProductName = o.order_items.FirstOrDefault().products.product_name,
                    StaffName = o.staffs.first_name + " " + o.staffs.last_name,
                    OrderDate = o.order_date,
                    Amount = o.order_items.Sum(oi => oi.quantity * oi.list_price * (1 - oi.discount))
                })
                .ToListAsync();

            return sales;
        }

        private async Task<List<DataPoint>> GetStockItemsReport()
        {
            var stockData = await db.stocks
                .Include(s => s.products)
                .Where(s => s.quantity > 0)
                .GroupBy(s => s.products.product_name)
                .Select(g => new
                {
                    ProductName = g.Key,
                    TotalStock = g.Sum(s => s.quantity)
                })
                .OrderByDescending(s => s.TotalStock)
                .Take(10)
                .ToListAsync();

            return stockData.Select(s => new DataPoint(
                (double)(s.TotalStock ?? 0),
                s.ProductName
            )).ToList();
        }

        private async Task<List<StockRecord>> GetStockRecords()
        {
            var stock = await db.stocks
                .Include(s => s.products)
                .Include(s => s.products.brands)
                .Include(s => s.products.categories)
                .Where(s => s.quantity > 0)
                .OrderByDescending(s => s.quantity)
                .Take(20)
                .Select(s => new StockRecord
                {
                    ProductName = s.products.product_name,
                    Brand = s.products.brands.brand_name,
                    Category = s.products.categories.category_name,
                    Quantity = s.quantity ?? 0
                })
                .ToListAsync();

            return stock;
        }

        private async Task<List<DataPoint>> GetPopularProductsReport()
        {
            var popularProducts = await db.order_items
                .Include(oi => oi.products)
                .GroupBy(oi => oi.products.product_name)
                .Select(g => new
                {
                    ProductName = g.Key,
                    TotalSold = g.Sum(oi => oi.quantity)
                })
                .OrderByDescending(p => p.TotalSold)
                .Take(10)
                .ToListAsync();

            return popularProducts.Select(p => new DataPoint(
                (double)p.TotalSold,
                p.ProductName
            )).ToList();
        }

        private async Task<List<DataPoint>> GetCustomerRankingReport()
        {
            var customerRanking = await db.orders
                .Include(o => o.customers)
                .Where(o => o.customers != null)
                .GroupBy(o => new { o.customers.first_name, o.customers.last_name })
                .Select(g => new
                {
                    CustomerName = g.Key.first_name + " " + g.Key.last_name,
                    TotalOrders = g.Count()
                })
                .OrderByDescending(c => c.TotalOrders)
                .Take(10)
                .ToListAsync();

            return customerRanking.Select(c => new DataPoint(c.TotalOrders, c.CustomerName)).ToList();
        }

        private async Task<List<CustomerRecord>> GetCustomerRecords()
        {
            var customers = await db.orders
                .Include(o => o.customers)
                .Include(o => o.order_items)
                .Where(o => o.customers != null)
                .GroupBy(o => new { o.customers.customer_id, o.customers.first_name, o.customers.last_name })
                .Select(g => new CustomerRecord
                {
                    CustomerName = g.Key.first_name + " " + g.Key.last_name,
                    TotalOrders = g.Count(),
                    TotalSpent = g.Sum(o => o.order_items.Sum(oi => oi.quantity * oi.list_price * (1 - oi.discount)))
                })
                .OrderByDescending(c => c.TotalSpent)
                .Take(10)
                .ToListAsync();

            return customers;
        }

        private async Task<List<DataPoint>> GetStaffRankingReport()
        {
            var staffRanking = await db.orders
                .Include(o => o.staffs)
                .GroupBy(o => new { o.staffs.first_name, o.staffs.last_name })
                .Select(g => new
                {
                    StaffName = g.Key.first_name + " " + g.Key.last_name,
                    TotalSales = g.Count()
                })
                .OrderByDescending(s => s.TotalSales)
                .Take(10)
                .ToListAsync();

            return staffRanking.Select(s => new DataPoint(s.TotalSales, s.StaffName)).ToList();
        }

        private async Task<List<DataPoint>> GetStoreRankingReport()
        {
            var storeRanking = await db.orders
                .Include(o => o.stores)
                .GroupBy(o => o.stores.store_name)
                .Select(g => new
                {
                    StoreName = g.Key,
                    TotalOrders = g.Count()
                })
                .OrderByDescending(s => s.TotalOrders)
                .Take(5)
                .ToListAsync();

            return storeRanking.Select(s => new DataPoint(s.TotalOrders, s.StoreName)).ToList();
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                db.Dispose();
            }
            base.Dispose(disposing);
        }
    }
}