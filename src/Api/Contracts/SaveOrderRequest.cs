namespace Api.Contracts;

public sealed record SaveOrderLineRequest(
    string? ProductId,
    string? Name,
    decimal Price,
    int Quantity);

public sealed record SaveOrderRequest(
    string? Customer,
    string? CustomerId,
    DateTime? CreatedAt,
    List<SaveOrderLineRequest>? Lines)
{
    public string EffectiveCustomer =>
        !string.IsNullOrWhiteSpace(Customer) ? Customer.Trim() : (CustomerId?.Trim() ?? string.Empty);

    public bool TryValidate(out Dictionary<string, string[]> errors)
    {
        var found = new Dictionary<string, string[]>();

        var customer = EffectiveCustomer;
        if (string.IsNullOrWhiteSpace(customer))
        {
            found[nameof(Customer)] = ["Ідентифікатор/ім'я клієнта є обов'язковим."];
        }
        else if (customer.Length > 100)
        {
            found[nameof(Customer)] = ["Ідентифікатор/ім'я клієнта не може перевищувати 100 символів."];
        }

        if (Lines is null || Lines.Count == 0)
        {
            found[nameof(Lines)] = ["Список товарів (lines) не може бути порожнім."];
        }
        else
        {
            for (int i = 0; i < Lines.Count; i++)
            {
                var line = Lines[i];

                if (string.IsNullOrWhiteSpace(line.ProductId))
                {
                    found[$"{nameof(Lines)}[{i}].{nameof(line.ProductId)}"] = ["Ідентифікатор товару є обов'язковим."];
                }
                else if (line.ProductId.Length > 50)
                {
                    found[$"{nameof(Lines)}[{i}].{nameof(line.ProductId)}"] = ["Ідентифікатор товару не може перевищувати 50 символів."];
                }

                if (string.IsNullOrWhiteSpace(line.Name))
                {
                    found[$"{nameof(Lines)}[{i}].{nameof(line.Name)}"] = ["Назва товару є обов'язковою."];
                }
                else if (line.Name.Length > 200)
                {
                    found[$"{nameof(Lines)}[{i}].{nameof(line.Name)}"] = ["Назва товару не може перевищувати 200 символів."];
                }

                if (line.Price < 0)
                {
                    found[$"{nameof(Lines)}[{i}].{nameof(line.Price)}"] = ["Ціна товару не може бути від'ємною."];
                }

                if (line.Quantity <= 0)
                {
                    found[$"{nameof(Lines)}[{i}].{nameof(line.Quantity)}"] = ["Кількість товару повинна бути більшою за нуль."];
                }
            }

            var duplicateProduct = Lines
                .Where(l => !string.IsNullOrWhiteSpace(l.ProductId))
                .GroupBy(l => l.ProductId!, StringComparer.OrdinalIgnoreCase)
                .FirstOrDefault(g => g.Count() > 1);

            if (duplicateProduct != null)
            {
                found[nameof(Lines)] = [$"Товар '{duplicateProduct.Key}' повторюється в рядках замовлення."];
            }
        }

        errors = found;
        return found.Count == 0;
    }
}
