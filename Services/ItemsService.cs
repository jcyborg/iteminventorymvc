using EFCore.BulkExtensions;
using ItemInventory2.DataLayer.ApplicationUsers;
using ItemInventory2.DataLayer.ApplicationUsers.Models;
using ItemInventory2.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace ItemInventory2.Services
{
    public class ItemService : IItemService
    {
        private readonly AppUsersContext _context;

        public ItemService(AppUsersContext context)
        {
            _context = context;
        }

        public async Task<List<Item>> ParseItemsFromFileAsync(IFormFile file)
        {
            if (file == null || file.Length == 0)
            {
                throw new InvalidOperationException("No file uploaded.");
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
                        continue; // skip header
                    }

                    var parts = line.Split('|');
                    if (parts.Length != 4)
                    {
                        throw new FormatException("Invalid file structure.");
                    }

                    items.Add(new Item
                    {
                        ItemNo = parts[0],
                        ItemDescription = parts[1],
                        Quantity = int.Parse(parts[2]),
                        Price = decimal.Parse(parts[3])
                    });
                }
            }

            return items;
        }

        public async Task<bool> ItemsAlreadyExistAsync(List<Item> items)
        {
            var distinctItemNos = items.Select(i => i.ItemNo).Distinct().ToList();

            var existingItemsCount = await _context.Items
                .Where(dbItem => distinctItemNos.Contains(dbItem.ItemNo))
                .CountAsync();

            // If the count of existing items is the same as the distinct item numbers,
            // it implies all those items exist already.
            return existingItemsCount == distinctItemNos.Count;
        }

        public async Task BulkInsertOrUpdateAsync(List<Item> items)
        {
            await _context.BulkInsertOrUpdateAsync(items);
        }

        public IQueryable<Item> GetAllItems()
        {
            return _context.Items;
        }

        public IQueryable<Item> GetItemsByBatchId(string batchId)
        {
            return _context.Items.Where(item => item.BatchId == batchId);
        }
    }

}
