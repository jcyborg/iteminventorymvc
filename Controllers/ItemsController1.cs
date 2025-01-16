using EFCore.BulkExtensions;
using ItemInventory2.DataLayer.ApplicationUsers;
using ItemInventory2.DataLayer.ApplicationUsers.Models;
using ItemInventory2.ViewModel;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ItemInventory2.Controllers
{
    public class ItemsController : Controller
    {
        private readonly AppUsersContext _context;

        public ItemsController(AppUsersContext context)
        {
            _context = context;
        }

        public IActionResult Upload()
        {
            return View();
        }
        public IActionResult Index(int page = 1, int pagesToShow = 20)
        {
            int pageSize = 20; 
            int totalItems = _context.Items.Count(); 
            int totalPages = (int)Math.Ceiling((double)totalItems / pageSize);

            var items = _context.Items
                .OrderBy(item => item.ItemNo)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(item => new ItemVM
                {
                    ItemNo = item.ItemNo,
                    ItemDescription = item.ItemDescription,
                    Quantity = item.Quantity,
                    Price = item.Price
                })
                .ToList();

            
            int startPage = ((page - 1) / pagesToShow) * pagesToShow + 1;
            int endPage = Math.Min(startPage + pagesToShow - 1, totalPages);

            ViewData["TotalItems"] = totalItems;
            ViewData["CurrentPage"] = page;
            ViewData["TotalPages"] = totalPages;
            ViewData["StartPage"] = startPage;
            ViewData["EndPage"] = endPage;

            return View(items);
        }


        [HttpPost]
        public async Task<IActionResult> Import(IFormFile file)
        {
            if (file == null || file.Length == 0)
            {
                TempData["ErrorMessage"] = "No file uploaded.";
                return RedirectToAction("Upload", "Items");
            }

            var items = new List<Item>();

            using (var reader = new StreamReader(file.OpenReadStream()))
            {
                bool isFirstLine = true;

                while (!reader.EndOfStream)
                {
                    var line = await reader.ReadLineAsync();

                    if (isFirstLine)
                    {
                        isFirstLine = false;
                        continue;
                    }

                    var parts = line.Split('|');
                    if (parts.Length == 4)
                    {
                        try
                        {
                            items.Add(new Item
                            {
                                ItemNo = parts[0],
                                ItemDescription = parts[1],
                                Quantity = int.Parse(parts[2]),
                                Price = decimal.Parse(parts[3])
                            });
                        }
                        catch (FormatException)
                        {
                            TempData["ErrorMessage"] = "Invalid data format in the uploaded file.";
                            return RedirectToAction("Upload", "Items");
                        }
                    }
                    else
                    {
                        TempData["ErrorMessage"] = "Invalid file structure.";
                        return RedirectToAction("Upload", "Items");
                    }
                }
            }

            // Ensure distinct items are processed
            var distinctItems = items
                .GroupBy(i => i.ItemNo)
                .Select(g => g.First())
                .ToList();

            try
            {
                // Check if all items already exist in the database
                var existingItems = await _context.Items
                    .Where(dbItem => distinctItems.Select(fileItem => fileItem.ItemNo).Contains(dbItem.ItemNo))
                    .ToListAsync();

                if (existingItems.Count == distinctItems.Count)
                {
                    TempData["ErrorMessage"] = "This file was recently uploaded. All records already exist in the database.";
                    return RedirectToAction(nameof(Index));
                }

                // Perform bulk insert or update
                await _context.BulkInsertOrUpdateAsync(distinctItems);

                TempData["SuccessMessage"] = "File uploaded successfully!";
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = $"An error occurred: {ex.Message}";
                return RedirectToAction("Upload", "Items");
            }
        }
    }

}
