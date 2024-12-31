using Microsoft.OpenApi.Models;
using OngakuVault.Services;
using System.Text.Json.Serialization;

// User defined allowed origins, if null, no origins where set
string[]? customCorsOrigins = Environment.GetEnvironmentVariable("OVERWRITE_CORS_ORIGIN")?.Split('|', StringSplitOptions.RemoveEmptyEntries) ?? null;

WebApplicationBuilder builder = WebApplication.CreateBuilder(args);
// Add services to the container & configure a additional json option to change enums to strings in api REST.
builder.Services.AddControllers().AddJsonOptions(options =>
 options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter()));
/// Add services

// Add WebSocketManagerService as a Singleton
builder.Services.AddSingleton<IWebSocketManagerService, WebSocketManagerService>();
// Add MediaDownloaderService as a Singleton
builder.Services.AddSingleton<IMediaDownloaderService, MediaDownloaderService>();
// Add a JobService as a Singleton (Parallel Method Execution Queue Service)
builder.Services.AddSingleton<IJobService, JobService>();

// Add Swagger to the service collection. Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
	options.SwaggerDoc("v1", new OpenApiInfo
	{
		Version = "v1",
		Title = "OngakuVault",
		Description = "An ASP.NET API for archieving audio/songs of a webpage locally on a device",
		License = new OpenApiLicense
		{
			Name = "Licensed under Apache 2.0",
			Url = new Uri("https://www.apache.org/licenses/LICENSE-2.0.txt")
		},
		Contact = new OpenApiContact
		{
			Name = "Github Repo",
			Url = new Uri("https://github.com/kitsumed/OngakuVault")
		},
	});
	options.EnableAnnotations();
});

// Add CORS services
builder.Services.AddCors(options =>
{
	// Default production CORS policy (allow all origins)
	options.AddDefaultPolicy(policy =>
	{
		policy.AllowAnyOrigin() // Allow all origins
			   .AllowAnyMethod() // Allow any HTTP method
			   .AllowAnyHeader(); // Allow any header
	});

	// If the env variable for cors origins is defined, allow the specified origin only
	if (customCorsOrigins != null)
	{
		// Allow a specific origin
		options.AddPolicy("OverwritePolicy", policy =>
		{
			policy.WithOrigins(customCorsOrigins)
				.AllowAnyMethod()
				.AllowAnyHeader();
		});
	}
});

var app = builder.Build();

// Configure the HTTP request pipeline.
if (Environment.GetEnvironmentVariable("ENFORCE_HTTPS") == "true") app.UseHttpsRedirection();

// Enable Swagger API docs
if (app.Environment.IsDevelopment() || Environment.GetEnvironmentVariable("ENABLE_SWAGGER_DOC") == "true")
{
	app.UseSwagger();  // Enable Swagger UI
	app.UseSwaggerUI();  // Enable Swagger UI for interactive API docs
}
else 
{
	// We take the predicate and verify if the request first segment is /swagger
	app.UseWhen(context => context.Request.Path.StartsWithSegments("/swagger"), appBuilder =>
	{
		// Custom middleware to handle the request
		appBuilder.Run(async requestContext =>
		{
			requestContext.Response.StatusCode = 405;
			await requestContext.Response.WriteAsync("Swagger API documentation is disabled in production environment. Set the environment variable 'ENABLE_SWAGGER_DOC' to 'true' to enable it.");
		});
	});
}

// Create websocket configuration, default allow all origins
WebSocketOptions webSocketOptions = new WebSocketOptions
{
	// Ensure websocket connection are kept alive with a heartbeat every 2 minutes
	KeepAliveInterval = TimeSpan.FromMinutes(2),
};

// Use the CORS DefaultPolicy if the env OVERWRITE_CORS_ORIGIN is null or empty
if (customCorsOrigins == null)
{
	app.UseCors();
}
else 
{
	app.UseCors("OverwritePolicy");
    // Restrict websocket to the origin mentioned in the ENV variable
    foreach (string currentUrl in customCorsOrigins)
    {
		webSocketOptions.AllowedOrigins.Add(currentUrl);
	}
} 

app.UseWebSockets(webSocketOptions);
app.MapControllers();

app.Run();