using System.ComponentModel.DataAnnotations;

namespace Northwind.Services.EntityFramework.Entities;

public class Category
{
    public Category()
    {
        this.Products = new List<Product>();
    }

    [Key]
    public long CategoryID { get; set; }

    public string CategoryName { get; set; } = default!;

    public string? Description { get; set; } = default!;

    public virtual ICollection<Product> Products { get; set; }
}
