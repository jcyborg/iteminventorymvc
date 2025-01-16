using EFCore.BulkExtensions;
using ItemInventory2.DataLayer.ApplicationUsers;
using ItemInventory2.DataLayer.ApplicationUsers.Models;
using ItemInventory2.ViewModel;
using Microsoft.AspNetCore.Mvc;

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
            int pageSize = 20; // Items per page
            int totalItems = _context.Items.Count(); // Total number of records
            int totalPages = (int)Math.Ceiling((double)totalItems / pageSize); // Total pages

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

            // Calculate visible page range
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
                return BadRequest("No file uploaded.");

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
                            return BadRequest($"Invalid data format on line: {line}");
                        }
                    }
                    else
                    {
                        return BadRequest($"Invalid line structure: {line}");
                    }
                }
            }

            var distinctItems = items
                .GroupBy(i => i.ItemNo) 
                .Select(g => g.First()) 
                .ToList();

            try
            {
                await _context.BulkInsertOrUpdateAsync(distinctItems);

                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                return BadRequest($"An error occurred: {ex.Message}");
            }
        }
    }

}
