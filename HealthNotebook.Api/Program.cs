using System.Text;
using HealthNotebook.Authentication.Configuration;
using HealthNotebook.DataService.Data;
using HealthNotebook.DataService.IConfiguration;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;

var builder = WebApplication.CreateBuilder(args);

// Adding controller support
builder.Services.AddControllers();

// Update the JWT config from the settings
builder.Services.Configure<JwtConfig>(builder.Configuration.GetSection("JwtConfig"));

// Add DbContext
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlite(builder.Configuration.GetConnectionString("DefaultConnection")));

// Adding dependency injection for UnitOfWork
builder.Services.AddScoped<IUnitOfWork, UnitOfWork>();

// Adding API versioning
builder.Services.AddApiVersioning(opt =>
            {
                // Provides to the client the differant Api versions that we have
                opt.ReportApiVersions = true;

                // this will allow the api to automatically provide a default version
                opt.AssumeDefaultVersionWhenUnspecified = true;

                opt.DefaultApiVersion = ApiVersion.Default;
            });

// Getting the secret from the config
var key = Encoding.ASCII.GetBytes(builder.Configuration["JwtConfig:Secret"]);

var tokenValidationParameters = new TokenValidationParameters
{
    ValidateIssuerSigningKey = true,
    IssuerSigningKey = new SymmetricSecurityKey(key),
    ValidateIssuer = false, // ToDo Update
    ValidateAudience = false, // ToDo Update
    RequireExpirationTime = false, // ToDo Update
    ValidateLifetime = true
};

// Injecting into our Di container
builder.Services.AddSingleton(tokenValidationParameters);

builder.Services.AddAuthentication(option =>
{
    option.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    option.DefaultScheme = JwtBearerDefaults.AuthenticationScheme;
    option.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(jwt =>
{
    jwt.SaveToken = true;
    jwt.TokenValidationParameters = tokenValidationParameters;
});

builder.Services.AddDefaultIdentity<IdentityUser>(options
    => options.SignIn.RequireConfirmedAccount = true)
.AddEntityFrameworkStores<AppDbContext>();

// Add swagger
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Builds the web application
var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseDeveloperExceptionPage();
    app.UseSwagger();
    app.UseSwaggerUI(c => c.SwaggerEndpoint("/swagger/v1/swagger.json", "HealthNotebook.Api v1"));
}

app.UseHttpsRedirection();

app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();