using System;
using System.Collections.Generic;
using System.Web.Mvc;

namespace U23593963_HW03.Models
{
    public class ReportViewModel
    {
        public string ReportTitle { get; set; }
        public List<DataPoint> SalesByMonth { get; set; }
        public List<DataPoint> TopProducts { get; set; }
        public List<DataPoint> BrandPerformance { get; set; }
        public List<DataPoint> CustomerPerformance { get; set; }
        public List<DataPoint> StockStatus { get; set; }
        public List<DataPoint> StaffPerformance { get; set; }
        public List<DataPoint> StorePerformance { get; set; }

        // For report generation
        public string SelectedReportType { get; set; }
        public List<SelectListItem> ReportTypes { get; set; }

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
        public string Description { get; set; } // For bonus requirement
        public DateTime CreatedDate { get; set; }
        public string FileSize { get; set; }
        public byte[] FileContent { get; set; }
    }
}