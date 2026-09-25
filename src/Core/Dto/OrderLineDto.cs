namespace Core.Dto;

public sealed record OrderLineDto(string ProductId, string Name, decimal Price, int Quantity) : 
    ProductDto(ProductId, Name, Price);