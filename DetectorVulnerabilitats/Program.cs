
using DetectorVulnerabilitats.Services;
using DetectorVulnerabilitatsDatabase.Context;
using Microsoft.EntityFrameworkCore;
using System.Text.Json.Serialization;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddControllers().AddJsonOptions(options =>
{
    // Aquesta línia trenca el cicle infinit:
    options.JsonSerializerOptions.ReferenceHandler = ReferenceHandler.IgnoreCycles;
});
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddSingleton<IQueueService, QueueService>();
builder.Services.AddHostedService<ScanUpdateListener>();
builder.Services.AddScoped<ResultsReaderService>();
builder.Services.AddSignalR();

builder.Services.AddDbContext<DetectorVulnerabilitatsDatabaseContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));

builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAngular",
        policy =>
        {
            policy.WithOrigins("http://localhost:55468") // O "*" per desenvolupament
                  .AllowAnyHeader()
                  .AllowAnyMethod()
                  .AllowCredentials();
        });
});


var app = builder.Build();

app.MapHub<ScanHub>("/scanhub");

using (var scope = app.Services.CreateScope())
{
    var dbContext = scope.ServiceProvider.GetRequiredService<DetectorVulnerabilitatsDatabaseContext>();
    dbContext.Database.Migrate();
}

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
    app.UseCors("AllowAngular");
}

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.Run();
