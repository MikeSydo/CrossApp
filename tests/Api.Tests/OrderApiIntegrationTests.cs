using System.Net;
using System.Net.Http.Json;
using Api.Contracts;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Testing;

namespace Api.Tests;

public class OrderApiIntegrationTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly HttpClient _client;

    public OrderApiIntegrationTests(WebApplicationFactory<Program> factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task Post_CreatesOrder_Returns201_And_GetByLocation_Returns200()
    {
        // 1. Arrange: Prepare valid order request
        var request = new SaveOrderRequest(
            Customer: "C-001",
            CustomerId: null,
            CreatedAt: DateTime.UtcNow,
            Lines:
            [
                new SaveOrderLineRequest("P-001", "Клавіатура", 850m, 2),
                new SaveOrderLineRequest("P-002", "Миша", 420m, 1)
            ]);

        // 2. Act: Send POST /api/orders
        var postResponse = await _client.PostAsJsonAsync("/api/orders", request);

        // 3. Assert POST: 201 Created and Location header present
        Assert.Equal(HttpStatusCode.Created, postResponse.StatusCode);

        var location = postResponse.Headers.Location;
        Assert.NotNull(location);

        var createdOrder = await postResponse.Content.ReadFromJsonAsync<OrderDto>();
        Assert.NotNull(createdOrder);
        Assert.Equal("C-001", createdOrder.Customer);
        Assert.Equal(2, createdOrder.Lines.Count);
        Assert.Equal(2120m, createdOrder.Total); // 850*2 + 420*1 = 2120

        // 4. Act: Send GET using the received Location
        var getResponse = await _client.GetAsync(location);

        // 5. Assert GET: 200 OK and data matches
        Assert.Equal(HttpStatusCode.OK, getResponse.StatusCode);

        var retrievedOrder = await getResponse.Content.ReadFromJsonAsync<OrderDto>();
        Assert.NotNull(retrievedOrder);
        Assert.Equal(createdOrder.Id, retrievedOrder.Id);
        Assert.Equal(createdOrder.Customer, retrievedOrder.Customer);
        Assert.Equal(createdOrder.Total, retrievedOrder.Total);
    }

    [Fact]
    public async Task Post_InvalidData_Returns400_ValidationProblemDetails()
    {
        // Arrange: Missing customer and empty lines
        var invalidRequest = new SaveOrderRequest(
            Customer: "",
            CustomerId: null,
            CreatedAt: null,
            Lines: []);

        // Act
        var response = await _client.PostAsJsonAsync("/api/orders", invalidRequest);

        // Assert: 400 Bad Request
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);

        var problem = await response.Content.ReadFromJsonAsync<HttpValidationProblemDetails>();
        Assert.NotNull(problem);
        Assert.True(problem.Errors.ContainsKey("EffectiveCustomer") || problem.Errors.ContainsKey("Customer"));
        Assert.True(problem.Errors.ContainsKey("Lines"));
    }

    [Fact]
    public async Task Post_DuplicateProductInLines_ThrowsDomainException_Returns409Conflict()
    {
        // Arrange: Request with duplicate product IDs (triggers domain rule in Order.AddLine)
        var request = new SaveOrderRequest(
            Customer: "C-001",
            CustomerId: null,
            CreatedAt: DateTime.UtcNow,
            Lines:
            [
                new SaveOrderLineRequest("P-001", "Клавіатура", 850m, 1),
                new SaveOrderLineRequest("P-001", "Клавіатура дублікат", 850m, 1)
            ]);

        // Act
        var response = await _client.PostAsJsonAsync("/api/orders", request);

        // Assert: 409 Conflict mapped by DomainExceptionHandler
        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);

        var problem = await response.Content.ReadFromJsonAsync<ProblemDetails>();
        Assert.NotNull(problem);
        Assert.Equal(409, problem.Status);
        Assert.Equal("Conflict", problem.Title);
        Assert.NotNull(problem.Detail);
        Assert.Contains("уже є в замовленні", problem.Detail);
    }

    [Fact]
    public async Task GetOrders_WithPaginationAndSearch_ReturnsPagedResult()
    {
        // Act: Request page 1 with pageSize 5
        var response = await _client.GetFromJsonAsync<PagedResult<OrderDto>>("/api/orders?page=1&pageSize=5");

        // Assert
        Assert.NotNull(response);
        Assert.True(response.Total > 0);
        Assert.True(response.Items.Count <= 5);
    }

    [Fact]
    public async Task GetOrderTotal_ReturnsCorrectTotal()
    {
        // Arrange: create an order first
        var request = new SaveOrderRequest(
            Customer: "C-002",
            CustomerId: null,
            CreatedAt: DateTime.UtcNow,
            Lines: [new SaveOrderLineRequest("P-003", "Монітор", 6500m, 1)]);

        var postResponse = await _client.PostAsJsonAsync("/api/orders", request);
        var created = await postResponse.Content.ReadFromJsonAsync<OrderDto>();
        Assert.NotNull(created);

        // Act: GET /api/orders/{id}/total
        var totalResponse = await _client.GetFromJsonAsync<OrderTotalResponse>($"/api/orders/{created.Id}/total");

        // Assert
        Assert.NotNull(totalResponse);
        Assert.Equal(created.Id, totalResponse.Id);
        Assert.Equal(6500m, totalResponse.Total);
    }

    [Fact]
    public async Task Delete_Order_Returns204_And_SubsequentGet_Returns404()
    {
        // Arrange: Create order
        var request = new SaveOrderRequest(
            Customer: "C-003",
            CustomerId: null,
            CreatedAt: DateTime.UtcNow,
            Lines: [new SaveOrderLineRequest("P-004", "USB-кабель", 180m, 1)]);

        var postResponse = await _client.PostAsJsonAsync("/api/orders", request);
        var location = postResponse.Headers.Location;
        Assert.NotNull(location);

        // Act 1: DELETE
        var deleteResponse = await _client.DeleteAsync(location);
        Assert.Equal(HttpStatusCode.NoContent, deleteResponse.StatusCode);

        // Act 2: Subsequent GET should return 404
        var getResponse = await _client.GetAsync(location);
        Assert.Equal(HttpStatusCode.NotFound, getResponse.StatusCode);
    }
}
