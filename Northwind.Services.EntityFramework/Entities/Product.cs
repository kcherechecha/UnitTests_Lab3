using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Northwind.Services.EntityFramework.Entities;

public class Product
{
    public Product()
    {
        this.OrderDetails = new List<OrderDetail>();
    }

    [Key]
    public long ProductID { get; set; }

    public string ProductName { get; set; } = default!;

    public long SupplierID { get; set; }

    [ForeignKey(nameof(SupplierID))]
    public virtual Supplier Supplier { get; set; } = default!;

    public long CategoryID { get; set; }

    [ForeignKey(nameof(CategoryID))]
    public virtual Category Category { get; set; } = default!;

    public double? UnitPrice { get; set; } = default!;

    public int? UnitsOnOrder { get; set; } = default!;

    public int? ReorderLevel { get; set; } = default!;

    public int? Discontinued { get; set; } = default!;

    [NotMapped]
    public virtual ICollection<OrderDetail> OrderDetails { get; set; }
}
