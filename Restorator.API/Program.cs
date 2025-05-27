using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Localization;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using Restorator.API.Infrastructure;
using Restorator.API.Services;
using Restorator.Application.Services;
using Restorator.Application.Services.Abstract;
using Restorator.DataAccess.Data;
using Restorator.Domain.Services;
using Restorator.Mail.Configuration;
using Restorator.Mail.Services;
using Restorator.Seeder.Extensions;
using System.Globalization;
using System.Reflection;
using System.Text;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.MapType<DateOnly>(() => new OpenApiSchema
    {
        Type = "string",
        Format = "date"
    });

    c.SwaggerDoc("v1", new OpenApiInfo { Title = "Restorator.API", Version = "v1" });
    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Description = "JWT ��������� ����������� ���������� Bearer �����." +
        "\r\n\r\n������� 'Bearer' [������] � ����� ���� ����� � ���� ����." +
        "\r\n\r\n��������: 'Bearer 12345abcdef'",
        Name = "Authorization",
        In = ParameterLocation.Header,
        Type = SecuritySchemeType.ApiKey,
        Scheme = JwtBearerDefaults.AuthenticationScheme
    });

    c.AddSecurityRequirement(new OpenApiSecurityRequirement()
      {
        {
          new OpenApiSecurityScheme
          {
            Reference = new OpenApiReference
              {
                Type = ReferenceType.SecurityScheme,
                Id = "Bearer"
              },
              Scheme = "oauth2",
              Name = "Bearer",
              In = ParameterLocation.Header,

            },[]
          }
        });

    var xmlPath = Path.Combine(AppContext.BaseDirectory, $"{Assembly.GetExecutingAssembly().GetName().Name}.xml");

    c.IncludeXmlComments(xmlPath);
});

builder.Services.Configure<RouteOptions>(options => options.LowercaseUrls = true);

builder.Services.AddSingleton(builder.Configuration.GetRequiredSection("SmtpConfiguration")
                                                   .Get<SmtpConfiguration>()!);

CultureInfo.DefaultThreadCurrentCulture = new CultureInfo("ru-RU");
CultureInfo.DefaultThreadCurrentUICulture = new CultureInfo("ru-RU");

builder.Services.AddAuthentication(x =>
{
    x.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    x.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.RequireHttpsMetadata = false; options.TokenValidationParameters = new TokenValidationParameters()
    {
        ValidateIssuerSigningKey = true,
        ValidateAudience = false,
        ValidateIssuer = false,
        ValidateLifetime = true,
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(builder.Configuration["Jwt:Key"]!)),
    };
});

builder.Services.AddDbContext<RestoratorDbContext>(opt =>
{
    opt.UseSqlServer(builder.Configuration.GetConnectionString("LocalConnection"));
});

builder.Services.AddScoped<IJwtService, JwtService>();
builder.Services.AddScoped<IAccountService, AccountService>();
builder.Services.AddScoped<IReservationService, ReservationService>();
builder.Services.AddScoped<IRestaurantService, RestaurantService>();
builder.Services.AddScoped<ITemplateService, TemplateService>();
builder.Services.AddScoped<IReportService, ReportService>();

builder.Services.AddScoped<IUserManager, UserManager>();
builder.Services.AddScoped<IMailService, MailService>();

builder.Services.AddScoped<IRestaurantFilesManager, RestaurantFilesManager>();
builder.Services.AddScoped<IRestaurantTemplateFilesManager, RestaurantTemplateFilesManager>();

builder.Services.AddScoped<MailTemplateBuilder>();

builder.Services.AddSeeder();

builder.Services.AddHttpContextAccessor();

builder.Services.AddAuthorization();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseStaticFiles();

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

await app.RunAsync();
