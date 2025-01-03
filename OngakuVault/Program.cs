using Microsoft.OpenApi.Models;
using OngakuVault.Services;
using System.Text.Json;
using System.Text.Json.Serialization;

// User defined allowed origins, if null, no origins where set
string[]? customCorsOrigins = Environment.GetEnvironmentVariable("OVERWRITE_CORS_ORIGIN")?.Split('|', StringSplitOptions.RemoveEmptyEntries) ?? null;

WebApplicationBuilder builder = WebApplication.CreateBuilder(args);
// Define the "wwwroot" directory as the directory exposed by the server as the website
builder.WebHost.UseWebRoot("wwwroot");

// Add services to the container & configure json options
builder.Services.AddControllers().AddJsonOptions(options => {
	// Ensure json serializer use "camelCase" for keys name
	options.JsonSerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
	// Configure a additional json option to change all enums to strings in api REST
	// This make it so we don't need to specify it for every propriety
	options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
});
/// Add services

// Add WebSocketManagerService as a Singleton (Service allowing management and interaction with websocket connections)
builder.Services.AddSingleton<IWebSocketManagerService, WebSocketManagerService>();
// Add MediaDownloaderService as a Singleton (Service allowing interaction with the scraper)
builder.Services.AddSingleton<IMediaDownloaderService, MediaDownloaderService>();
// Add ATLCoreLoggingHandlerService as a Singleton (Redirect ATL library logs to our app)
builder.Services.AddSingleton<ATLCoreLoggingHandlerService>();
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

WebApplication app = builder.Build();

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

// Verify if website (static file serving) is disabled in env variable
if (Environment.GetEnvironmentVariable("DISABLE_WEBSITE") != "true") 
{
	app.UseDefaultFiles(); // Url rewriter to support "index.html" like files
	app.UseStaticFiles(); // Allow app to serve files on the wwwroot directory
}

/// Force initialisation of some services as they need to be "called" at least once to be created and kept
/// during the whole process life-time
_ = app.Services.GetRequiredService<ATLCoreLoggingHandlerService>(); // Init the service in charge of redirecting ATL logs to our logs
app.Run();