using System.Text.Json.Serialization;
using Api;
using Api.Contracts;
using Api.Endpoints;
using Api.Validation;
using Core;
using Core.Abstractions;
using Core.Storage;
using FluentValidation;
using Scalar.AspNetCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.ConfigureHttpJsonOptions(options =>
{
    options.SerializerOptions.Converters.Add(new JsonStringEnumConverter());
});

builder.Services.AddOpenApi();
builder.Services.AddProblemDetails();
builder.Services.AddExceptionHandler<DomainExceptionHandler>();
builder.Services.AddExceptionHandler<GlobalExceptionHandler>();

builder.Services.AddScoped<IValidator<SaveOrderRequest>, SaveOrderRequestValidator>();
builder.Services.AddScoped<IValidator<SaveOrderLineRequest>, SaveOrderLineRequestValidator>();

builder.Services.AddSingleton<IOrderStore>(_ => new InMemoryOrderStore(SampleData.Orders()));

var app = builder.Build();

app.UseExceptionHandler();
app.UseStatusCodePages();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.MapScalarApiReference();
}

app.MapGet("/health", () => Results.Ok(new { status = "ok" }));
app.MapOrders();

await app.RunAsync();

public partial class Program { }
