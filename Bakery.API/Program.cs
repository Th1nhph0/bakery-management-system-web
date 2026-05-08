using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);
// Đăng ký DbContext
builder.Services.AddDbContext<Bakery.Data.Models.BakeryManagementDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));
// Add services to the container.

builder.Services.AddControllers();
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddScoped<Bakery.Services.SanPhamService>();
builder.Services.AddScoped<Bakery.Services.DonHangService>();
builder.Services.AddScoped<Bakery.Services.KhuyenMaiService>();
builder.Services.AddScoped<Bakery.Services.DonBanhCustomService>();
var app = builder.Build();
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.UseAuthorization();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.Run();
