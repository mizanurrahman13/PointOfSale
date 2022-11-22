using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;
using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddDbContext<PosDb>(
    opt => opt.UseInMemoryDatabase("Sales"));

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.InferSecuritySchemes();
    options.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference{ Type = ReferenceType.SecurityScheme, Id="Bearer" }
            },
            new string[] {}
        }
    });
});
builder.Services.Configure<SwaggerGeneratorOptions>(options =>
{
    options.InferSecuritySchemes = true;
});

builder.Services.AddAuthentication().AddJwtBearer();
builder.Services.AddAuthorization();

var app = builder.Build();

app.UseSwagger();
app.UseSwaggerUI();

app.MapGet("/", () => "Hello Lamia!");

var sales = app.MapGroup("/sales").RequireAuthorization();

sales.MapGet("/", (PosDb db) =>
    db.Sales.ToList());

sales.MapPost("/", async (Sale sale, PosDb db) =>
{
    db.Sales.Add(sale);
    await db.SaveChangesAsync();
    return TypedResults.Created($"/sales/{sale.Id}", sale);
});

sales.MapGet("/{id}", async Task<Results<Ok<Sale>, NotFound>> (int id, PosDb db) =>
     await db.Sales.FindAsync(id)
     is Sale sale
     ? TypedResults.Ok(sale)
     : TypedResults.NotFound());

sales.MapPut("/{id}", async Task<Results<NotFound, NoContent>> (int id, Sale inputSale, PosDb db) => {
    var sale = await db.Sales.FindAsync(id);

    if (sale is null)
        return TypedResults.NotFound();

    sale.CustomerName = inputSale.CustomerName;
    sale.Total = inputSale.Total;

    await db.SaveChangesAsync();

    return TypedResults.NotFound();

});

sales.MapDelete("/{id}", async Task<Results<Ok<Sale>, NotFound>> (int id, PosDb db) =>
{
    if (await db.Sales.FindAsync(id) is Sale sale)
    {
        db.Sales.Remove(sale);
        await db.SaveChangesAsync();
        return TypedResults.Ok(sale);
    }

    return TypedResults.NotFound();
});

app.Run();

class Sale
{
    public int Id { get; set; }
    public string? CustomerName { get; set; }
    public int Total { get; set; }
}

class SaleDetail
{
    public int Id { get; set; }
    public int TotalAmount { get; set; }
    public string? Details { get; set; }

    public int SaleId { get; set; }
}

class PosDb: DbContext
{
    public PosDb(DbContextOptions<PosDb> options)
        : base(options) { }

    public DbSet<Sale> Sales => Set<Sale>();
    public DbSet<SaleDetail> SalesDetails => Set<SaleDetail>();
}
