using Nexus.Core.Services;
using Nexus.Linker.Services;
using Microsoft.Extensions.Caching.Memory;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Core Services
builder.Services.AddMemoryCache(); // Cache for CachedCodeIndexer
// Register the base CodeIndexer and wrap it with CachedCodeIndexer for performance
builder.Services.AddScoped<CodeIndexer>(); // Concrete implementation
builder.Services.AddScoped<ICodeIndexer>(sp =>
{
    var baseIndexer = sp.GetRequiredService<CodeIndexer>();
    var cache = sp.GetRequiredService<IMemoryCache>();
    return new CachedCodeIndexer(baseIndexer, cache);
});
builder.Services.AddScoped<IContextCompiler, ContextCompiler>();

// Rule Provider - Singleton since rules are read-only for MVP
builder.Services.AddSingleton<IRuleProvider, InMemoryRuleProvider>();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseAuthorization();

app.MapControllers();

app.Run();
