using Core.Domain;
using Core.Dto;

namespace Core.Import;

public static class OrderImporter
{
    public static ImportResult<Order> Convert(
        ImportResult<OrderDto> source)
    {
        ArgumentNullException.ThrowIfNull(source);

        var orders = new List<Order>();
        var errors = new List<string>(source.Errors);

        for (int i = 0; i < source.Items.Count; i++)
        {
            OrderDto dto = source.Items[i];

            try
            {
                orders.Add(Order.FromDto(dto));
            }
            catch (ArgumentException ex)
            {
                errors.Add(
                    $"Замовлення {i + 1} (Id: {dto?.Id ?? "<немає>"}): " +
                    ex.Message);
            }
            catch (InvalidOperationException ex)
            {
                errors.Add(
                    $"Замовлення {i + 1} (Id: {dto?.Id ?? "<немає>"}): " +
                    ex.Message);
            }
        }

        return new ImportResult<Order>(orders, errors);
    }
}