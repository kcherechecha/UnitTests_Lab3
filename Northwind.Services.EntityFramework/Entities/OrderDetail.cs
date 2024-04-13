using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Northwind.Services.EntityFramework.Entities;

public class OrderDetail
{
    public long OrderID { get; set; }

    [ForeignKey(nameof(OrderID))]
    public virtual Order Order { get; set; } = default!;

    [Key]
    public long ProductID { get; set; }

    [ForeignKey(nameof(ProductID))]
    public virtual Product Product { get; set; } = default!;

    public double UnitPrice { get; set; } = default!;

    public long Quantity { get; set; } = default!;

    public double Discount { get; set; } = default!;
}
