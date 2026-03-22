using Microsoft.EntityFrameworkCore;
using OneManVekery.Models.Db;
using OneManVekery.Services;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllersWithViews();
builder.Services.AddDbContext<OneManVekeryDBContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("OneManVekeryDb")));
builder.Services.AddSingleton<IInventoryCatalogService, InMemoryInventoryCatalogService>();
builder.Services.AddScoped<IAccountDirectoryService, DbAccountDirectoryService>();
builder.Services.AddSingleton<IStoreCatalogService, InMemoryStoreCatalogService>();
builder.Services.AddHttpContextAccessor();
builder.Services.AddDistributedMemoryCache();
builder.Services.AddSession(options =>
{
    options.Cookie.Name = ".OneManVekery.Session";
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
    options.IdleTimeout = TimeSpan.FromHours(8);
});
builder.Services.AddScoped<IStoreCartService, SessionStoreCartService>();
builder.Services.AddScoped<IStoreOrderService, SessionStoreOrderService>();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseRouting();
app.UseSession();

app.UseAuthorization();

app.MapStaticAssets();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}")
    .WithStaticAssets();


app.Run();
