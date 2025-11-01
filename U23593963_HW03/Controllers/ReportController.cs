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

                var savedReport = new SavedReport
                {
                    FileName = fileName + "." + (fileType ?? "pdf"),
                    FileType = fileType ?? "pdf",
                    Description = description,
                    CreatedDate = DateTime.Now,
                    FileSize = "N/A",
                    FileContent = System.Text.Encoding.UTF8.GetBytes(chartData ?? "")
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

            // Return as downloadable file
            return File(report.FileContent, "application/octet-stream", report.FileName);
        }

        private async Task LoadReportData(ReportViewModel model, string reportType)
        {
            switch (reportType)
            {
                case "CurrentSales":
                    model.ReportTitle = "Current Sales Report";
                    model.SalesByMonth = await GetCurrentSalesReport();
                    break;
                case "StockItems":
                    model.ReportTitle = "Stock Items Report";
                    model.StockStatus = await GetStockItemsReport();
                    break;
                case "PopularProducts":
                    model.ReportTitle = "Popular Products Report";
                    model.TopProducts = await GetPopularProductsReport();
                    break;
                case "CustomerRanking":
                    model.ReportTitle = "Customer Performance Ranking";
                    model.CustomerPerformance = await GetCustomerRankingReport();
                    break;
                case "StaffRanking":
                    model.ReportTitle = "Staff Performance Ranking";
                    model.StaffPerformance = await GetStaffRankingReport();
                    break;
                case "StoreRanking":
                    model.ReportTitle = "Store Performance Ranking";
                    model.StorePerformance = await GetStoreRankingReport();
                    break;
                default:
                    model.ReportTitle = "Sales Report";
                    model.SalesByMonth = await GetCurrentSalesReport();
                    break;
            }
        }

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