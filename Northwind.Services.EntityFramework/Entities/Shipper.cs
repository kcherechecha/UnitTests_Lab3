namespace Northwind.Services.EntityFramework.Entities;

public class Shipper
{
    public Shipper()
    {
        this.Orders = new List<Order>();
    }

    public long ShipperID { get; set; }

    public string CompanyName { get; set; } = default!;

    public string Phone { get; set; } = default!;

    public virtual ICollection<Order> Orders { get; set; }
}
