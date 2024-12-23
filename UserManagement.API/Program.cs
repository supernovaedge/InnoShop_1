using Microsoft.EntityFrameworkCore;
using UserManagement.Application.Interfaces;
using UserManagement.Application.Services;
using UserManagement.Infrastructure.Data;
using UserManagement.API.Configurations;
using UserManagement.Core.Interfaces;
using UserManagement.Infrastructure.Repositories;
using Microsoft.OpenApi.Models;
using AutoMapper;
using UserManagement.Application.Mapping;
using UserManagement.Domain.Entities;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.UI.Services;
using UserManagement.Infrastructure.Identity;
using FluentValidation.AspNetCore;
using UserManagement.Application.Validators;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddControllers()
    .AddNewtonsoftJson()
    .AddFluentValidation(fv => fv.RegisterValidatorsFromAssemblyContaining<CreateUserDtoValidator>());

builder.Services.AddSingleton<IConfiguration>(builder.Configuration);
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo { Title = "User Management API", Version = "v1" });
    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        In = ParameterLocation.Header,
        Description = "Please enter a valid token",
        Name = "Authorization",
        Type = SecuritySchemeType.Http,
        BearerFormat = "JWT",
        Scheme = "Bearer"
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

builder.Services.AddScoped<IUserService, UserService>();
builder.Services.AddScoped<IUserRepository, UserRepository>();
builder.Services.AddDbContext<UserManagementDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

builder.Services.AddIdentitySetup();

builder.Services.AddAutoMapper(typeof(UserMappingProfile));
builder.Services.AddJwtConfiguration(builder.Configuration);

// Register the EmailSender service
builder.Services.AddTransient<IEmailSender>(sp => new EmailSender(
    builder.Configuration["EmailSettings:SmtpServer"],
    int.Parse(builder.Configuration["EmailSettings:SmtpPort"]),
    builder.Configuration["EmailSettings:SmtpUser"],
    builder.Configuration["EmailSettings:SmtpPass"]
));

// Register DataSeeder
builder.Services.AddTransient<DataSeeder>();

builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", builder =>
    {
        builder.AllowAnyOrigin()
               .AllowAnyMethod()
               .AllowAnyHeader();
    });
});

// Register HttpClient
builder.Services.AddHttpClient<IUserService, UserService>(client =>
{
    client.BaseAddress = new Uri("https://localhost:7153");
});

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseDeveloperExceptionPage();
    app.UseSwagger();
    app.UseSwaggerUI();
}
else
{
    app.UseExceptionHandler("/error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseCors("AllowAll");
app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Map("/error", (HttpContext httpContext) =>
{
    var feature = httpContext.Features.Get<IExceptionHandlerFeature>();
    var exception = feature?.Error;

    var problemDetails = new ProblemDetails
    {
        Status = StatusCodes.Status500InternalServerError,
        Title = "An error occurred while processing your request.",
        Detail = exception?.Message
    };

    return Results.Problem(problemDetails);
});

// Call the seeding method
using (var scope = app.Services.CreateScope())
{
    var dataSeeder = scope.ServiceProvider.GetRequiredService<DataSeeder>();
    await dataSeeder.SeedAsync();
}

app.Run();





