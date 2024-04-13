using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Northwind.Services.EntityFramework.Entities;

public class Order
{
    public Order()
    {
        this.OrderDetails = new List<OrderDetail>();
    }

    [Key]
    public long OrderID { get; set; }

    public string CustomerID { get; set; } = default!;

    [ForeignKey(nameof(CustomerID))]
    public virtual Customer Customer { get; set; } = default!;

    public long EmployeeID { get; set; }

    [ForeignKey(nameof(EmployeeID))]
    public virtual Employee Employee { get; set; } = default!;

    public DateTime OrderDate { get; set; } = default!;

    public DateTime RequiredDate { get; set; } = default!;

    public DateTime? ShippedDate { get; set; } = default!;

    public long ShipVia { get; set; } = default!;

    [ForeignKey(nameof(ShipVia))]
    public Shipper Shipper { get; set; } = default!;

    public double Freight { get; set; } = default!;

    public string ShipName { get; set; } = default!;

    public string ShipAddress { get; set; } = default!;

    public string ShipCity { get; set; } = default!;

    public string? ShipRegion { get; set; } = default!;

    public string ShipPostalCode { get; set; } = default!;

    public string ShipCountry { get; set; } = default!;

    [NotMapped]
    public virtual ICollection<OrderDetail> OrderDetails { get; set; }
}
