using Bakery.Data;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// 1. ĐĂNG KÝ DATABASE
builder.Services.AddDbContext<Bakery.Data.Models.BakeryManagementDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();

// CẤU HÌNH SWAGGER PHẲNG: Chỉ giữ lại dòng trị lỗi trùng tên DTO
builder.Services.AddSwaggerGen(options =>
{
    options.CustomSchemaIds(type => type.FullName);
});

// 3. ĐĂNG KÝ SERVICES SYSTEM
builder.Services.AddScoped<Bakery.Services.KhachHangService>();
builder.Services.AddScoped<Bakery.Services.SanPhamService>();
builder.Services.AddScoped<Bakery.Services.DonHangService>();
builder.Services.AddScoped<Bakery.Services.KhuyenMaiService>();
builder.Services.AddScoped<Bakery.Services.DonBanhCustomService>();
builder.Services.AddScoped<AdoNetHelper>();

var app = builder.Build();

// 4. PIPELINE RUN
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseDefaultFiles();
app.UseStaticFiles();
app.UseHttpsRedirection();
app.UseAuthorization();
app.MapControllers();
app.Run();