using DetectorVulnerabilitatsDatabase.Context;
using Microsoft.EntityFrameworkCore;
using Worker.Operations;
using Worker.Services;

var builder = WebApplication.CreateBuilder(args);
builder.Configuration.AddJsonFile("Secrets.json", optional: true, reloadOnChange: true);
// Add services to the container.

builder.Services.AddControllers();
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddSingleton<IQueueService, QueueService>();
builder.Services.AddHostedService<QueueWorker>();
builder.Services.AddDbContext<DetectorVulnerabilitatsDatabaseContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));
builder.Services.AddScoped<DbOperations>();
builder.Services.AddHttpClient<LocalAiService>();

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