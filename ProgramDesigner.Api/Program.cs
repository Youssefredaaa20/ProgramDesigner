using Microsoft.EntityFrameworkCore;
using ProgramDesigner.Infrastructure;
using Scalar.AspNetCore;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllers();
builder.Services.AddScoped<ProgramDesigner.Application.Services.TreeBuilder>();
builder.Services.AddScoped<ProgramDesigner.Application.Services.IProgramService, ProgramDesigner.Application.Services.ProgramService>();
builder.Services.AddScoped<ProgramDesigner.Application.Services.IValidationService, ProgramDesigner.Application.Services.ValidationService>();

builder.Services.AddDbContext<ProgramDesignerDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

builder.Services.AddOpenApi();

var app = builder.Build();

app.MapControllers();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.MapScalarApiReference();
}

app.Run();
