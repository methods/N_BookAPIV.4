using BookApi.NET.Services;
using BookApi.NET.Models;
using BookApi.NET.Middleware;
using MongoDB.Driver;
using Microsoft.Extensions.Options;
using Microsoft.AspNetCore.Authentication.Cookies;
using System.Security.Claims;
using BookApi.NET.Common;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddAuthentication(Options =>
    {
        Options.DefaultAuthenticateScheme = CookieAuthenticationDefaults.AuthenticationScheme;
        Options.DefaultSignInScheme = CookieAuthenticationDefaults.AuthenticationScheme;
        Options.DefaultChallengeScheme = CookieAuthenticationDefaults.AuthenticationScheme;
    })
    .AddCookie(Options =>
    {
        Options.Events.OnRedirectToLogin = context =>
        {
            context.Response.StatusCode = StatusCodes.Status401Unauthorized;
            return Task.CompletedTask;
        };
    })
    .AddGoogle(Options =>
    {
        Options.ClientId = builder.Configuration["Google:ClientId"]!;
        Options.ClientSecret = builder.Configuration["Google:ClientSecret"]!;
        Options.Events.OnCreatingTicket = async context =>
        {
            var claimsPrincipal = context.Principal;
            if (claimsPrincipal is null)
            {
                return;
            }
            var userService = context.HttpContext.RequestServices.GetRequiredService<UserService>();
            var user = await userService.FindOrCreateUserAsync(claimsPrincipal);

            var claimsIdentity = (ClaimsIdentity)claimsPrincipal.Identity!;
            claimsIdentity.AddClaim(new Claim(CustomClaimTypes.InternalUserId, user.Id.ToString()));
            claimsIdentity.AddClaim(new Claim(ClaimTypes.Role, user.Role));
        };
    });
builder.Services.AddAuthorization();
builder.Services.AddHttpContextAccessor();
builder.Services.AddControllers()
    .AddApplicationPart(typeof(Program).Assembly) 
    .AddNewtonsoftJson();
builder.Services.Configure<BookstoreDbSettings>(
    builder.Configuration.GetSection("BookstoreDbSettings"));
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddSingleton<IMongoClient>(sp =>
    new MongoClient(sp.GetRequiredService<IOptions<BookstoreDbSettings>>().Value.ConnectionString));
builder.Services.AddSingleton<IBookRepository, BookRepository>();
builder.Services.AddSingleton<IReservationRepository, ReservationRepository>();
builder.Services.AddSingleton<IUserRepository, UserRepository>();
builder.Services.AddScoped<ReservationService>();
builder.Services.AddScoped<ReservationMapper>();
builder.Services.AddScoped<BookService>();
builder.Services.AddScoped<BookMapper>();
builder.Services.AddScoped<UserService>();

// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.UseAuthentication();
app.UseAuthorization();

app.UseMiddleware<ExceptionHandlerMiddleware>();

app.MapControllers();

app.Run();

public partial class Program { }