using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.Google;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Mvc.Formatters;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.FileProviders;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using System.Text;
using System.Text.Json.Serialization;
using WebAPI_FlowerShopSWP.Controllers;
using WebAPI_FlowerShopSWP.Models;
using WebAPI_FlowerShopSWP.Repository;
using WebAPI_FlowerShopSWP.Configurations;
using Microsoft.Extensions.Options;
using WebAPI_FlowerShopSWP.Dto;
using WebAPI_FlowerShopSWP.DTO;
using WebAPI_FlowerShopSWP.Services;
using AutoMapper;
using WebAPI_FlowerShopSWP.Mappings;
using Microsoft.AspNetCore.Http.Features;
using System.IO;
using Microsoft.AspNetCore.Cors.Infrastructure;

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
            builder.Configuration.AddEnvironmentVariables();
            var secretKey = builder.Configuration["Jwt:SecretKey"];
            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKey ?? throw new InvalidOperationException("Jwt:SecretKey is not configured.")));
            builder.Services.AddMemoryCache();
            builder.Services.AddTransient<IEmailSender, EmailSender>();
            builder.Services.AddScoped<IChatService, ChatService>();
            builder.Services.AddScoped<ShippingController>();
            builder.Services.AddScoped<ICartService, CartService>();
            builder.Services.AddAutoMapper(cfg =>
            {
                cfg.CreateMap<CartItem, CartItemDto>();
                cfg.CreateMap<Cart, CartDto>();
            });
            builder.Services.Configure<FormOptions>(options =>
            {
                options.MultipartBodyLengthLimit = 10 * 1024 * 1024;
            });
            var uploadsDirectory = Path.Combine(builder.Environment.WebRootPath, "uploads");
            if (!Directory.Exists(uploadsDirectory))
            {
                Directory.CreateDirectory(uploadsDirectory);
            }
            builder.Services.AddCors(options =>
            {
                options.AddPolicy("AllowSpecificOrigin",
                    builder =>
                    {
                        builder.WithOrigins("http://localhost:5173", "http://localhost:5174")
                               .AllowAnyHeader()
                               .AllowAnyMethod()
                               .AllowCredentials()
                               .SetIsOriginAllowed(_ => true)
                               .WithExposedHeaders("Authorization");
                    });
            });

            builder.Services.AddScoped<INotificationService, NotificationService>();
            builder.Services.AddControllers()
     .AddJsonOptions(options =>
     {
         options.JsonSerializerOptions.ReferenceHandler = null;
     });
            builder.Services.Configure<VNPayConfig>(builder.Configuration.GetSection("VNPay"));
            builder.Services.AddEndpointsApiExplorer();
            builder.Services.AddSingleton<VNPayConfig>(sp =>
            {
                return new VNPayConfig
                {
                    TmnCode = builder.Configuration["VNPayConfig:TmnCode"],
                    HashSecret = builder.Configuration["VNPayConfig:HashSecret"],
                    Url = builder.Configuration["VNPayConfig:Url"]
                };
            });
            builder.Services.AddSwaggerGen(c =>
            {
                c.SwaggerDoc("v1", new OpenApiInfo { Title = "Flower Shop API", Version = "v1" });

                c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
                {
                    In = ParameterLocation.Header,
                    Description = "Please enter 'Bearer' [space] and then your token",
                    Name = "Authorization",
                    Type = SecuritySchemeType.ApiKey
                });

                c.AddSecurityRequirement(new OpenApiSecurityRequirement
                {
                    {
                        new OpenApiSecurityScheme
                        {
                            Reference = new OpenApiReference
                            {
                                Type = ReferenceType.SecurityScheme,
                                Id = "Bearer"
                            }
                        },
                        new string[] {}
                    }
                });
            });
                
            builder.Services.AddDbContext<FlowerEventShopsContext>(options =>
                options.UseSqlServer(builder.Configuration.GetConnectionString("ConnectDB")));
            builder.Services.AddSignalR(options =>
            {
                options.EnableDetailedErrors = true;
            });
            builder.Services.Configure<FormOptions>(options =>
            {
                options.MultipartBodyLengthLimit = 5 * 1024 * 1024; // 5MB
            });
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
               };

               options.Events = new JwtBearerEvents
               {
                   OnMessageReceived = context =>
                   {
                       var accessToken = context.Request.Query["access_token"];
                       var path = context.HttpContext.Request.Path;
                       if (!string.IsNullOrEmpty(accessToken) && path.StartsWithSegments("/chathub"))
                       {
                           context.Token = accessToken;
                       }
                       return Task.CompletedTask;
                   }
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
            builder.Services.Configure<GhnApiSettings>(builder.Configuration.GetSection("GhnApiSettings"));

            builder.Services.AddHttpClient("GHNClient", (serviceProvider, client) =>
            {
                var ghnSettings = serviceProvider.GetRequiredService<IOptions<GhnApiSettings>>().Value;

                if (string.IsNullOrEmpty(ghnSettings.BaseAddress) || string.IsNullOrEmpty(ghnSettings.Token))
                {
                    throw new ArgumentException("GHN API configuration is missing.");
                }

                client.BaseAddress = new Uri(ghnSettings.BaseAddress);
                client.DefaultRequestHeaders.Add("Token", ghnSettings.Token);
            });
            builder.Services.AddScoped<ISellerFollowService, SellerFollowService>();
            builder.Services.AddAutoMapper(typeof(MappingProfile));
            builder.Services.AddScoped<INotificationService, NotificationService>();
            var app = builder.Build();
            app.Environment.WebRootPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot");

            if (app.Environment.IsDevelopment())
            {
                app.UseSwagger();
                app.UseSwaggerUI();
                app.UseDeveloperExceptionPage();
            }
            app.UseStaticFiles(new StaticFileOptions
            {
                FileProvider = new PhysicalFileProvider(uploadsDirectory),
                RequestPath = "/uploads"
            });
            app.UseHttpsRedirection();
            app.UseHttpsRedirection();
            app.UseStaticFiles();
            app.UseStaticFiles(new StaticFileOptions
            {
                FileProvider = new PhysicalFileProvider(
        Path.Combine(builder.Environment.ContentRootPath, "wwwroot", "chat-images")),
                RequestPath = "/chat-images"
            });
            app.UseCors("AllowSpecificOrigin");
            app.UseCors("AllowReactApp");

            app.UseRouting();

            app.UseAuthentication();
            app.UseAuthorization();

            app.MapControllers();

            app.MapHub<ChatHub>("/chathub");
            app.MapHub<NotificationHub>("/notificationHub");
            var uploadDirectory = Path.Combine(builder.Environment.WebRootPath, "chat-images");
            if (!Directory.Exists(uploadDirectory))
            {
                Directory.CreateDirectory(uploadDirectory);
            }

            app.UseStaticFiles();

            app.UseStaticFiles(new StaticFileOptions
            {
                FileProvider = new PhysicalFileProvider(uploadDirectory),
                RequestPath = "/chat-images"
            });
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