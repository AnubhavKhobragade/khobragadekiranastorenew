using System.Text.Json;

var builder = WebApplication.CreateBuilder(args);
var app = builder.Build();
app.UseDefaultFiles();
app.UseStaticFiles();
var dataDir = Environment.GetEnvironmentVariable("DATA_DIR") ?? Path.Combine(AppContext.BaseDirectory, "data");
Directory.CreateDirectory(dataDir);
var dataFile = Path.Combine(dataDir, "store.json");
var jsonOptions = new JsonSerializerOptions { PropertyNameCaseInsensitive = true, WriteIndented = true };
var gate = new SemaphoreSlim(1, 1);

async Task<StoreData> Load()
{
    if (!File.Exists(dataFile))
    {
        var seed = new StoreData {
            Products = new() {
                new(1,"Fresh Tomatoes","Vegetables",32,"kg",25,"🍅"), new(2,"Bananas","Fruits",50,"dozen",18,"🍌"),
                new(3,"Full Cream Milk","Dairy",32,"500 ml",40,"🥛"), new(4,"Basmati Rice","Staples",110,"kg",30,"🍚"),
                new(5,"Brown Bread","Bakery",45,"pack",12,"🍞"), new(6,"Farm Eggs","Dairy",78,"6 pcs",20,"🥚"),
                new(7,"Potatoes","Vegetables",28,"kg",35,"🥔"), new(8,"Apples","Fruits",140,"kg",15,"🍎")
            }
        };
        await Save(seed); return seed;
    }
    return await JsonSerializer.DeserializeAsync<StoreData>(File.OpenRead(dataFile), jsonOptions) ?? new();
}
async Task Save(StoreData data)
{
    await using var stream = File.Create(dataFile);
    await JsonSerializer.SerializeAsync(stream, data, jsonOptions);
}
app.MapGet("/api/store", async () => { await gate.WaitAsync(); try { var d=await Load(); return Results.Ok(new { products=d.Products, orders=d.Orders }); } finally { gate.Release(); } });
app.MapPost("/api/products", async (ProductInput input) => {
    if (string.IsNullOrWhiteSpace(input.Name) || input.Price <= 0 || input.Stock < 0) return Results.BadRequest("Enter a name, positive price and valid stock.");
    await gate.WaitAsync(); try { var d=await Load(); var id=d.Products.Count==0?1:d.Products.Max(x=>x.Id)+1; d.Products.Add(new(id,input.Name.Trim(),input.Category?.Trim()??"Other",input.Price,input.Unit??"each",input.Stock,input.Emoji??"🛒")); await Save(d); return Results.Ok(); } finally { gate.Release(); }
});
app.MapPut("/api/products/{id:int}", async (int id, ProductInput input) => {
    await gate.WaitAsync(); try { var d=await Load(); var i=d.Products.FindIndex(x=>x.Id==id); if(i<0)return Results.NotFound(); if(string.IsNullOrWhiteSpace(input.Name)||input.Price<=0||input.Stock<0)return Results.BadRequest(); d.Products[i]=new(id,input.Name.Trim(),input.Category??"Other",input.Price,input.Unit??"each",input.Stock,input.Emoji??"🛒"); await Save(d); return Results.Ok(); } finally { gate.Release(); }
});
app.MapDelete("/api/products/{id:int}", async (int id) => { await gate.WaitAsync(); try { var d=await Load(); d.Products.RemoveAll(x=>x.Id==id); await Save(d); return Results.Ok(); } finally { gate.Release(); } });
app.MapPost("/api/orders", async (OrderInput input) => {
    if (string.IsNullOrWhiteSpace(input.Customer) || input.Items.Count==0) return Results.BadRequest("Customer and items are required.");
    await gate.WaitAsync(); try {
        var d=await Load(); decimal total=0; var lines=new List<OrderLine>();
        foreach(var item in input.Items){var p=d.Products.FirstOrDefault(x=>x.Id==item.ProductId); if(p is null||item.Quantity<1||p.Stock<item.Quantity)return Results.BadRequest($"Insufficient stock for {p?.Name ?? "product"}."); lines.Add(new(p.Name,item.Quantity,p.Price)); total+=p.Price*item.Quantity;}
        foreach(var item in input.Items){var i=d.Products.FindIndex(x=>x.Id==item.ProductId);var p=d.Products[i];d.Products[i]=p with { Stock=p.Stock-item.Quantity };}
        d.Orders.Insert(0,new(d.Orders.Count==0?1:d.Orders.Max(x=>x.Id)+1,input.Customer.Trim(),DateTime.UtcNow,lines,total)); await Save(d); return Results.Ok();
    } finally { gate.Release(); }
});
app.MapFallbackToFile("index.html");
var port=Environment.GetEnvironmentVariable("PORT")??"8080";
app.Urls.Add($"http://0.0.0.0:{port}");
app.Run();

record Product(int Id,string Name,string Category,decimal Price,string Unit,int Stock,string Emoji);
record ProductInput(string Name,string? Category,decimal Price,string? Unit,int Stock,string? Emoji);
record CartItem(int ProductId,int Quantity);
record OrderInput(string Customer,List<CartItem> Items);
record OrderLine(string Product,int Quantity,decimal UnitPrice);
record Order(int Id,string Customer,DateTime CreatedAt,List<OrderLine> Items,decimal Total);
class StoreData { public List<Product> Products {get;set;}=new(); public List<Order> Orders {get;set;}=new(); }
