using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Web.Mvc;

namespace U23593963_HW03.Models
{
    public class HomeViewModel
    {
        public List<staffs> Staffs { get; set; }
        public List<customers> Customers { get; set; }
        public List<products> Products { get; set; }
        public List<brands> Brands { get; set; }
        public List<categories> Categories { get; set; }

        // For filtering
        public int? SelectedBrandId { get; set; }
        public int? SelectedCategoryId { get; set; }
    }
}