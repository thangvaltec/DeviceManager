using DeviceApi.Data;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// ===== Đăng ký service =====
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// CORS cho frontend (Vite dev server)
var allowedOrigins = new[]
{
    "http://localhost:5173",
    "http://10.200.2.29:5173"
};

builder.Services.AddCors(options =>
{
    options.AddPolicy("FrontendCors", policy =>
    {
        policy.WithOrigins(allowedOrigins)
              .AllowAnyHeader()
              .AllowAnyMethod();
    });
});

// Đăng ký DbContext với ConnectionString
builder.Services.AddDbContext<DeviceDbContext>(options =>
    options.UseSqlServer(
        builder.Configuration.GetConnectionString("DefaultConnection")
    )
);

var app = builder.Build();

// ===== Pipeline xử lý HTTP =====
if (app.Environment.IsDevelopment())
{
    app.UseDeveloperExceptionPage();
    app.UseSwagger();
    app.UseSwaggerUI();
}

// Không bắt buộc HTTPS trong môi trường dev nội bộ
// app.UseHttpsRedirection();

// ===== ① Thêm 2 dòng này để phục vụ file tĩnh (frontend) =====
app.UseDefaultFiles();  // tự động tìm index.html trong wwwroot
app.UseStaticFiles();   // cho phép trả file tĩnh từ wwwroot
// ==========================================================

app.UseCors("FrontendCors"); // cho phép frontend gọi API

app.UseAuthorization();

app.MapControllers();   // /api/... vẫn hoạt động như cũ

app.Run();
