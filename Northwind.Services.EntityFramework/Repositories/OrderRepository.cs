using Microsoft.EntityFrameworkCore;
using Northwind.Services.EntityFramework.Entities;
using Northwind.Services.Repositories;
using RepositoryCustomer = Northwind.Services.Repositories.Customer;
using RepositoryCustomerCode = Northwind.Services.Repositories.CustomerCode;
using RepositoryEmployee = Northwind.Services.Repositories.Employee;
using RepositoryOrder = Northwind.Services.Repositories.Order;
using RepositoryOrderDetail = Northwind.Services.Repositories.OrderDetail;
using RepositoryProduct = Northwind.Services.Repositories.Product;
using RepositoryShipper = Northwind.Services.Repositories.Shipper;
using RepositoryShippingAddress = Northwind.Services.Repositories.ShippingAddress;

namespace Northwind.Services.EntityFramework.Repositories;

public sealed class OrderRepository : IOrderRepository
{
    private readonly NorthwindContext context;

    public OrderRepository(NorthwindContext context)
    {
        this.context = context;
    }

    public async Task<RepositoryOrder> GetOrderAsync(long orderId)
    {
        var dbOrder = this.context.Orders.Find(orderId);
        if (dbOrder != null)
        {
            var dbCustomer = await this.context.Customers.FindAsync(dbOrder.CustomerID);
            var dbEmployee = await this.context.Employees.FindAsync(dbOrder.EmployeeID);
            var dbShipper = await this.context.Shippers.FindAsync(dbOrder.ShipVia);
            var dbOrderDetails = this.context.OrderDetails.Where(o => o.OrderID == orderId).ToList();

            if (dbShipper != null && dbCustomer != null && dbEmployee != null)
            {
                var customer = new RepositoryCustomer(new RepositoryCustomerCode(dbOrder.CustomerID))
                {
                    CompanyName = dbCustomer.CompanyName,
                };

                var employee = new RepositoryEmployee(dbOrder.EmployeeID)
                {
                    FirstName = dbEmployee.FirstName,
                    LastName = dbEmployee.LastName,
                    Country = dbEmployee.Country,
                };

                var shipper = new RepositoryShipper(dbOrder.ShipVia)
                {
                    CompanyName = dbShipper.CompanyName,
                };

                var shippingAddress = new RepositoryShippingAddress(
                    dbOrder.ShipAddress,
                    dbOrder.ShipCity,
                    dbOrder.ShipRegion,
                    dbOrder.ShipPostalCode,
                    dbOrder.ShipCountry);

                var orderDetail = new List<RepositoryOrderDetail>();

                foreach (var data in dbOrderDetails)
                {
                    var dbProduct = this.context.Products.Find(data.ProductID);

                    if (dbProduct != null)
                    {
                        var dbSupplier = this.context.Suppliers.Find(dbProduct.SupplierID);
                        var dbCategory = this.context.Categories.Find(dbProduct.CategoryID);

                        if (dbCategory != null && dbSupplier != null)
                        {
                            var product = new RepositoryProduct(data.ProductID)
                            {
                                ProductName = dbProduct.ProductName,
                                SupplierId = dbProduct.SupplierID,
                                CategoryId = dbProduct.CategoryID,
                                Supplier = dbSupplier.CompanyName,
                                Category = dbCategory.CategoryName,
                            };

                            orderDetail.Add(new RepositoryOrderDetail(new RepositoryOrder(dbOrder.OrderID))
                            {
                                UnitPrice = data.UnitPrice,
                                Quantity = data.Quantity,
                                Discount = data.Discount,
                                Product = product,
                            });
                        }
                        else
                        {
                            throw new OrderNotFoundException();
                        }
                    }
                    else
                    {
                        throw new OrderNotFoundException();
                    }
                }

                var order = new RepositoryOrder(dbOrder.OrderID)
                {
                    Customer = customer,
                    Employee = employee,
                    OrderDate = dbOrder.OrderDate,
                    RequiredDate = dbOrder.RequiredDate,
                    ShippedDate = dbOrder.ShippedDate.GetValueOrDefault(),
                    Shipper = shipper,
                    Freight = dbOrder.Freight,
                    ShipName = dbOrder.ShipName,
                    ShippingAddress = shippingAddress,
                };

                foreach (var data in orderDetail)
                {
                    order.OrderDetails.Add(data);
                }

                return order;
            }
            else
            {
                throw new OrderNotFoundException();
            }
        }

        throw new OrderNotFoundException();
    }

    public async Task<IList<RepositoryOrder>> GetOrdersAsync(int skip, int count)
    {
        Helper.GetOrdersCheck(skip, count);

        long id = 10248 + skip;
        var orders = new List<RepositoryOrder>();

        for (int i = 0; i < count; i++)
        {
            var order = this.GetOrderAsync(id);
            orders.Add(order.Result);
            id++;
        }

        return await Task.FromResult(orders);
    }

