using Microsoft.OpenApi.Models;
using OngakuVault.Models;
using OngakuVault.Services;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllers();

/// Add services

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
		Description = "An ASP.NET API for archieving songs locally on a device",
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

var app = builder.Build();
// Configure the HTTP request pipeline.

app.UseHttpsRedirection();

// Enable Swagger API docs
if (app.Environment.IsDevelopment())
{
	app.UseSwagger();  // Enable Swagger UI
	app.UseSwaggerUI();  // Enable Swagger UI for interactive API docs
}


//app.UseAuthorization();

app.MapControllers();

app.Run();
