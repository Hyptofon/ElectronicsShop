using System.Text;
using Api.Filters;
using Api.Services;
using Application.Common.Interfaces;
using FluentValidation;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;

namespace Api.Modules;

public static class SetupModule
{
    public static void SetupServices(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddControllers(options => 
        { 
            options.Filters.Add<ValidationFilter>();
            options.Filters.Add<ValidationExceptionFilter>(); 
        });
        services.AddCors();
        services.AddRequestValidators();
        services.AddAuthenticationServices(configuration);
        services.AddApplicationServices();
        services.AddEndpointsApiExplorer();
        services.AddSwaggerGenConfiguration();
    }

    private static void AddCors(this IServiceCollection services)
    {
        services.AddCors(options =>
            options.AddDefaultPolicy(policy =>
                policy.SetIsOriginAllowed(_ => true)
                    .AllowAnyHeader()
                    .AllowAnyMethod()
                    .AllowCredentials()));
    }

    private static void AddRequestValidators(this IServiceCollection services)
    {
        services.AddValidatorsFromAssemblyContaining<Program>();
    }

    private static void AddAuthenticationServices(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddAuthentication(options =>
            {
                options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
                options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
                options.DefaultScheme = JwtBearerDefaults.AuthenticationScheme;
            })
            .AddJwtBearer(options =>
            {
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidateAudience = true,
                    ValidateLifetime = true,
                    ValidateIssuerSigningKey = true,
                    ValidIssuer = configuration["JwtSettings:Issuer"],
                    ValidAudience = configuration["JwtSettings:Audience"],
                    IssuerSigningKey = new SymmetricSecurityKey(
                        Encoding.UTF8.GetBytes(configuration["JwtSettings:SecretKey"]!)),
                };
            });

        services.AddAuthorization();

        services.ConfigureApplicationCookie(options =>
        {
            options.Events.OnRedirectToLogin = context =>
            {
                context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                return Task.CompletedTask;
            };
            options.Events.OnRedirectToAccessDenied = context =>
            {
                context.Response.StatusCode = StatusCodes.Status403Forbidden;
                return Task.CompletedTask;
            };
        });
    }

    private static void AddApplicationServices(this IServiceCollection services)
    {
        services.AddHttpContextAccessor();
        services.AddScoped<ICurrentUserService, CurrentUserService>();
    }

    private static void AddSwaggerGenConfiguration(this IServiceCollection services)
    {
        services.AddSwaggerGen(c =>
        {
            c.SwaggerDoc("v1", new OpenApiInfo
            {
                Title = "Electronics Shop API",
                Version = "v1",
                Description = "REST API для інтернет-магазину електроніки"
            });

            c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
            {
                Description =
                    "JWT Authorization header using the Bearer scheme. Enter 'Bearer' [space] and then your token",
                Name = "Authorization",
                In = ParameterLocation.Header,
                Type = SecuritySchemeType.ApiKey,
                Scheme = "Bearer"
            });

            c.AddSecurityRequirement(new OpenApiSecurityRequirement
            {
                {
                    new OpenApiSecurityScheme
                    {
                        Reference = new OpenApiReference { Type = ReferenceType.SecurityScheme, Id = "Bearer" }
                    },
                    Array.Empty<string>()
                }
            });
        });
    }
}