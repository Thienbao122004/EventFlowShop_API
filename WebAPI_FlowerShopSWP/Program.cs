using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.Google;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.FileProviders;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using System.Text;
using WebAPI_FlowerShopSWP.Models;
using WebAPI_FlowerShopSWP.Repository;

namespace WebAPI_FlowerShopSWP
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var wwwrootPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot");
            if (!Directory.Exists(wwwrootPath))
            {
                Directory.CreateDirectory(wwwrootPath);
            }
            var imagesPath = Path.Combine(wwwrootPath, "images");
            if (!Directory.Exists(imagesPath))
            {
                Directory.CreateDirectory(imagesPath);
            }
            var builder = WebApplication.CreateBuilder(args);

            var secretKey = builder.Configuration["Jwt:SecretKey"];
            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKey ?? throw new InvalidOperationException("Jwt:SecretKey is not configured.")));

            // Configure
            
            // 
            //Add Email
            builder.Services.AddTransient<IEmailSender, EmailSender>();

            builder.Services.AddCors(options =>
            {
                options.AddPolicy("AllowSpecificOrigin",
                    builder =>
                    {
                        builder.WithOrigins("http://localhost:5173", "http://localhost:5174") // Your frontend URL
                               .AllowAnyHeader()
                               .AllowAnyMethod()
                               .AllowCredentials()
                               .SetIsOriginAllowed(_ => true)
                               .WithExposedHeaders("Authorization");
                    });
            });

            builder.Services.AddControllers();
            builder.Services.AddEndpointsApiExplorer();
            builder.Services.AddSwaggerGen();

            // Configure database context
            builder.Services.AddDbContext<FlowerEventShopsContext>(options =>
                options.UseSqlServer(builder.Configuration.GetConnectionString("ConnectDB")));

            // Configure authentication schemes
            builder.Services.AddAuthentication(options =>
            {
                options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
                options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
                options.DefaultScheme = CookieAuthenticationDefaults.AuthenticationScheme;
            })
            .AddCookie(options =>
            {
                options.LoginPath = "/api/LoginGoogle/login-google";
                options.ExpireTimeSpan = TimeSpan.FromMinutes(50);
            })
            .AddJwtBearer(options =>
            {
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuerSigningKey = true,
                    IssuerSigningKey = key,
                    ValidateIssuer = false,
                    ValidateAudience = false,
                    ValidateLifetime = true,
                    ClockSkew = TimeSpan.Zero,
                    RoleClaimType = "UserType" // Add this line to use UserType as the role claim
                };
            })
            .AddGoogle(googleOptions =>
            {
                googleOptions.ClientId = builder.Configuration["Authentication:Google:ClientId"];
                googleOptions.ClientSecret = builder.Configuration["Authentication:Google:ClientSecret"];
                googleOptions.CallbackPath = "/api/LoginGoogle/google-callback";
            });

            // Configure authorization policies
            builder.Services.AddAuthorization(options =>
            {
                options.AddPolicy("AdminOnly", policy => policy.RequireRole("Admin"));
                options.AddPolicy("BuyerSellerOnly", policy => policy.RequireRole("Buyer", "Seller"));
            });
            builder.Services.AddSingleton<IWebHostEnvironment>(builder.Environment);


            var app = builder.Build();
            app.Environment.WebRootPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot");

            if (app.Environment.IsDevelopment())
            {
                app.UseSwagger();
                app.UseSwaggerUI();
                app.UseDeveloperExceptionPage();
            }

            app.UseHttpsRedirection();
            app.UseStaticFiles();

            // Use CORS before routing and authorization
            app.UseCors("AllowSpecificOrigin");

            app.UseRouting();

            app.UseAuthentication();
            app.UseAuthorization();

            app.MapControllers();
            app.UseStaticFiles(new StaticFileOptions
            {
                FileProvider = new PhysicalFileProvider(
        Path.Combine(Directory.GetCurrentDirectory(), "wwwroot")),
                RequestPath = ""
            });
            app.Run();
        }
    }
}