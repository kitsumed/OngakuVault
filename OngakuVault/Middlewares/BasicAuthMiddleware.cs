using OngakuVault.Models;
using System.Text;

namespace OngakuVault.Middlewares
{
	/// <summary>
	/// This class is a middleware that allows for basic authentication using a username and password.
	/// It is intended as a really simple auth method to protect the static files and the API.
	/// 
	/// <strong>As it is initialised before AppSetting, the valid credentials need to be passed as a string type.</strong>
	/// For example, inside program.cs adding the middleware to the pipeline:
	/// <code>
	/// <![CDATA[
	/// app.UseMiddleware<BasicAuthMiddleware>(appSettings.BASIC_AUTH_CREDENTIALS);
	/// ]]>
	/// </code>
	/// </summary>
	public class BasicAuthMiddleware
	{
		// Store the next middleware in the pipeline
		private readonly RequestDelegate _next;

		// Store the username and password for authentication
		private readonly string _username;
		private readonly string _password;

		public BasicAuthMiddleware(RequestDelegate next, string BasicAuthCredentials)
        {
			_next = next;

			// Parse the valid credentials from the app settings
			if (BasicAuthCredentials == string.Empty) throw new InvalidOperationException("BasicAuthMiddleware cannot be used if no valid credentials where defined."); 
			string[] validCredentials = BasicAuthCredentials.Split(':', 2);
			if (validCredentials.Length != 2) throw new InvalidOperationException($"BasicAuthMiddleware format need to be 'username:password' (2 args) but parser found {validCredentials.Length} args.");
			_username = validCredentials[0];
			_password = validCredentials[1];
		}

		// Called when a request reached this middleware
		public async Task InvokeAsync(HttpContext context) 
		{
			// Prompt client for http auth
			if (!context.Request.Headers.TryGetValue("Authorization", out var authHeader) || !authHeader.ToString().StartsWith("Basic "))
			{
				context.Response.StatusCode = 401;
				context.Response.Headers["WWW-Authenticate"] = "Basic realm=\"Please authenticate yourself\", charset=\"UTF-8\"";
				await context.Response.WriteAsync("Authorization header missing or using invalid format.");
				return;
			}

			// Remove the "Basic " prefix and decode the base64 string
			string encodedCredentials = authHeader.ToString().Substring(6).Trim();
			string decodedCredentials;

			try
			{
				byte[] bytes = Convert.FromBase64String(encodedCredentials);
				decodedCredentials = Encoding.UTF8.GetString(bytes);
			}
			catch
			{
				context.Response.StatusCode = 400;
				await context.Response.WriteAsync("Invalid Base64 encoding.");
				return;
			}

			// Split the decoded credentials into username and password
			string[] credentials = decodedCredentials.Split(':', 2);
			if (credentials.Length != 2)
			{
				context.Response.StatusCode = 400;
				await context.Response.WriteAsync("Invalid credentials format.");
				return;
			}

			// Compare
			if (_username != credentials[0] || _password != credentials[1])
			{
				context.Response.StatusCode = 401;
				context.Response.Headers["WWW-Authenticate"] = "Basic realm=\"Please authenticate yourself\", charset=\"UTF-8\"";
				await context.Response.WriteAsync("Invalid username or password.");
				return;
			}
			// Call next delegate/middleware in the pipeline
			await _next(context);
		}
    }
}
