using EFCore.BulkExtensions;
using ItemInventory2.DataLayer.ApplicationUsers;
using ItemInventory2.DataLayer.ApplicationUsers.Models;
using ItemInventory2.Interfaces;
using ItemInventory2.ViewModel;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ItemInventory2.Controllers
{
    public class ItemsController : Controller
    {
        private readonly IItemService _itemService;

        public ItemsController(IItemService itemService)
        {
            _itemService = itemService;
        }

        public IActionResult Upload()
        {
            HttpContext.Session.Clear();
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> Import(IFormFile file)
        {
            HttpContext.Session.Remove("TotalRecordsMessage");
            try
            {
                var items = await _itemService.ParseItemsFromFileAsync(file);

                var uploadBatchId = Guid.NewGuid().ToString();
                items.ForEach(item => item.BatchId = uploadBatchId);

                var distinctItems = items
                    .GroupBy(i => i.ItemNo)
                    .Select(g => g.First())
                    .ToList();

                HttpContext.Session.SetString("TotalRecordsMessage",
                    $"Total records of pre-existing items: {distinctItems.Count}");

                var allExist = await _itemService.ItemsAlreadyExistAsync(distinctItems);
                if (allExist)
                {
                    TempData["ErrorMessage"] = "This file was recently uploaded. All records already exist in the database.";
                    return RedirectToAction(nameof(Index));
                }

                await _itemService.BulkInsertOrUpdateAsync(distinctItems);

                HttpContext.Session.SetString("RecentBatchId", uploadBatchId);
                HttpContext.Session.SetString("TotalRecordsMessage",
                    $"Total records of newly imported items: {distinctItems.Count}");

                TempData["SuccessMessage"] = "File uploaded successfully!";
                return RedirectToAction(nameof(Index));
            }
            catch (InvalidOperationException ex)
            {
                TempData["ErrorMessage"] = ex.Message;
                return RedirectToAction(nameof(Upload));
            }
            catch (FormatException ex)
            {
                TempData["ErrorMessage"] = $"Invalid data format in the uploaded file. {ex.Message}";
                return RedirectToAction(nameof(Upload));
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = $"An error occurred: {ex.Message}";
                return RedirectToAction(nameof(Upload));
            }
        }


        public IActionResult Index(int page = 1, int pagesToShow = 20)
        {
            int pageSize = 20;
            var recentBatchId = HttpContext.Session.GetString("RecentBatchId");

            IQueryable<Item> query = !string.IsNullOrEmpty(recentBatchId)
                ? _itemService.GetItemsByBatchId(recentBatchId)
                : _itemService.GetAllItems();

            int totalItems = query.Count();
            int totalPages = (int)Math.Ceiling((double)totalItems / pageSize);

            var items = query
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

            ViewBag.TotalRecordsMessage = HttpContext.Session.GetString("TotalRecordsMessage");

            return View(items);
        }

        public IActionResult AllItems(int page = 1, int pagesToShow = 20)
        {
            HttpContext.Session.Remove("TotalRecordsMessage");
            int pageSize = 20;

            IQueryable<Item> query = _itemService.GetAllItems();

            int totalItems = query.Count();
            int totalPages = (int)Math.Ceiling((double)totalItems / pageSize);

            HttpContext.Session.SetString("TotalRecordsMessage", $"Total records of all items: {totalItems}");

            return RedirectToAction(nameof(Index), new { page, pagesToShow });
        }
    }


}
