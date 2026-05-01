using System.Reflection;
using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using Serilog;
using TeacherDiary.Api.Hubs;
using TeacherDiary.Api.Middlewares;
using TeacherDiary.Application.Abstractions.Services;
using TeacherDiary.Infrastructure;
using TeacherDiary.Infrastructure.Auth;
using TeacherDiary.Infrastructure.Extensions;
using TeacherDiary.Infrastructure.Persistence;

namespace TeacherDiary.Api
{
    public class Program
    {
        public static async Task Main(string[] args)
        {
            Log.Logger = new LoggerConfiguration()
                .ReadFrom.Configuration(new ConfigurationBuilder()
                    .AddJsonFile("appsettings.json")
                    .Build())
                .CreateLogger();

            try
            {
                Log.Information("Starting TeacherDiary API");
                Log.Information("Step 1: Creating WebApplication builder");
                var builder = WebApplication.CreateBuilder(args);
                Log.Information("Step 2: Builder created");

                builder.Host.UseSerilog();

                builder.Services.AddSignalR();
                builder.Services.AddScoped<INotificationPusher, HubNotificationPusher>();

                builder.Services.AddControllers()
                    .AddJsonOptions(opt =>
                    {
                        opt.JsonSerializerOptions.Converters.Add(
                            new System.Text.Json.Serialization.JsonStringEnumConverter());
                    });
                builder.Services.AddEndpointsApiExplorer();

                builder.Services.AddInfrastructure(builder.Configuration);

                var jwt = builder.Configuration.GetSection("Jwt").Get<JwtOptions>()!;

                builder.Services
                    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
                    .AddJwtBearer(opt =>
                    {
                        opt.TokenValidationParameters = new TokenValidationParameters
                        {
                            ValidateIssuer = true,
                            ValidateAudience = true,
                            ValidateLifetime = true,
                            ValidateIssuerSigningKey = true,
                            ValidIssuer = jwt.Issuer,
                            ValidAudience = jwt.Audience,
                            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwt.SigningKey)),
                            ClockSkew = TimeSpan.FromSeconds(30)
                        };
                        // Allow SignalR to pass token via query string (WebSocket can't set headers)
                        opt.Events = new JwtBearerEvents
                        {
                            OnMessageReceived = context =>
                            {
                                var token = context.Request.Query["access_token"].ToString();
                                if (!string.IsNullOrEmpty(token) &&
                                    context.HttpContext.Request.Path.StartsWithSegments("/hubs"))
                                    context.Token = token;
                                return Task.CompletedTask;
                            }
                        };
                    });

                builder.Services.AddAuthorization();

                builder.Services.AddSwaggerGen(c =>
                {
                    var xmlFile = $"{Assembly.GetExecutingAssembly().GetName().Name}.xml";
                    var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
                    c.IncludeXmlComments(xmlPath);
                    c.SwaggerDoc("v1", new OpenApiInfo
                    {
                        Title = "TeacherDiary API",
                        Version = "v1",
                        Description =
                            "API for managing classes, students, reading activities, assignments and gamification.",
                        Contact = new OpenApiContact
                        {
                            Name = "TeacherDiary",
                            Email = "support@teacherdiary.com"
                        }
                    });

                    var securityScheme = new OpenApiSecurityScheme
                    {
                        Name = "Authorization",
                        Description = "Enter: Bearer {your JWT token}",
                        In = ParameterLocation.Header,
                        Type = SecuritySchemeType.Http,
                        Scheme = "bearer",
                        BearerFormat = "JWT",
                        Reference = new OpenApiReference { Type = ReferenceType.SecurityScheme, Id = "Bearer" }
                    };

                    c.AddSecurityDefinition("Bearer", securityScheme);
                    c.AddSecurityRequirement(new OpenApiSecurityRequirement
                    {
                        { securityScheme, new List<string>() }
                    });
                });

                var allowedCorsOrigins = builder.Configuration.GetSection("AllowedCorsOrigins").Get<string[]>() ?? [];

                builder.Services.AddCors(options =>
                {
                    options.AddPolicy("Frontend", policy =>
                    {
                        policy
                            .WithOrigins(allowedCorsOrigins)
                            .AllowAnyHeader()
                            .AllowAnyMethod()
                            .AllowCredentials();
                    });
                });

                Log.Information("Step 3: Building app");
                var app = builder.Build();
                Log.Information("Step 4: App built, configuring middleware");

                app.UseGlobalExceptionMiddleware();
                app.UseSerilogRequestLogging(options =>
                {
                    options.MessageTemplate =
                        "HTTP {RequestMethod} {RequestPath} responded {StatusCode} in {Elapsed:0.0000} ms";

                    options.EnrichDiagnosticContext = (diagnosticContext, httpContext) =>
                    {
                        diagnosticContext.Set("TraceId", httpContext.TraceIdentifier);
                        diagnosticContext.Set("RequestHost", httpContext.Request.Host.Value ?? string.Empty);
                        diagnosticContext.Set("RequestScheme", httpContext.Request.Scheme);
                        diagnosticContext.Set("UserAgent", httpContext.Request.Headers.UserAgent.ToString());
                        diagnosticContext.Set("RemoteIpAddress", httpContext.Connection.RemoteIpAddress?.ToString());
                    };
                });

                Log.Information("Step 5: Running database migrations");
                using (var scope = app.Services.CreateScope())
                {
                    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
                    await db.Database.MigrateAsync();
                }
                Log.Information("Step 6: Migrations done, seeding badges");

                await BadgeSeeder.SeedBadgesAsync(app.Services);
                Log.Information("Step 7: Badge seeding done, re-evaluating badges");

                await BadgeReEvaluator.ReEvaluateAllAsync(app.Services);
                Log.Information("Step 8: Badge re-evaluation done, starting Kestrel");

                app.UseSwagger();
                app.UseSwaggerUI(c =>
                {
                    c.DocumentTitle = "TeacherDiary API";

                    c.SwaggerEndpoint("/swagger/v1/swagger.json", "TeacherDiary API v1");

                    c.DefaultModelsExpandDepth(-1); // hide schemas section
                });

                app.UseStaticFiles();

                app.UseHttpsRedirection();

                app.UseAuthentication();
                app.UseAuthorization();

                app.UseCors("Frontend");

                app.MapControllers();
                app.MapHub<NotificationHub>("/hubs/notifications");

                app.Run();
            }
            catch (Exception ex)
            {
                Log.Fatal(ex, "Application terminated unexpectedly");
            }
            finally
            {
                Log.CloseAndFlush();
            }
        }
    }
}