    public Task<long> AddOrderAsync(RepositoryOrder order)
    {
        if (order.Id == 0)
        {
            throw new RepositoryException();
        }

        foreach (var data in order.OrderDetails)
        {
            if (data.Product.Id == 0 || data.UnitPrice < 0 || data.Discount < 0 || data.Quantity < 1)
            {
                throw new RepositoryException();
            }
        }

        var dbOrder = new Entities.Order()
        {
            CustomerID = order.Customer.Code.Code,
            EmployeeID = order.Employee.Id,
            OrderDate = order.OrderDate,
            ShippedDate = order.ShippedDate,
            RequiredDate = order.RequiredDate,
            ShipVia = order.Shipper.Id,
            Freight = order.Freight,
            ShipName = order.ShipName,
            ShipAddress = order.ShippingAddress.Address,
            ShipCity = order.ShippingAddress.City,
            ShipCountry = order.ShippingAddress.Country,
            ShipPostalCode = order.ShippingAddress.PostalCode,
            ShipRegion = order.ShippingAddress.Region,
            Customer = this.context.Customers.Find(order.Customer.Code.Code) ?? throw new OrderNotFoundException(),
            Employee = this.context.Employees.Find(order.Employee.Id) ?? throw new OrderNotFoundException(),
            Shipper = this.context.Shippers.Find(order.Shipper.Id) ?? throw new OrderNotFoundException(),
        };

        var dbOrderDetails = new List<Entities.OrderDetail>();

        foreach (var item in order.OrderDetails)
        {
            var dbProduct = this.context.Products
                .Include(c => c.Category)
                .Include(s => s.Supplier)
                .FirstOrDefault(p => p.ProductID == item.Product.Id)
                ?? throw new OrderNotFoundException();

            dbOrderDetails.Add(new Entities.OrderDetail()
            {
                OrderID = order.Id,
                ProductID = dbProduct.ProductID,
                UnitPrice = item.UnitPrice,
                Discount = item.Discount,
                Quantity = item.Quantity,
                Product = dbProduct,
                Order = dbOrder,
            });
        }

        _ = this.context.Add(dbOrder);
        _ = this.context.SaveChanges();

        foreach (var item in dbOrderDetails)
        {
            dbOrder.OrderDetails.Add(item);

            _ = this.context.Add(item);
            _ = this.context.SaveChangesAsync();
        }

        return Task.FromResult(dbOrder.OrderID);
    }

    public async Task RemoveOrderAsync(long orderId)
    {
        var dbOrder = await this.context.Orders.FindAsync(orderId);

        if (dbOrder != null)
        {
            var dbOrderDetails = this.context.OrderDetails.Where(o => o.OrderID == orderId);

            foreach (var data in dbOrderDetails)
            {
                _ = this.context.OrderDetails.Remove(data);
                _ = await this.context.SaveChangesAsync();
            }

            _ = this.context.Orders.Remove(dbOrder);
            _ = this.context.SaveChangesAsync();
        }
        else
        {
            throw new OrderNotFoundException();
        }
    }

    public async Task UpdateOrderAsync(RepositoryOrder order)
    {
        var dbOrder = await this.context.Orders
        .Include(o => o.Customer)
        .Include(o => o.Employee)
        .Include(o => o.Shipper)
        .SingleOrDefaultAsync(o => o.OrderID == order.Id);

        var oldOrderDetails = this.context.OrderDetails
            .Include(p => p.Product)
            .Where(c => c.OrderID == order.Id);

        if (dbOrder != null)
        {
            var existingCustomer = this.context.Customers.FirstOrDefault(c => c.CustomerID == order.Customer.Code.Code);
            var existingEmployee = this.context.Employees.Find(order.Employee.Id);
            var existingShipper = this.context.Shippers.Find(order.Shipper.Id);

            if (existingCustomer == null || existingEmployee == null || existingShipper == null)
            {
                throw new OrderNotFoundException();
            }

            this.context.OrderDetails.RemoveRange(oldOrderDetails);
            _ = await this.context.SaveChangesAsync();

            dbOrder.CustomerID = existingCustomer.CustomerID;
            dbOrder.EmployeeID = existingEmployee.EmployeeID;
            dbOrder.ShipVia = existingShipper.ShipperID;

            dbOrder.OrderDate = order.OrderDate;
            dbOrder.RequiredDate = order.RequiredDate;
            dbOrder.ShippedDate = order.ShippedDate;
            dbOrder.Freight = order.Freight;
            dbOrder.ShipName = order.ShipName;
            dbOrder.ShipAddress = order.ShippingAddress.Address;
            dbOrder.ShipCity = order.ShippingAddress.City;
            dbOrder.ShipRegion = order.ShippingAddress.Region;
            dbOrder.ShipPostalCode = order.ShippingAddress.PostalCode;
            dbOrder.ShipCountry = order.ShippingAddress.Country;

            _ = await this.context.SaveChangesAsync();

            var dbOrderDetails = new List<Entities.OrderDetail>();
            foreach (var data in order.OrderDetails)
            {
                var dbSupplier = this.context.Suppliers.Find(data.Product.SupplierId);
                var dbCategory = this.context.Categories.Find(data.Product.CategoryId);
                var existingProduct = this.context.Products.Find(data.Product.Id);

                if (dbSupplier != null && dbCategory != null && existingProduct != null)
                {
                    var dbOrderDetail = new Entities.OrderDetail()
                    {
                        OrderID = order.Id,
                        ProductID = existingProduct.ProductID,
                        Product = existingProduct,
                        Order = dbOrder,
                        Discount = data.Discount,
                        Quantity = data.Quantity,
                        UnitPrice = data.UnitPrice,
                    };

                    dbOrderDetails.Add(dbOrderDetail);
                }
                else
                {
                    throw new OrderNotFoundException();
                }
            }

            dbOrder.OrderDetails = dbOrderDetails;

            await this.context.AddRangeAsync(dbOrderDetails);
            _ = await this.context.SaveChangesAsync();
        }
        else
        {
            throw new OrderNotFoundException();
        }
    }
}
