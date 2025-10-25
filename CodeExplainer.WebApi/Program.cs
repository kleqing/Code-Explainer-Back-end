using System.Reflection;
using System.Text;
using System.Text.Json;
using CodeExplainer.BusinessObject;
using CodeExplainer.Services.Implements;
using CodeExplainer.Services.Interfaces;
using CodeExplainer.Shared.Jwt;
using CodeExplainer.Shared.Utils;
using CodeExplainer.WebApi.Hubs;
using dotenv.net;
using MaIN.Core;
using MaIN.Domain.Configuration;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using StackExchange.Redis;

namespace CodeExplainer.WebApi;

public class Program
{
    public static void Main(string[] args)
    {
        DotEnv.Load();
        var builder = WebApplication.CreateBuilder(args);
        
        var corsPolicy = "AllowSpecificOrigins"; 
        
        var allowedOrigins = builder.Configuration.GetSection("CorsSettings:AllowedOrigins").Get<string[]>();
        if (allowedOrigins == null || allowedOrigins.Length == 0)
        {
            var originsFromEnv = Environment.GetEnvironmentVariable("CORS_ALLOWED_ORIGINS");
            if (!string.IsNullOrEmpty(originsFromEnv))
            {
                allowedOrigins = originsFromEnv.Split(',', StringSplitOptions.RemoveEmptyEntries);
            }
        }
        
        var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");

        if (string.IsNullOrWhiteSpace(connectionString))
        {
            connectionString = Environment.GetEnvironmentVariable("SQL_CONNECTION_STRING");
            if (string.IsNullOrWhiteSpace(connectionString))
            {
                throw new InvalidOperationException("Database connection string is not set in environment variables.");
            }
        }
        
        var openAiApiKey = Environment.GetEnvironmentVariable("OPENAI_API_KEY");
        if (string.IsNullOrWhiteSpace(openAiApiKey))
        {
            openAiApiKey = builder.Configuration["OpenAI:ApiKey"];
            if (string.IsNullOrWhiteSpace(openAiApiKey))
            {
                throw new InvalidOperationException("OpenAI API key is not set in environment variables.");
            }
        }

        builder.Services.AddDbContext<ApplicationDbContext>(options
            => options.UseNpgsql(connectionString));
        
        builder.Services.Configure<Jwt>(options =>
        {
            options.Secret = Environment.GetEnvironmentVariable("JWT_SECRET") ?? builder.Configuration["Jwt:Secret"] ?? string.Empty;
            options.Issuer = Environment.GetEnvironmentVariable("JWT_ISSUER") ?? builder.Configuration["Jwt:Issuer"] ?? string.Empty;
            options.Audience = Environment.GetEnvironmentVariable("JWT_AUDIENCE") ?? builder.Configuration["Jwt:Audience"] ?? string.Empty;
            options.ExpiryInMinutes = int.Parse(Environment.GetEnvironmentVariable("JWT_EXPIRY_MINUTES") ?? builder.Configuration["Jwt:ExpiryInMinutes"] ?? "60");
        });
        
        builder.Services.Configure<RouteOptions>(options => options.LowercaseUrls = true);
        
        builder.Services.AddCors(options => 
        {
            options.AddPolicy(name: corsPolicy,
                policy =>
                {
                    if (allowedOrigins != null && allowedOrigins.Length > 0)
                    {
                        policy.WithOrigins(allowedOrigins)
                            .AllowAnyHeader()
                            .AllowAnyMethod()
                            .AllowCredentials();
                    }
                    else
                    {
                        policy.AllowAnyOrigin()
                            .AllowAnyHeader()
                            .AllowAnyMethod();
                    }
                });
        });
        
        var currentAssembly  = typeof(Program).Assembly;
        var referencedAssemblies = currentAssembly .GetReferencedAssemblies()
            .Where(x => x.Name!.StartsWith("CodeExplainer") && x.Name != null); //! Replace "YourApp" with your actual project prefix
        
        var assemblies = referencedAssemblies
            .Select(Assembly.Load)
            .Append(currentAssembly);

        foreach (var type in assemblies.SelectMany(a => a.GetTypes()))
        {
            if (type.IsClass && !type.IsAbstract)
            {
                foreach (var iface in type.GetInterfaces())
                {
                    if (iface.Name == $"I{type.Name}")
                    {
                        builder.Services.AddScoped(iface, type);
                    }
                }
            }
        }
        
        builder.Services.AddTransient<IEmailSender, EmailSender>();
        builder.Services.AddTransient<CloudinaryUploader>();
        // Add services to the container.

        builder.Services.AddControllers()
            .AddJsonOptions(options =>
            {
                options.JsonSerializerOptions.PropertyNamingPolicy =
                    JsonNamingPolicy.CamelCase; //* Use original property names
                options.JsonSerializerOptions.PropertyNameCaseInsensitive =
                    true; //* Enable case-insensitive property names
            });
        
        // Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
        builder.Services.AddEndpointsApiExplorer();
        builder.Services.AddSwaggerGen(c =>
        {
            c.SwaggerDoc("v1", new() { Title = "Code Explainer", Version = "v1", Description = "An API endpoint to response code for user" });
            
            //* Add JWT Authentication to Swagger
            c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
            {
                In = ParameterLocation.Header,
                Description = "Please enter a valid token",
                Name = "Authorization",
                Type = SecuritySchemeType.Http,
                BearerFormat = "JWT",
                Scheme = "bearer"
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
                    new string[]{}
                }
            });
        });
        
        builder.Services.AddAuthentication(options =>
        {
            options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
            options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
            options.DefaultSignInScheme = "External";
        })
        .AddJwtBearer("Bearer", _ => { })
        .AddCookie("External", options =>
        {
            options.Events.OnRedirectToLogin = context =>
            {
                context.Response.StatusCode = 401; // Unauthorized
                return Task.CompletedTask;
            };
            options.Events.OnRedirectToAccessDenied = context =>
            {
                context.Response.StatusCode = 403; // Forbidden
                return Task.CompletedTask;
            };
            options.ExpireTimeSpan = TimeSpan.FromMinutes(5);
        })
        .AddGoogle(options =>
        {
            var clientId = Environment.GetEnvironmentVariable("GOOGLE_CLIENT_ID") ?? builder.Configuration["Authentication:Google:ClientId"] ?? string.Empty;
            var clientSecret = Environment.GetEnvironmentVariable("GOOGLE_CLIENT_SECRET") ?? builder.Configuration["Authentication:Google:ClientSecret"] ?? string.Empty;
            options.ClientId = clientId;
            options.ClientSecret = clientSecret;
            options.SaveTokens = true;
            options.ClaimActions.MapJsonKey("picture", "picture");
            options.CallbackPath = "/signin-google";
        });
        
        builder.Services.PostConfigure<JwtBearerOptions>("Bearer", options =>
        {
            var issuer = Environment.GetEnvironmentVariable("JWT_ISSUER") ?? builder.Configuration["Jwt:Issuer"] ?? string.Empty;
            var audience = Environment.GetEnvironmentVariable("JWT_AUDIENCE") ?? builder.Configuration["Jwt:Audience"] ?? string.Empty;
            var secret = Environment.GetEnvironmentVariable("JWT_SECRET") ?? builder.Configuration["Jwt:Secret"] ?? string.Empty;

            options.TokenValidationParameters = new TokenValidationParameters
            {
                ValidateIssuer = true,
                ValidateAudience = true,
                ValidateLifetime = true,
                ValidateIssuerSigningKey = true,
                ValidIssuer = issuer,
                ValidAudience = audience,
                IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secret)),
                ClockSkew = TimeSpan.Zero, //* Disable the default 5-minute clock skew
                RequireExpirationTime = true //* Require the token to have an expiration time
            };
            
            options.Events = new JwtBearerEvents
            {
                OnMessageReceived = context =>
                {
                    var accessToken = context.Request.Cookies["ACCESS_TOKEN"];
                    if (!string.IsNullOrEmpty(accessToken))
                    {
                        context.Token = accessToken;
                    }
                    return Task.CompletedTask;
                },
                OnAuthenticationFailed = context =>
                {
                    var logger = context.HttpContext.RequestServices.GetRequiredService<ILogger<Program>>();
                    logger.LogError(context.Exception, "Authentication failed.");
                    return Task.CompletedTask;
                },
                OnTokenValidated = _ =>
                {
                    // You can add additional validation here if needed
                    return Task.CompletedTask;
                },
                OnChallenge = context =>
                {
                    // Skip the default logic.
                    context.HandleResponse();
                    context.Response.StatusCode = 401;
                    context.Response.ContentType = "application/json";
                    var result = JsonSerializer.Serialize(new { error = "You are not authorized" });
                    return context.Response.WriteAsync(result);
                }
            };
        });
        
        builder.Services.AddAuthorization();
        builder.Services.AddHttpContextAccessor();
        
        builder.Services.AddSingleton<IConnectionMultiplexer>(_ =>
        {
            var redisConnectionString = Environment.GetEnvironmentVariable("REDIS_CONNECTION_STRING");

            if (string.IsNullOrEmpty(redisConnectionString))
            {
                redisConnectionString = builder.Configuration.GetValue<string>("Redis:ConnectionString");
                if (string.IsNullOrEmpty(redisConnectionString))
                {
                    throw new InvalidOperationException("Redis connection string is not set in environment variables or configuration.");
                }
            }

            var configurationOptions = ConfigurationOptions.Parse(redisConnectionString, true);
            configurationOptions.AbortOnConnectFail = false;

            try
            {
                return ConnectionMultiplexer.Connect(configurationOptions);
            }
            catch (RedisConnectionException ex)
            {
                throw new InvalidOperationException("Failed to connect to Redis: " + ex.Message, ex);
            }
        });
        
        builder.Services.AddScoped<IDatabase>(sp =>
        {
            var connectionMultiplexer = sp.GetRequiredService<IConnectionMultiplexer>();
            return connectionMultiplexer.GetDatabase();
        });
        
        MaINBootstrapper.Initialize(null, options =>
        {
            var backend = Environment.GetEnvironmentVariable("AI_BACKEND") ?? "Gemini";
            options.BackendType = backend switch
            {
                "OpenAI" => BackendType.OpenAi,
                "Claude" => BackendType.DeepSeek,
                _ => BackendType.Gemini
            };
            //options.GeminiKey = Environment.GetEnvironmentVariable("GEMINI_API_KEY");
            options.GeminiKey = Environment.GetEnvironmentVariable("OPENAI_API_KEY");
        });

        builder.Services.AddSignalR();
        
        var app = builder.Build();

        // Configure the HTTP request pipeline.
        if (app.Environment.IsDevelopment() || builder.Configuration.GetValue<bool>("EnableSwagger"))
        {
            app.UseSwagger();
            app.UseSwaggerUI();
        }
        else
        {
            // Custom error handling endpoint
            app.UseExceptionHandler(appError =>
            {
                appError.Run(async context =>
                {
                    var logger = app.Services.GetRequiredService<ILogger<Program>>();
                    var exceptionHandlerPathFeature =
                        context.Features.Get<IExceptionHandlerFeature>();

                    if (exceptionHandlerPathFeature?.Error != null)
                    {
                        logger.LogError(exceptionHandlerPathFeature.Error, "An unhandled exception has occurred.");
                    }

                    context.Response.StatusCode = StatusCodes.Status500InternalServerError;
                    context.Response.ContentType = "application/json";
                    await context.Response.WriteAsync(JsonSerializer.Serialize(new
                        { error = "Internal Server Error" }));
                });
            });
            app.UseHsts();
        }

        app.UseHttpsRedirection();

        app.UseRouting();
        app.UseCors(corsPolicy);

        app.UseAuthentication();
        app.UseAuthorization();
        
        app.MapControllers();

        app.MapHub<ChatHub>("/hubs/chat");
        app.MapHub<NotificationHub>("/hubs/notification");

        app.Run();
    }
}