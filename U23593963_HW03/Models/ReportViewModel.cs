using System;
using System.Collections.Generic;
using System.Web.Mvc;

namespace U23593963_HW03.Models
{
    public class ReportViewModel
    {
        public string ReportTitle { get; set; }
        public string ChartType { get; set; }
        public List<DataPoint> SalesByMonth { get; set; }
        public List<DataPoint> TopProducts { get; set; }
        public List<DataPoint> BrandPerformance { get; set; }
        public List<DataPoint> CustomerPerformance { get; set; }
        public List<DataPoint> StockStatus { get; set; }
        public List<DataPoint> StaffPerformance { get; set; }
        public List<DataPoint> StorePerformance { get; set; }

        // Tabular data for reports
        public List<SalesRecord> SalesRecords { get; set; }
        public List<StockRecord> StockRecords { get; set; }
        public List<CustomerRecord> CustomerRecords { get; set; }

        // For report generation
        public string SelectedReportType { get; set; }
        public List<SelectListItem> ReportTypes { get; set; }
        public List<SelectListItem> FileTypes { get; set; }

        // For saved reports archive
        public List<SavedReport> SavedReports { get; set; }
    }

    public class DataPoint
    {
        public DataPoint(double value, string label)
        {
            Value = value;
            Label = label;
        }

        public double Value { get; set; }
        public string Label { get; set; }
    }

    public class SavedReport
    {
        public string FileName { get; set; }
        public string FileType { get; set; }
        public string Description { get; set; }
        public DateTime CreatedDate { get; set; }
        public string FileSize { get; set; }
        public byte[] FileContent { get; set; }
    }

    // Additional data models for tabular reports
    public class SalesRecord
    {
        public string CustomerName { get; set; }
        public string ProductName { get; set; }
        public string StaffName { get; set; }
        public DateTime OrderDate { get; set; }
        public decimal Amount { get; set; }
    }

    public class StockRecord
    {
        public string ProductName { get; set; }
        public string Brand { get; set; }
        public string Category { get; set; }
        public int Quantity { get; set; }
    }

    public class CustomerRecord
    {
        public string CustomerName { get; set; }
        public int TotalOrders { get; set; }
        public decimal TotalSpent { get; set; }
    }
}