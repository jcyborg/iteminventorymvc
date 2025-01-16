using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;

namespace ItemInventory2.ViewModel
{
    public class ItemVM
    {
        public string ItemNo { get; set; } = null!;

        public string? ItemDescription { get; set; }

        public int? Quantity { get; set; }

        public decimal? Price { get; set; }
    }
}
