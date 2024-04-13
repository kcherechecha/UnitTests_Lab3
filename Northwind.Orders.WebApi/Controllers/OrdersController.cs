using Microsoft.AspNetCore.Mvc;
using Northwind.Orders.WebApi.Models;
using Northwind.Services.Repositories;

namespace Northwind.Orders.WebApi.Controllers;

[ApiController]
[Route("api/orders")]
public sealed class OrdersController : ControllerBase
{
    private readonly IOrderRepository orderRepository;
    private readonly ILogger<OrdersController> logger;

    public OrdersController(IOrderRepository orderRepository, ILogger<OrdersController> logger)
    {
        this.orderRepository = orderRepository;
        this.logger = logger;
    }

    [HttpGet("{orderId}")]
    public async Task<ActionResult<FullOrder>> GetOrderAsync(long orderId)
    {
        this.logger.LogInformation("Order with ID {@orderId} was requested.", orderId);

        this.logger.LogInformation("Check Exeption for Get repository method called");
        var dbOrderCheck = this.orderRepository.GetOrderAsync(orderId).Exception;

        if (dbOrderCheck?.InnerException != null)
        {
            var ex = dbOrderCheck.InnerException.GetType();

            if (ex == typeof(Exception))
            {
                this.logger.LogError("Internal Server Error for Order @orderId ", orderId);
                return await Task.FromResult(this.StatusCode(500));
            }

            if (ex == typeof(OrderNotFoundException))
            {
                this.logger.LogError("Order @orderId not found", orderId);
                return await Task.FromResult(this.NotFound());
            }
        }

        this.logger.LogInformation("Get repository method called");
        var dbOrder = await this.orderRepository.GetOrderAsync(orderId);

        var fullOrderDetails = new List<FullOrderDetail>();

        foreach (var item in dbOrder.OrderDetails)
        {
            fullOrderDetails.Add(new FullOrderDetail()
            {
                Discount = item.Discount,
                CategoryId = item.Product.CategoryId,
                CategoryName = item.Product.Category,
                SupplierCompanyName = item.Product.Supplier,
                ProductId = item.Product.Id,
                ProductName = item.Product.ProductName,
                Quantity = item.Quantity,
                SupplierId = item.Product.SupplierId,
                UnitPrice = item.UnitPrice,
            });
        }

        var fullOrder = new FullOrder()
        {
            Id = orderId,
            Customer = new Models.Customer()
            {
                Code = dbOrder.Customer.Code.Code,
                CompanyName = dbOrder.Customer.CompanyName,
            },
            Employee = new Models.Employee()
            {
                Id = dbOrder.Employee.Id,
                FirstName = dbOrder.Employee.FirstName,
                LastName = dbOrder.Employee.LastName,
                Country = dbOrder.Employee.Country,
            },
            OrderDate = dbOrder.OrderDate,
            RequiredDate = dbOrder.RequiredDate,
            ShippedDate = dbOrder.ShippedDate,
            Shipper = new Models.Shipper()
            {
                Id = dbOrder.Shipper.Id,
                CompanyName = dbOrder.Shipper.CompanyName,
            },
            Freight = dbOrder.Freight,
            ShipName = dbOrder.ShipName,
            ShippingAddress = new Models.ShippingAddress()
            {
                Address = dbOrder.ShippingAddress.Address,
                City = dbOrder.ShippingAddress.City,
                Region = dbOrder.ShippingAddress.Region,
                Country = dbOrder.ShippingAddress.Country,
                PostalCode = dbOrder.ShippingAddress.PostalCode,
            },
            OrderDetails = new List<FullOrderDetail>(fullOrderDetails),
        };

        this.logger.LogInformation("Order with ID {@orderId} was succesfully returned.", orderId);
        return await Task.FromResult(this.Ok(fullOrder));
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<BriefOrder>>> GetOrdersAsync(int? skip, int? count)
    {
        count = count == null ? 10 : count;
        skip = skip == null ? 0 : skip;

        this.logger.LogInformation("Order with skip {@skip} and count {@count} was requested.", skip, count);

        this.logger.LogInformation("GetOrders repository method called");
        var dbOrdersCheck = this.orderRepository.GetOrdersAsync(skip.Value, count.Value);

        if (dbOrdersCheck.Exception != null)
        {
            this.logger.LogError("Internal Server Error for Order with skip {@skip} and count {@count}.", skip, count);
            return await Task.FromResult(this.StatusCode(500));
        }

        var dbOrders = dbOrdersCheck.Result;

        if (dbOrders == null)
        {
            return await Task.FromResult(new BadRequestResult());
        }

        var briefOrder = new List<BriefOrder>();

        foreach (var item in dbOrders)
        {
            var briefOrderDetail = new List<BriefOrderDetail>();

            foreach (var data in item.OrderDetails)
            {
                briefOrderDetail.Add(new BriefOrderDetail()
                {
                    ProductId = data.Product.Id,
                    UnitPrice = data.UnitPrice,
                    Quantity = data.Quantity,
                    Discount = data.Discount,
                });
            }

            briefOrder.Add(new BriefOrder
            {
                Id = item.Id,
                CustomerId = item.Customer.Code.Code,
                EmployeeId = item.Employee.Id,
                OrderDate = item.OrderDate,
                RequiredDate = item.RequiredDate,
                ShippedDate = item.ShippedDate,
                ShipperId = item.Shipper.Id,
                Freight = item.Freight,
                ShipName = item.ShipName,
                ShipAddress = item.ShippingAddress.Address,
                ShipCity = item.ShippingAddress.City,
                ShipRegion = item.ShippingAddress.Region,
                ShipPostalCode = item.ShippingAddress.PostalCode,
                ShipCountry = item.ShippingAddress.Country,
                OrderDetails = new List<BriefOrderDetail>(briefOrderDetail),
            });
        }

        this.logger.LogInformation("Order with skip {@skip} and count {@count} was succesfully returned.", skip, count);
        return this.Ok(briefOrder);
    }

    [HttpPost]
    public async Task<ActionResult<AddOrder>> AddOrderAsync(BriefOrder order)
    {
        this.logger.LogInformation("Add method was called for Order");

        var orderDetails = new List<OrderDetail>();

        foreach (var item in order.OrderDetails)
        {
            orderDetails.Add(new OrderDetail(new Order(order.Id))
            {
                Product = new Product(item.ProductId),
                Discount = item.Discount,
                Quantity = item.Quantity,
                UnitPrice = item.UnitPrice,
            });
        }

        var dbOrder = new Order(order.Id)
        {
            Customer = new Northwind.Services.Repositories.Customer(new CustomerCode(order.CustomerId)),
            Employee = new Northwind.Services.Repositories.Employee(order.EmployeeId),
            OrderDate = order.OrderDate,
            RequiredDate = order.RequiredDate,
            ShippedDate = order.ShippedDate.GetValueOrDefault(),
            Shipper = new Northwind.Services.Repositories.Shipper(order.ShipperId),
            Freight = order.Freight,
            ShipName = order.ShipName,
            ShippingAddress = new Northwind.Services.Repositories.ShippingAddress(order.ShipAddress, order.ShipCity, order.ShipRegion, order.ShipPostalCode, order.ShipCountry),
        };

        this.logger.LogInformation("Add repository method called");
        var checkEx = this.orderRepository.AddOrderAsync(dbOrder).Exception;

        if (checkEx != null)
        {
            this.logger.LogError("Internal Server Error add Method for Order");
            return await Task.FromResult(this.StatusCode(500));
        }
        else
        {
            this.logger.LogInformation("Order with id {@order.Id} was succesfully Added.", order.Id);
            return new OkObjectResult(new AddOrder() { OrderId = this.orderRepository.AddOrderAsync(dbOrder).Result, });
        }
    }

    [HttpDelete("{orderId}")]
    public async Task<ActionResult> RemoveOrderAsync(long orderId)
    {
        this.logger.LogInformation("Remove method was called for Order with id {@orderId}", orderId);

        this.logger.LogInformation("Remove repository method called");
        var dbOrderCheck = this.orderRepository.RemoveOrderAsync(orderId).Exception;

        if (dbOrderCheck?.InnerException != null)
        {
            var ex = dbOrderCheck.InnerException.GetType();

            if (ex == typeof(Exception))
            {
                this.logger.LogError("Internal Server Error for Order @orderId ", orderId);
                return await Task.FromResult(this.StatusCode(500));
            }

            if (ex == typeof(OrderNotFoundException))
            {
                this.logger.LogError("Order @orderId not found", orderId);
                return await Task.FromResult(this.NotFound());
            }
        }

        this.logger.LogInformation("Order with id {@orderId} was succesfully deleted", orderId);

        return new NoContentResult();
    }

    [HttpPut("{orderId}")]
    public async Task<ActionResult> UpdateOrderAsync(long orderId, BriefOrder order)
    {
        this.logger.LogInformation("Update method was called for Order with id {@orderId}", orderId);

        var dbOrder = new Order(orderId)
        {
            Customer = new Northwind.Services.Repositories.Customer(new CustomerCode(order.CustomerId)),
            Employee = new Northwind.Services.Repositories.Employee(order.EmployeeId),
            OrderDate = order.OrderDate,
            RequiredDate = order.RequiredDate,
            ShippedDate = order.ShippedDate.GetValueOrDefault(),
            Shipper = new Northwind.Services.Repositories.Shipper(order.ShipperId),
            Freight = order.Freight,
            ShipName = order.ShipName,
            ShippingAddress = new Northwind.Services.Repositories.ShippingAddress(order.ShipAddress, order.ShipCity, order.ShipRegion, order.ShipPostalCode, order.ShipCountry),
        };

        foreach (var item in order.OrderDetails)
        {
            dbOrder.OrderDetails.Add(new OrderDetail(dbOrder)
            {
                Product = new Product(item.ProductId),
                UnitPrice = item.UnitPrice,
                Quantity = item.Quantity,
                Discount = item.Discount,
            });
        }

        this.logger.LogInformation("Update repository method called");
        var dbOrderCheck = this.orderRepository.UpdateOrderAsync(dbOrder).Exception;

        if (dbOrderCheck?.InnerException != null)
        {
            var ex = dbOrderCheck.InnerException.GetType();

            if (ex == typeof(Exception))
            {
                this.logger.LogError("Internal Server Error for Order @orderId ", orderId);
                return await Task.FromResult(this.StatusCode(500));
            }

            if (ex == typeof(OrderNotFoundException))
            {
                this.logger.LogError("Order @orderId not found", orderId);
                return await Task.FromResult(this.NotFound());
            }
        }

        return new NoContentResult();
    }
}
