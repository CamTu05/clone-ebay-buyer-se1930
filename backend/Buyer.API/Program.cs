using Buyer.API.Filters;
using Microsoft.EntityFrameworkCore;
using Buyer.Application.Catalog;
using Buyer.Application.Interfaces.Addresses;
using System.IdentityModel.Tokens.Jwt;
using System.Text;
using System.Threading.RateLimiting;
using Buyer.API.Controllers;
using Buyer.API.Middlewares;
using Buyer.Infrastructure;
using Buyer.Infrastructure.Authentication;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Mvc;
using Microsoft.OpenApi.Models;
using Microsoft.IdentityModel.Tokens;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers().ConfigureApiBehaviorOptions(options =>
{
    options.InvalidModelStateResponseFactory = context =>
    {
        var errors = context.ModelState
            .Where(entry => entry.Value?.Errors.Count > 0)
            .SelectMany(entry => entry.Value!.Errors.Select(_ =>
                new ApiError("VALIDATION_ERROR", entry.Key)))
            .ToArray();
        return new BadRequestObjectResult(
            new ApiEnvelope<object>(false, "Validation failed", null, errors));
    };
});
builder.Services.AddScoped<ProductService>();
builder.Services.AddScoped<AddressService>();
builder.Services.AddScoped<AddressExceptionFilter>();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    var defaultSchemaId = new Swashbuckle.AspNetCore.SwaggerGen.SchemaGeneratorOptions().SchemaIdSelector;
    options.CustomSchemaIds(type => type == typeof(Buyer.Application.DTOs.ListingCatalog.ProductImageResponse)
        ? "ListingProductImageResponse" : defaultSchemaId(type));
    var bearerScheme = new OpenApiSecurityScheme
    {
        Name = "Authorization",
        Description = "Enter a Bearer token",
        In = ParameterLocation.Header,
        Type = SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT",
        Reference = new OpenApiReference
        {
            Type = ReferenceType.SecurityScheme,
            Id = JwtBearerDefaults.AuthenticationScheme
        }
    };
    options.AddSecurityDefinition(JwtBearerDefaults.AuthenticationScheme, bearerScheme);
    options.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        [bearerScheme] = []
    });
});
builder.Services.AddProblemDetails();
builder.Services.AddExceptionHandler<GlobalExceptionHandler>();
builder.Services.AddHealthChecks();
builder.Services.AddInfrastructure(builder.Configuration);
if (builder.Configuration.GetValue<string>("BuyerSchema") == "V3" && builder.Configuration.GetValue<bool>("BuyerLifecycle:Enabled"))
    builder.Services.AddHostedService<Buyer.API.BuyerLifecycleWorker>();
if (builder.Configuration.GetValue<string>("BuyerSchema") == "V3" && builder.Configuration.GetValue<bool>("EmailOutbox:Enabled"))
    builder.Services.AddHostedService<Buyer.API.EmailOutboxWorker>();
var jwt = builder.Configuration.GetSection(JwtOptions.SectionName).Get<JwtOptions>() ?? new JwtOptions();
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidIssuer = jwt.Issuer,
            ValidateAudience = true,
            ValidAudience = jwt.Audience,
            ValidateIssuerSigningKey = true,
            RequireSignedTokens = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwt.SigningKey)),
            ValidateLifetime = true,
            RequireExpirationTime = true,
            ClockSkew = TimeSpan.Zero,
            ValidAlgorithms = [SecurityAlgorithms.HmacSha256]
        };
        options.Events = new JwtBearerEvents
        {
            OnTokenValidated = async context =>
            {
                if (builder.Configuration.GetValue<string>("BuyerSchema") != "V3") return;
                var db = context.HttpContext.RequestServices.GetService<Buyer.Infrastructure.BuyerSchema.BuyerSchemaContext>();
                if (db is null) return;
                if (!int.TryParse(context.Principal?.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value, out var uid)
                    || !int.TryParse(context.Principal?.FindFirst("ver")?.Value, out var version)) { context.Fail("Session invalid"); return; }
                var active = await Microsoft.EntityFrameworkCore.EntityFrameworkQueryableExtensions.AnyAsync(db.Users,
                    x => x.id == uid && x.authVersion == version && x.accountStatus == "ACTIVE" && x.role == "BUYER" && x.emailVerified);
                if (!active) context.Fail("Session revoked");
            },
            OnChallenge = async context =>
            {
                context.HandleResponse();
                context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                await context.Response.WriteAsJsonAsync(
                    ApiEnvelope<object>.Failure("AUTH_UNAUTHORIZED", "Authentication required"));
            },
            OnForbidden = context =>
            {
                context.Response.StatusCode = StatusCodes.Status403Forbidden;
                return context.Response.WriteAsJsonAsync(
                    ApiEnvelope<object>.Failure("AUTH_FORBIDDEN", "Access forbidden"));
            }
        };
    });
