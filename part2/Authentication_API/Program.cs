using Authentication_API.Services;
using Authentication_API.Models;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace Authentication_API
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            builder.Services.AddControllers();

            builder.Services.AddDbContext<AppDbContext>(options =>
                options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

            // Register the Azure Blob Storage service for dependency injection.
            builder.Services.AddScoped<BlobStorageService>();   

            builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
                .AddCookie();

            builder.Services.AddAuthorization();

            builder.Services.AddEndpointsApiExplorer();
            builder.Services.AddSwaggerGen(options => //Configure Swagger to read the XML documentation
            {
                var xmlFile = $"{typeof(Program).Assembly.GetName().Name}.xml";

                var xmlPath = Path.Combine(
                    AppContext.BaseDirectory,
                    xmlFile
                );
                options.IncludeXmlComments(xmlPath);
            });


            var app = builder.Build();

            if (app.Environment.IsDevelopment())
            {
                app.UseSwagger();
                app.UseSwaggerUI();
            }
            //2 guards set up that check at every request before it reaches controllers
            app.UseAuthentication();//Who is this person? 
            app.UseAuthorization(); //What can this person do
            app.MapControllers();
            app.Run();
        }
    }
}
