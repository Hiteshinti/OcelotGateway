using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using Ocelot.DependencyInjection;
using Ocelot.Middleware;
using System.Text;

var builder = WebApplication.CreateBuilder(args);

// ✅ CORS
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", policy =>
    {
        policy.WithOrigins("http://localhost:4200")
              .AllowAnyMethod()
              .AllowAnyHeader()
              .AllowCredentials();
    });
});

// ✅ Load Ocelot config
builder.Configuration.AddJsonFile("ocelot.json", optional: false, reloadOnChange: true);

// ✅ Add Ocelot
builder.Services.AddOcelot();

// 🔴 IMPORTANT FIX (was missing)
builder.Services.AddAuthorization();

// ✅ JWT config
var key = builder.Configuration["Jwt:Key"];
var issuer = builder.Configuration["Jwt:Issuer"];
var audiences = builder.Configuration.GetSection("Jwt:Audience")
                          .GetChildren()
                          .Select(x => x.Value)
                          .ToArray();

// 🔴 Safety check (prevents runtime crash)
if (string.IsNullOrEmpty(key))
{
    throw new Exception("JWT Key is missing in configuration");
}

// ✅ Authentication
builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;

})
.AddJwtBearer(options =>
{
    options.RequireHttpsMetadata = false; // 🔁 true in production
    options.SaveToken = true;

    options.TokenValidationParameters = new TokenValidationParameters()
    {
        ValidateIssuer = true,
        ValidateAudience = true,
        ValidateLifetime = true,
        ValidateIssuerSigningKey = true,

        ValidIssuer = issuer,
        ValidAudiences = audiences,

        IssuerSigningKey = new SymmetricSecurityKey(
            Encoding.UTF8.GetBytes(key)
        ),

        ClockSkew = TimeSpan.Zero // ✅ avoids token expiry delays
    };

    // ✅ Debugging (optional but useful)
    options.Events = new JwtBearerEvents
    {
        OnAuthenticationFailed = context =>
        {
            Console.WriteLine("Auth failed: " + context.Exception.Message);
            return Task.CompletedTask;
        },
        OnTokenValidated = context =>
        {
            Console.WriteLine("Token validated");
            return Task.CompletedTask;
        }
    };
});

// ✅ Set port
builder.WebHost.UseUrls("http://*:8000");

var app = builder.Build();

// ✅ Middleware order
app.UseCors("AllowAll");

app.UseAuthentication();
app.UseAuthorization();

// ✅ Ocelot LAST
await app.UseOcelot();

app.MapGet("/", () => "Gateway running...");

app.Run();