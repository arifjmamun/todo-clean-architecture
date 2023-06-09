using Microsoft.EntityFrameworkCore;
using Serilog;
using Serilog.Events;
using TodoCQRS.Application.Handlers;
using TodoCQRS.Application.Mapping;
using TodoCQRS.Infrastructure.Data.Context;
using TodoCQRS.Infrastructure.Data.Repositories;

var builder = WebApplication.CreateBuilder(args);

// Configure logging
Log.Logger = new LoggerConfiguration()
    .Enrich.FromLogContext()
    .MinimumLevel.Debug()
    .MinimumLevel.Override("Microsoft", LogEventLevel.Warning)
    .MinimumLevel.Override("System", LogEventLevel.Warning)
    .WriteTo.Console()
    .CreateLogger();

builder.Host.UseSerilog();

// Configure services
MappingConfig.Configure();

if (builder.Environment.IsEnvironment("Test"))
    builder.Services.AddDbContext<ApplicationDbContext>(options =>
        options.UseInMemoryDatabase("TodoTestDb"));
else
    builder.Services.AddDbContext<ApplicationDbContext>(
        options => options.UseSqlite(
            builder.Configuration.GetConnectionString(nameof(ApplicationDbContext)),
            x => x.MigrationsAssembly("TodoCQRS.Infrastructure")
        )
    );

builder.Services.AddMediatR(cfg => cfg.RegisterServicesFromAssembly(typeof(CreateTodoCommandHandler).Assembly));
builder.Services.AddScoped<ITodoRepository, TodoRepository>();

builder.Services.Configure<RouteOptions>(options => { options.LowercaseUrls = true; });

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

MigrateDatabase();

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

void MigrateDatabase()
{
    if (app.Environment.IsEnvironment("Test")) return;

    using var serviceScope = app.Services.GetRequiredService<IServiceScopeFactory>().CreateScope();
    using var context = serviceScope.ServiceProvider.GetService<ApplicationDbContext>();
    context!.Database.Migrate();
}