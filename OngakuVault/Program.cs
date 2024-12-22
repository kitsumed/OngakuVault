using Microsoft.AspNetCore.Builder;
using Microsoft.OpenApi.Models;
using OngakuVault.Services;
using System.Text.Json.Serialization;
var builder = WebApplication.CreateBuilder(args);

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

	//Get the env variable to verify if we should overwrite the cors
	string? customOrigin = Environment.GetEnvironmentVariable("OVERWRITE_CORS_ORIGIN");

	// If the env variable is defined, allow the specified origin only
	if (!string.IsNullOrEmpty(customOrigin))
	{
		// Allow a specific origin
		options.AddPolicy("OverwritePolicy", policy =>
		{
			policy.WithOrigins(customOrigin)
				.AllowAnyMethod()
				.AllowAnyHeader();
		});
	}
});

var app = builder.Build();

// Configure the HTTP request pipeline.
app.UseHttpsRedirection();

// Enable Swagger API docs
if (app.Environment.IsDevelopment())
{
	app.UseSwagger();  // Enable Swagger UI
	app.UseSwaggerUI();  // Enable Swagger UI for interactive API docs
}

// Create websocket configuration, default allow all origins
WebSocketOptions webSocketOptions = new WebSocketOptions
{
	// Ensure websocket connection is kept alive with a heartbeat every 2 minutes
	KeepAliveInterval = TimeSpan.FromMinutes(2),
};

// Use the CORS DefaultPolicy if the env OVERWRITE_CORS_ORIGIN is null or empty
string? customOrigin = Environment.GetEnvironmentVariable("OVERWRITE_CORS_ORIGIN");
if (string.IsNullOrEmpty(customOrigin))
{
	app.UseCors();
}
else 
{
	app.UseCors("OverwritePolicy");
	// Restrict websocket to the origin mentioned in the ENV variable
	webSocketOptions.AllowedOrigins.Add(customOrigin);
} 

app.UseWebSockets(webSocketOptions);
app.MapControllers();

app.Run();
