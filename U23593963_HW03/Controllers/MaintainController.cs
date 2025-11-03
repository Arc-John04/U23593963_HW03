using System;
using System.Data.Entity;
using System.Linq;
using System.Threading.Tasks;
using System.Web.Mvc;
using U23593963_HW03.Models;

namespace U23593963_HW03.Controllers
{
    public class MaintainController : Controller
    {
        private BikeStoresEntities db = new BikeStoresEntities();

        // GET: Maintain
        public async Task<ActionResult> Index()
        {
            try
            {
                // Populate ViewBag for dropdowns used in edit modals
                ViewBag.Stores = await db.stores.ToListAsync();
                ViewBag.Brands = await db.brands.ToListAsync();
                ViewBag.Categories = await db.categories.ToListAsync();

                var viewModel = new HomeViewModel
                {
                    // Load staff with stores and manager information
                    Staffs = await db.staffs
                        .Include(s => s.stores)
                        .Include(s => s.staffs1) // Manager
                        .ToListAsync(),

                    // Load customers
                    Customers = await db.customers
                        .ToListAsync(),

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

        // POST: Update Staff
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> UpdateStaff(staffs staff)
        {
            try
            {
                if (ModelState.IsValid)
                {
                    var existingStaff = await db.staffs.FindAsync(staff.staff_id);
                    if (existingStaff != null)
                    {
                        // Update properties
                        existingStaff.first_name = staff.first_name;
                        existingStaff.last_name = staff.last_name;
                        existingStaff.email = staff.email;
                        existingStaff.phone = staff.phone;
                        existingStaff.active = staff.active;
                        existingStaff.store_id = staff.store_id;
                        existingStaff.manager_id = staff.manager_id;

                        await db.SaveChangesAsync();
                        TempData["SuccessMessage"] = "Staff updated successfully!";
                    }
                    else
                    {
                        TempData["ErrorMessage"] = "Staff member not found.";
                    }
                }
                else
                {
                    TempData["ErrorMessage"] = "Please check the form for errors.";
                }
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = "Error updating staff: " + ex.Message;
            }

            return RedirectToAction("Index");
        }

        // POST: Update Customer
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> UpdateCustomer(customers customer)
        {
            try
            {
                if (ModelState.IsValid)
                {
                    var existingCustomer = await db.customers.FindAsync(customer.customer_id);
                    if (existingCustomer != null)
                    {
                        // Update properties
                        existingCustomer.first_name = customer.first_name;
                        existingCustomer.last_name = customer.last_name;
                        existingCustomer.email = customer.email;
                        existingCustomer.phone = customer.phone;
                        existingCustomer.street = customer.street;
                        existingCustomer.city = customer.city;
                        existingCustomer.state = customer.state;
                        existingCustomer.zip_code = customer.zip_code;

                        await db.SaveChangesAsync();
                        TempData["SuccessMessage"] = "Customer updated successfully!";
                    }
                    else
                    {
                        TempData["ErrorMessage"] = "Customer not found.";
                    }
                }
                else
                {
                    TempData["ErrorMessage"] = "Please check the form for errors.";
                }
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = "Error updating customer: " + ex.Message;
            }

            return RedirectToAction("Index");
        }

        // POST: Update Product
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> UpdateProduct(products product)
        {
            try
            {
                if (ModelState.IsValid)
                {
                    var existingProduct = await db.products.FindAsync(product.product_id);
                    if (existingProduct != null)
                    {
                        // Update properties
                        existingProduct.product_name = product.product_name;
                        existingProduct.brand_id = product.brand_id;
                        existingProduct.category_id = product.category_id;
                        existingProduct.model_year = product.model_year;
                        existingProduct.list_price = product.list_price;

                        await db.SaveChangesAsync();
                        TempData["SuccessMessage"] = "Product updated successfully!";
                    }
                    else
                    {
                        TempData["ErrorMessage"] = "Product not found.";
                    }
                }
                else
                {
                    TempData["ErrorMessage"] = "Please check the form for errors.";
                }
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = "Error updating product: " + ex.Message;
            }

            return RedirectToAction("Index");
        }

        // POST: Delete Staff
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> DeleteStaff(int staff_id)
        {
            try
            {
                var staff = await db.staffs
                    .Include(s => s.orders) // Check for orders created by this staff
                    .Include(s => s.staffs1) // Check if this staff manages others
                    .FirstOrDefaultAsync(s => s.staff_id == staff_id);

                if (staff != null)
                {
                    // Check constraints
                    if (staff.orders.Any())
                    {
                        TempData["ErrorMessage"] = "Cannot delete staff member who has created orders. The staff member has " + staff.orders.Count + " order(s).";
                        return RedirectToAction("Index");
                    }

                    if (staff.staffs1.Any())
                    {
                        TempData["ErrorMessage"] = "Cannot delete staff member who manages other staff members. Please reassign their reports first.";
                        return RedirectToAction("Index");
                    }

                    db.staffs.Remove(staff);
                    await db.SaveChangesAsync();
                    TempData["SuccessMessage"] = "Staff deleted successfully!";
                }
                else
                {
                    TempData["ErrorMessage"] = "Staff member not found.";
                }
            }
            catch (Exception ex)
            {
                // Check for specific constraint violations
                if (ex.InnerException != null && ex.InnerException.Message.Contains("REFERENCE constraint"))
                {
                    TempData["ErrorMessage"] = "Cannot delete staff member due to database constraints. This staff member may have related records in orders or manages other staff.";
                }
                else
                {
                    TempData["ErrorMessage"] = "Error deleting staff: " + ex.Message;
                }
            }

            return RedirectToAction("Index");
        }

        // POST: Delete Customer
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> DeleteCustomer(int customer_id)
        {
            try
            {
                var customer = await db.customers
                    .Include(c => c.orders) // Check for customer orders
                    .FirstOrDefaultAsync(c => c.customer_id == customer_id);

                if (customer != null)
                {
                    // Check constraints
                    if (customer.orders.Any())
                    {
                        TempData["ErrorMessage"] = "Cannot delete customer who has order history. The customer has " + customer.orders.Count + " order(s).";
                        return RedirectToAction("Index");
                    }

                    db.customers.Remove(customer);
                    await db.SaveChangesAsync();
                    TempData["SuccessMessage"] = "Customer deleted successfully!";
                }
                else
                {
                    TempData["ErrorMessage"] = "Customer not found.";
                }
            }
            catch (Exception ex)
            {
                // Check for specific constraint violations
                if (ex.InnerException != null && ex.InnerException.Message.Contains("REFERENCE constraint"))
                {
                    TempData["ErrorMessage"] = "Cannot delete customer due to database constraints. This customer may have order history.";
                }
                else
                {
                    TempData["ErrorMessage"] = "Error deleting customer: " + ex.Message;
                }
            }

            return RedirectToAction("Index");
        }

        // POST: Delete Product
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> DeleteProduct(int product_id)
        {
            try
            {
                var product = await db.products
                    .Include(p => p.order_items) // Check for order items
                    .Include(p => p.stocks) // Check for stock records
                    .FirstOrDefaultAsync(p => p.product_id == product_id);

                if (product != null)
                {
                    // Check constraints
                    if (product.order_items.Any())
                    {
                        TempData["ErrorMessage"] = "Cannot delete product that has been ordered. The product appears in " + product.order_items.Count + " order item(s).";
                        return RedirectToAction("Index");
                    }

                    if (product.stocks.Any())
                    {
                        // Remove stock records first
                        db.stocks.RemoveRange(product.stocks);
                    }

                    db.products.Remove(product);
                    await db.SaveChangesAsync();
                    TempData["SuccessMessage"] = "Product deleted successfully!";
                }
                else
                {
                    TempData["ErrorMessage"] = "Product not found.";
                }
            }
            catch (Exception ex)
            {
                // Check for specific constraint violations
                if (ex.InnerException != null && ex.InnerException.Message.Contains("REFERENCE constraint"))
                {
                    TempData["ErrorMessage"] = "Cannot delete product due to database constraints. This product may have order history or stock records.";
                }
                else
                {
                    TempData["ErrorMessage"] = "Error deleting product: " + ex.Message;
                }
            }

            return RedirectToAction("Index");
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