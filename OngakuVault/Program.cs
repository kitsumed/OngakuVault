using Microsoft.OpenApi.Models;
using OngakuVault.Adapters;
using OngakuVault.Models;
using OngakuVault.Services;
using System.Text.Json;
using System.Text.Json.Serialization;

// Create the webApplication and define the "wwwroot" directory as the website root for static files
WebApplicationBuilder builder = WebApplication.CreateBuilder(new WebApplicationOptions {
	WebRootPath = "wwwroot",
});

builder.Configuration.AddEnvironmentVariables();
// Verify the starting command arguments to overwrite app settings
builder.Configuration.AddCommandLine(args);
// Load the app settings for usage during init (in program.cs)
AppSettingsModel appSettings = builder.Configuration.GetSection("Ongaku").Get<AppSettingsModel>() ?? new AppSettingsModel();
// User defined allowed origins, if null, no origins where set
string[]? customCorsOrigins = appSettings.OVERWRITE_CORS_ORIGIN_ARRAY;

// Add services to the container & configure json options
builder.Services.AddControllers().AddJsonOptions(options => {
	// Ensure json serializer use "camelCase" for keys name
	options.JsonSerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
	// Configure a additional json option to change all enums to strings in api REST
	// This make it so we don't need to specify it for every propriety
	options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
});

/// Add services

// Add a AppSettingsModel object that contains the app config to the services list (config is taken in order appsettings.json → env variable → run variable)
builder.Services.Configure<AppSettingsModel>(builder.Configuration.GetSection("Ongaku")); // Load values under "Ongaku"
// Add WebSocketManagerService as a Singleton (Service allowing management and interaction with websocket connections)
builder.Services.AddSingleton<IWebSocketManagerService, WebSocketManagerService>();
// Add MediaDownloaderService as a Singleton (Service allowing interaction with the scraper)
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
		Description = "An ASP.NET API for archieving audio/songs of a webpage locally on a device using yt-dlp as its scraper",
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
	// Uses code comments as description on swagger for elements that don't have a custom swagger description
	options.IncludeXmlComments(Path.Combine(AppContext.BaseDirectory, "OngakuVault.xml"));
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
if (appSettings.ENFORCE_HTTPS == true) app.UseHttpsRedirection();

// Enable Swagger API docs
if (app.Environment.IsDevelopment() || appSettings.ENABLE_SWAGGER_DOC == true)
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
if (appSettings.DISABLE_WEBSITE == false) 
{
	app.UseDefaultFiles(); // Url rewriter to support "index.html" like files
	app.UseStaticFiles(); // Allow app to serve files on the wwwroot directory
}

// Get the app loggerFactory & call the LoggerAdapter to redirect third-party logging to the app
ILoggerFactory loggerFactory = app.Services.GetRequiredService<ILoggerFactory>();
 _ = new LoggerAdapter(loggerFactory);

app.Run();