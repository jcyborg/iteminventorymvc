using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace ItemInventory2.DataLayer.ApplicationUsers.Models;

[Index("ItemNo", Name = "UQ_ItemNo", IsUnique = true)]
public partial class Item
{
    [Key]
    [StringLength(50)]
    public string ItemNo { get; set; } = null!;

    [StringLength(255)]
    public string? ItemDescription { get; set; }

    public int? Quantity { get; set; }

    [Column(TypeName = "decimal(18, 2)")]
    public decimal? Price { get; set; }

    [StringLength(50)]
    [Unicode(false)]
    public string? BatchId { get; set; }
}
