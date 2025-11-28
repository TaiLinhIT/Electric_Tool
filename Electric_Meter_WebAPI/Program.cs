using Electric_Meter_WebAPI.Config;
using Electric_Meter_WebAPI.Interfaces;
using Electric_Meter_WebAPI.Models;
using Electric_Meter_WebAPI.Services;

using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

var configuration = builder.Configuration;
var appSetting = configuration.GetSection("AppSettings").Get<AppSetting>()
    ?? throw new NullReferenceException("AppSettings is null. Please check your appsetting.json file.");
builder.Services.AddSingleton(appSetting);
builder.Services.AddDbContext<PowerTempWatchContext>(options =>
    options.UseSqlServer(
        appSetting.ConnectString,
        sqlOptions =>
        {
            sqlOptions.CommandTimeout(240); // ⏱ Timeout 240 giây
        }
    )
);
builder.Services.AddScoped<Service>();
builder.Services.AddScoped<IService, Service>();

builder.Services.AddControllers();
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

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
