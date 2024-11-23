using Microsoft.OpenApi.Models;
using OngakuVault.Services;
using YoutubeDLSharp;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllers();

/// Add services

// Add the yt-dlp wrapper as a Singleton (with 8 parallel yt-dlp process allowed to run)
builder.Services.AddSingleton(new YoutubeDL(8));
// Add JobService as a Singleton
builder.Services.AddSingleton<IJobService, JobService>();
// Add the JobCleanupService
builder.Services.AddHostedService<JobCleanupService>();
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
