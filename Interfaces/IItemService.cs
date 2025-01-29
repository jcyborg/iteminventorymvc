using ItemInventory2.DataLayer.ApplicationUsers.Models;

namespace ItemInventory2.Interfaces
{
    public interface IItemService
    {
        Task<List<Item>> ParseItemsFromFileAsync(IFormFile file);
        Task<bool> ItemsAlreadyExistAsync(List<Item> items);
        Task BulkInsertOrUpdateAsync(List<Item> items);
        IQueryable<Item> GetAllItems();
        IQueryable<Item> GetItemsByBatchId(string batchId);
    }

}
