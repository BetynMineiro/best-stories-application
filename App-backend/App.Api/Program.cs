using System.IO.Compression;
using Microsoft.AspNetCore.ResponseCompression;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using App.Api;
using App.Api.Middleware;
using App.Api.Swagger;
using Microsoft.OpenApi;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddOpenApi();
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddHealthChecks();
builder.Services.AddMemoryCache();
builder.Services.AddLogging();
builder.Services.AddRouting(options => options.LowercaseUrls = true);
builder.Services.AddHttpContextAccessor();
builder.Services.AddHttpClient();
builder.Services.AddControllers(p => p.ConfigureFilters())
    .AddNewtonsoftJson(options =>
    {
        options.SerializerSettings.ContractResolver = new CamelCasePropertyNamesContractResolver();
        options.SerializerSettings.NullValueHandling = NullValueHandling.Ignore;
        options.SerializerSettings.DefaultValueHandling = DefaultValueHandling.Ignore;
    });

builder.Services.ConfigureApiServicesLayer(builder.Configuration, builder.Environment);
builder.Services.AddResponseCompression(options =>
{
    options.EnableForHttps = true;
    options.Providers.Add<GzipCompressionProvider>();
});
builder.Services.Configure<GzipCompressionProviderOptions>(options => { options.Level = CompressionLevel.Fastest; });
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowLocalhost", corsPolicyBuilder =>
    {
        corsPolicyBuilder.WithOrigins("http://127.0.0.1")
            .AllowAnyHeader()
            .AllowAnyMethod()
            .AllowCredentials();
    });
});

builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "Best Story API",
        Version = "v1",
        Description = "Best stories from Hacker News, ordered by score. Cursor-based pagination (pageSize 1–500, optional cursor). " +
                      "All responses share the same contract: `success`, `statusCode`, `messages` (when error), `data` (when success)."
    });
    options.OperationFilter<BestStoriesOperationFilter>();
});

var app = builder.Build();
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.UseSwagger();
    app.UseSwaggerUI(settings =>
    {
        settings.SwaggerEndpoint("/swagger/v1/swagger.json", "App Web API v1.0");
    });
    app.UseCors("AllowLocalhost");
}

app.UseResponseCompression();
app.UseHttpsRedirection();
app.UseMiddleware<ErrorHandlerMiddleware>();
app.MapControllers();
app.MapHealthChecks("/health");

app.Run();