if (!string.IsNullOrWhiteSpace(builder.Configuration["OAuth:Google:ClientId"]) && !string.IsNullOrWhiteSpace(builder.Configuration["OAuth:Google:ClientSecret"]))
{
    builder.Services.AddAuthentication().AddCookie("External", o => { o.ExpireTimeSpan = TimeSpan.FromMinutes(5); o.Cookie.HttpOnly = true; })
        .AddOpenIdConnect("Google", o =>
        {
            o.SignInScheme = "External"; o.Authority = "https://accounts.google.com";
            o.ClientId = builder.Configuration["OAuth:Google:ClientId"]!; o.ClientSecret = builder.Configuration["OAuth:Google:ClientSecret"]!;
            o.ResponseType = "code"; o.UsePkce = true; o.SaveTokens = false;
            o.CallbackPath = "/signin-google"; o.MapInboundClaims = false;
            o.Scope.Add("email"); o.Scope.Add("profile");
            o.Events.OnTokenValidated = context =>
            {
                if (!string.Equals(context.Principal?.FindFirst("email_verified")?.Value, "true", StringComparison.OrdinalIgnoreCase)) context.Fail("Verified provider email required");
                return Task.CompletedTask;
            };
        });
}
builder.Services.AddAuthorization();
builder.Services.AddCors(options =>
{
    options.AddPolicy("Frontend", policy =>
    {
        var origins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>()
            ?? ["http://localhost:5173"];
        policy.WithOrigins(origins).AllowAnyHeader().AllowAnyMethod();
    });
});
builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
    options.OnRejected = async (context, cancellationToken) =>
    {
        if (context.Lease.TryGetMetadata(MetadataName.RetryAfter, out var retryAfter))
        {
            context.HttpContext.Response.Headers.RetryAfter =
                Math.Ceiling(retryAfter.TotalSeconds).ToString(System.Globalization.CultureInfo.InvariantCulture);
        }

        var code = context.HttpContext.Request.Path.Equals("/api/auth/login", StringComparison.OrdinalIgnoreCase)
            ? "AUTH_RATE_LIMITED"
            : "RATE_LIMIT_EXCEEDED";
        await context.HttpContext.Response.WriteAsJsonAsync(
            ApiEnvelope<object>.Failure(code, "Too many requests"),
            cancellationToken);
    };
    options.AddPolicy("Api", context =>
    {
        var auth = HttpMethods.IsPost(context.Request.Method) && context.Request.Path.StartsWithSegments("/api/auth");
        var identity = auth ? "auth:" + context.Connection.RemoteIpAddress : "api:" + (context.User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value ?? context.Connection.RemoteIpAddress?.ToString() ?? "anonymous");
        return RateLimitPartition.GetFixedWindowLimiter(identity, _ => new FixedWindowRateLimiterOptions
        {
            PermitLimit = auth ? builder.Configuration.GetValue("RateLimiting:LoginPermitLimit",5) : builder.Configuration.GetValue("RateLimiting:PermitLimit",100),
            Window = TimeSpan.FromSeconds(auth ? builder.Configuration.GetValue("RateLimiting:LoginWindowSeconds",60) : builder.Configuration.GetValue("RateLimiting:WindowSeconds",60)),
            QueueLimit = 0
        });
    });
    options.AddPolicy("Login", context => RateLimitPartition.GetFixedWindowLimiter(
        context.Connection.RemoteIpAddress?.ToString() ?? "anonymous",
        _ => new FixedWindowRateLimiterOptions
        {
            PermitLimit = builder.Configuration.GetValue("RateLimiting:LoginPermitLimit", 5),
            Window = TimeSpan.FromSeconds(builder.Configuration.GetValue("RateLimiting:LoginWindowSeconds", 60)),
            QueueLimit = 0
        }));
});

builder.Services.Configure<Microsoft.AspNetCore.Builder.ForwardedHeadersOptions>(options =>
{
    options.ForwardedHeaders = Microsoft.AspNetCore.HttpOverrides.ForwardedHeaders.XForwardedFor | Microsoft.AspNetCore.HttpOverrides.ForwardedHeaders.XForwardedProto;
    foreach (var proxy in builder.Configuration.GetSection("ReverseProxy:KnownProxies").Get<string[]>() ?? [])
        if (System.Net.IPAddress.TryParse(proxy, out var ip)) options.KnownProxies.Add(ip);
});
var app = builder.Build();
app.UseForwardedHeaders();

app.UseExceptionHandler();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseCors("Frontend");
app.UseAuthentication();
app.UseRateLimiter();
app.UseAuthorization();
app.MapHealthChecks("/health/live");
app.MapGet("/health/ready", async (IServiceProvider services, CancellationToken ct) =>
{
    try
    {
        var db = services.GetService<Buyer.Infrastructure.BuyerSchema.BuyerSchemaContext>();
        if (db is null || !await db.Database.CanConnectAsync(ct)) return Results.StatusCode(503);
        var schema = await Microsoft.EntityFrameworkCore.EntityFrameworkQueryableExtensions.SingleAsync(db.Database.SqlQueryRaw<int>("SELECT CASE WHEN COL_LENGTH('dbo.User','authVersion') IS NOT NULL AND COL_LENGTH('dbo.CheckoutGroup','taxAmount') IS NOT NULL AND OBJECT_ID('dbo.OperationReceipt') IS NOT NULL AND OBJECT_ID('dbo.EmailOutbox') IS NOT NULL THEN 1 ELSE 0 END AS Value"), ct);
        if (schema != 1) return Results.StatusCode(503);
        return Results.Ok(new { status = "ready", database = "connected", schema = "buyer-completion-1" });
    }
    catch { return Results.StatusCode(503); }
});
app.MapControllers().RequireRateLimiting("Api");

app.Run();

public partial class Program;
