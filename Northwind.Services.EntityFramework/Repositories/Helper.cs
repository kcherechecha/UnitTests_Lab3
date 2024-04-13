namespace Northwind.Services.EntityFramework.Repositories;
public static class Helper
{
    public static void GetOrdersCheck(int skip, int count)
    {
        if (skip < 0 || count < 1)
        {
            throw new ArgumentOutOfRangeException(nameof(skip));
        }
    }
}
