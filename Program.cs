using dooo.Models;
using dooo.Services;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddControllers();

// 註冊 DbContext Pool
// builder.Services.AddDbContextFactory<WorldContext>(options =>
// {
//     // 設定連接字串及其他選項
//     options.UseMySQL(builder.Configuration.GetConnectionString("DefaultConnection") ?? "");

//     // 設定 DbContext Pool 相關選項
//     options.EnableSensitiveDataLogging()
//         .EnableDetailedErrors();
// });

builder.Services.AddDbContext<WorldContext>(options => options.UseMySQL(builder.Configuration.GetConnectionString("DefaultConnection") ?? ""));

var c = builder.Configuration.GetConnectionString("RedisCacheUrl");
builder.Services.AddStackExchangeRedisCache(options => { options.Configuration = c; });

// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddScoped<IMyService, MyService>();

// var connectionString = !string.IsNullOrEmpty(Configuration[CONNECTION_STRING_CONFIG_VAR]) ? Configuration[CONNECTION_STRING_CONFIG_VAR] : DEFAULT_CONNECTION_STRING;

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.Run();
