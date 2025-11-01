using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Threading.Tasks;
using System.Web.Mvc;
using U23593963_HW03.Models;

namespace U23593963_HW03.Controllers
{
    public class HomeController : Controller
    {
        private BikeStoresEntities db = new BikeStoresEntities();

        // GET: Home
        public async Task<ActionResult> Index()
        {
            try
            {
                // Populate ViewBag for dropdowns used in modals and filters
                ViewBag.Brands = await db.brands.ToListAsync();
                ViewBag.Categories = await db.categories.ToListAsync();
                ViewBag.Stores = await db.stores.ToListAsync();

                var viewModel = new HomeViewModel
                {
                    // Load staff with stores information
                    Staffs = await db.staffs
                        .Include(s => s.stores)
                        .Include(s => s.staffs1) // Manager relationship
                        .ToListAsync(),

                    Customers = await db.customers.ToListAsync(),

                    // Load products with brands and categories
                    Products = await db.products
                        .Include(p => p.brands)
                        .Include(p => p.categories)
                        .ToListAsync()
                };

                return View(viewModel);
            }
            catch (Exception ex)
            {
                ViewBag.ErrorMessage = "Error loading data: " + ex.Message;
                return View(new HomeViewModel());
            }
        }

        // POST: Home with product filtering
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> Index(HomeViewModel filterModel)
        {
            try
            {
                // Populate ViewBag for dropdowns
                ViewBag.Brands = await db.brands.ToListAsync();
                ViewBag.Categories = await db.categories.ToListAsync();
                ViewBag.Stores = await db.stores.ToListAsync();

                var productsQuery = db.products
                    .Include(p => p.brands)
                    .Include(p => p.categories)
                    .AsQueryable();

                // Apply filters if provided
                if (filterModel.SelectedBrandId.HasValue)
                {
                    productsQuery = productsQuery.Where(p => p.brand_id == filterModel.SelectedBrandId.Value);
                }

                if (filterModel.SelectedCategoryId.HasValue)
                {
                    productsQuery = productsQuery.Where(p => p.category_id == filterModel.SelectedCategoryId.Value);
                }

                var viewModel = new HomeViewModel
                {
                    Staffs = await db.staffs
                        .Include(s => s.stores)
                        .Include(s => s.staffs1)
                        .ToListAsync(),

                    Customers = await db.customers.ToListAsync(),
                    Products = await productsQuery.ToListAsync(),
                    Brands = await db.brands.ToListAsync(),
                    Categories = await db.categories.ToListAsync(),
                    SelectedBrandId = filterModel.SelectedBrandId,
                    SelectedCategoryId = filterModel.SelectedCategoryId
                };

                return View(viewModel);
            }
            catch (Exception ex)
            {
                ViewBag.ErrorMessage = "Error applying filters: " + ex.Message;
                return View(new HomeViewModel());
            }
        }

        // Create Staff - POST (without AJAX)
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> CreateStaff(staffs staff)
        {
            try
            {
                if (ModelState.IsValid)
                {
                    // Set default values if needed
                    if (staff.active == null)
                        staff.active = 1; // Default to active

                    db.staffs.Add(staff);
                    await db.SaveChangesAsync();

                    TempData["SuccessMessage"] = "Staff member created successfully!";
                    return RedirectToAction("Index");
                }

                // If validation fails, return to index with error
                TempData["ErrorMessage"] = "Please check the form for errors.";
                return RedirectToAction("Index");
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = "Error creating staff: " + ex.Message;
                return RedirectToAction("Index");
            }
        }

        // Create Customer - POST (without AJAX)
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> CreateCustomer(customers customer)
        {
            try
            {
                if (ModelState.IsValid)
                {
                    db.customers.Add(customer);
                    await db.SaveChangesAsync();

                    TempData["SuccessMessage"] = "Customer created successfully!";
                    return RedirectToAction("Index");
                }

                // If validation fails, return to index with error
                TempData["ErrorMessage"] = "Please check the form for errors.";
                return RedirectToAction("Index");
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = "Error creating customer: " + ex.Message;
                return RedirectToAction("Index");
            }
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