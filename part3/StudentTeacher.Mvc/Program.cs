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
            builder.Services.AddHttpClient("StudentTeacherApi") //registers a named HTTP client
                                                                //specifically for communicating with your API.
                .ConfigurePrimaryHttpMessageHandler(() => //configures how that client sends HTTP requests.
                    new HttpClientHandler
                    {
                        UseCookies = false //prevents the HTTP handler from automatically storing and reusing cookies.
                                           //This doesn't disable the API's cookie authentication.
                                           //Instead, the MVC controller must explicitly attach the cookie
                                           //belonging to the current session.
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

            app.UseHttpsRedirection();
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
