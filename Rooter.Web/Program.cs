using Rooter.Application.Interfaces;
using Rooter.Application.Services;
using Rooter.Infrastructure.Services;

var builder = WebApplication.CreateBuilder(args);

// Configure logging
builder.Logging.ClearProviders();
builder.Logging.AddConsole();
builder.Logging.AddDebug();

// Ensure console logging is configured
if (builder.Environment.IsDevelopment())
{
    builder.Logging.SetMinimumLevel(LogLevel.Debug);
}

// Add Blazor Server services
builder.Services.AddRazorPages();
builder.Services.AddServerSideBlazor();

// Register application services (Application layer interfaces with Infrastructure implementations)
builder.Services.AddScoped<IProjectAssetsParser, ProjectAssetsParser>();
builder.Services.AddScoped<IDependencyAnalyzer, DependencyAnalyzerService>();

var app = builder.Build();

// Add global exception handling middleware
app.Use(async (context, next) =>
{
    try
    {
        await next();
    }
    catch (Exception ex)
    {
        var logger = context.RequestServices.GetRequiredService<ILogger<Program>>();
        logger.LogError(ex, "Unhandled exception occurred. Request: {Method} {Path}",
            context.Request.Method, context.Request.Path);

        // Write error to console immediately
        Console.WriteLine($"[ERROR] {DateTime.Now:HH:mm:ss} Unhandled exception: {ex.Message}");
        Console.WriteLine($"[ERROR] Stack trace: {ex.StackTrace}");

        throw; // Re-throw to let ASP.NET Core handle the response
    }
});

// Configure the HTTP request pipeline
if (app.Environment.IsDevelopment())
{
    app.UseDeveloperExceptionPage(); // This will show detailed error pages in development
}
else
{
    app.UseExceptionHandler("/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();

app.MapRazorPages();
app.MapBlazorHub();
app.MapFallbackToPage("/_Host");

app.Run();
