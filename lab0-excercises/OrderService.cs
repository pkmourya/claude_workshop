public record OrderItem(decimal Price, int Quantity);

public decimal ProcessOrder(OrderItem[] items, decimal? discount, decimal? tax)
{
    decimal total = items
        .Where(item => item.Quantity > 0)
        .Sum(item => item.Price * item.Quantity);

    total -= total * (discount ?? 0) / 100;
    total += total * (tax ?? 0) / 100;

    return Math.Round(total, 2);
}
