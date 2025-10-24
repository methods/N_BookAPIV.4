using BookApi.NET.Services;
using BookApi.NET.Models;
using BookApi.NET.Middleware;
using MongoDB.Driver;
using Microsoft.Extensions.Options;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
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
builder.Services.AddScoped<ReservationService>();
builder.Services.AddScoped<ReservationMapper>();
builder.Services.AddScoped<BookService>();
builder.Services.AddScoped<BookMapper>();

// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

if (!app.Environment.IsDevelopment())
{
    app.UseHttpsRedirection();
}

app.UseMiddleware<ExceptionHandlerMiddleware>();

app.MapControllers();

app.Run();

public partial class Program { }