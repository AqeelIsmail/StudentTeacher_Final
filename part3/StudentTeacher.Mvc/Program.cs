namespace StudentTeacher.Mvc
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            // Add services to the container.
            builder.Services.AddControllersWithViews();

            // Configure an HTTP client for communication with the Part 2 API.
            // Cookies will be managed explicitly through MVC session.
            // Configure an HTTP client for communication with the Part 2 API.
            // Cookies will be managed explicitly through MVC session.
            builder.Services.AddHttpClient("StudentTeacherApi", client =>
            {
                // Registers a named HTTP client specifically for communicating
                // with your API.

                // Read the API address from configuration.
                // Locally, this comes from appsettings.json.
                // Docker can override it using an environment variable.
                string baseUrl =
                    builder.Configuration["ApiSettings:BaseUrl"]
                    ?? throw new InvalidOperationException(
                        "ApiSettings:BaseUrl is missing.");

                // Set the base address used when sending requests to the API.
                client.BaseAddress = new Uri(baseUrl);
            })
            .ConfigurePrimaryHttpMessageHandler(() =>
                // Configures how this client sends HTTP requests.
                new HttpClientHandler
                {
                    // Prevents the HTTP handler from automatically storing
                    // and reusing cookies.
                    // This doesn't disable the API's cookie authentication.
                    // Instead, the MVC controller must explicitly attach
                    // the cookie belonging to the current session.
                    UseCookies = false
                });

            builder.Services.AddDistributedMemoryCache();
            builder.Services.AddSession();

            var app = builder.Build();
            app.UseStaticFiles();

            // Configure the HTTP request pipeline.
            if (!app.Environment.IsDevelopment())
            {
                app.UseExceptionHandler("/Home/Error");
                // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
                app.UseHsts();
            }

            // Keep HTTPS redirection for normal use.
            // Disable it only when DockerLocal is enabled
            // for our local HTTP Docker practical.
            if (!app.Configuration.GetValue<bool>("DockerLocal"))
            {
                app.UseHttpsRedirection();
            }

            app.UseRouting();
            app.UseSession();

            app.UseAuthorization();

            app.MapStaticAssets();
            app.MapControllerRoute(
                name: "default",
                pattern: "{controller=Account}/{action=Index}/{id?}")
                .WithStaticAssets();

            app.Run();
        }
    }
}
