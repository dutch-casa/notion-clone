using System.Text;
using Backend.Application.Services;
using Backend.Infrastructure.Services;
using Backend.Application.UseCases.Auth.Login;
using Backend.Application.UseCases.Auth.Register;
using Backend.Application.UseCases.Organizations.CreateOrganization;
using Backend.Application.UseCases.Organizations.ListOrganizations;
using Backend.Application.UseCases.Organizations.GetOrganization;
using Backend.Application.UseCases.Organizations.InviteMember;
using Backend.Application.UseCases.Organizations.RemoveMember;
using Backend.Application.UseCases.Organizations.UpdateMemberRole;
using Backend.Application.UseCases.Organizations.CreateInvitation;
using Backend.Application.UseCases.Organizations.ListInvitations;
using Backend.Application.UseCases.Organizations.AcceptInvitation;
using Backend.Application.UseCases.Organizations.DeclineInvitation;
using Backend.Application.UseCases.Pages.CreatePage;
using Backend.Application.UseCases.Pages.GetPage;
using Backend.Application.UseCases.Pages.ListPages;
using Backend.Application.UseCases.Pages.UpdatePageTitle;
using Backend.Application.UseCases.Pages.DeletePage;
using Backend.Application.UseCases.Blocks.AddBlock;
using Backend.Application.UseCases.Blocks.UpdateBlock;
using Backend.Application.UseCases.Blocks.RemoveBlock;
using Backend.Application.UseCases.Users.UpdateUserProfile;
using Backend.Application.UseCases.Images.UploadImage;
using Backend.Application.UseCases.Images.DeleteImage;
using Backend.Infrastructure.Persistence;
using Backend.Presentation.Hubs;
using Backend.Presentation.Middleware;
using Minio;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new() { Title = "Notion Clone API", Version = "v1" });
});

// Redis configuration
var redisConnectionString = builder.Configuration.GetConnectionString("Redis")
    ?? "localhost:6379";

// Redis distributed cache for CRDT document state
builder.Services.AddStackExchangeRedisCache(options =>
{
    options.Configuration = redisConnectionString;
    options.InstanceName = "NotionClone:";
});

// SignalR with Redis backplane for real-time collaboration
builder.Services.AddSignalR()
    .AddStackExchangeRedis(redisConnectionString, options =>
    {
        options.Configuration.ChannelPrefix = StackExchange.Redis.RedisChannel.Literal("NotionClone:SignalR:");
    });

// Database - EF Core DbContext acts as both repository and unit of work
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseNpgsql(
        builder.Configuration.GetConnectionString("DefaultConnection")
            ?? throw new InvalidOperationException("ConnectionStrings:DefaultConnection is required"),
        npgsqlOptions => npgsqlOptions.MigrationsAssembly("Backend.Infrastructure")));

// Repositories - DDD pattern with dependency inversion
builder.Services.AddScoped<Backend.Domain.Repositories.IPageRepository, Backend.Infrastructure.Persistence.Repositories.PageRepository>();
builder.Services.AddScoped<Backend.Domain.Repositories.IOrgRepository, Backend.Infrastructure.Persistence.Repositories.OrgRepository>();
builder.Services.AddScoped<Backend.Domain.Repositories.IUserRepository, Backend.Infrastructure.Persistence.Repositories.UserRepository>();
builder.Services.AddScoped<Backend.Domain.Repositories.IBlockRepository, Backend.Infrastructure.Persistence.Repositories.BlockRepository>();
builder.Services.AddScoped<Backend.Domain.Repositories.IInvitationRepository, Backend.Infrastructure.Persistence.Repositories.InvitationRepository>();
builder.Services.AddScoped<Backend.Domain.Repositories.IImageRepository, Backend.Infrastructure.Persistence.Repositories.ImageRepository>();
builder.Services.AddScoped<Backend.Domain.Repositories.IUnitOfWork, Backend.Infrastructure.Persistence.Repositories.UnitOfWork>();

// Services
builder.Services.AddScoped<IPasswordHasher, PasswordHasher>();
builder.Services.AddScoped<IJwtTokenGenerator, JwtTokenGenerator>();
builder.Services.AddSingleton<IInvitationNotificationService, InvitationNotificationService>();
builder.Services.AddSingleton<IPageNotificationService, PageNotificationService>();

// Domain event handlers
builder.Services.AddScoped<Backend.Infrastructure.EventHandlers.PageCreatedEventHandler>();
builder.Services.AddScoped<Backend.Infrastructure.EventHandlers.PageTitleChangedEventHandler>();
builder.Services.AddScoped<Backend.Infrastructure.EventHandlers.PageDeletedEventHandler>();

// Domain event dispatcher
builder.Services.AddScoped<IDomainEventDispatcher, DomainEventDispatcher>();

// MinIO client configuration
builder.Services.AddSingleton<IMinioClient>(sp =>
{
    var endpoint = builder.Configuration["MinIO:Endpoint"] ?? throw new InvalidOperationException("MinIO:Endpoint is required");
    var accessKey = builder.Configuration["MinIO:AccessKey"] ?? throw new InvalidOperationException("MinIO:AccessKey is required");
    var secretKey = builder.Configuration["MinIO:SecretKey"] ?? throw new InvalidOperationException("MinIO:SecretKey is required");
    var useSSL = builder.Configuration.GetValue<bool>("MinIO:UseSSL", true);

    return new MinioClient()
        .WithEndpoint(endpoint)
        .WithCredentials(accessKey, secretKey)
        .WithSSL(useSSL)
        .Build();
});

// File storage service
builder.Services.AddScoped<IFileStorageService, MinioFileStorageService>(sp =>
{
    var minioClient = sp.GetRequiredService<IMinioClient>();
    var bucketName = builder.Configuration["MinIO:BucketName"] ?? throw new InvalidOperationException("MinIO:BucketName is required");
    return new MinioFileStorageService(minioClient, bucketName);
});

// Use case handlers
builder.Services.AddScoped<RegisterHandler>();
builder.Services.AddScoped<LoginHandler>();
builder.Services.AddScoped<CreateOrganizationHandler>();
builder.Services.AddScoped<ListOrganizationsHandler>();
builder.Services.AddScoped<GetOrganizationHandler>();
builder.Services.AddScoped<InviteMemberHandler>();
builder.Services.AddScoped<RemoveMemberHandler>();
builder.Services.AddScoped<UpdateMemberRoleHandler>();
builder.Services.AddScoped<CreateInvitationHandler>();
builder.Services.AddScoped<CreateInvitationByEmailHandler>();
builder.Services.AddScoped<ListInvitationsHandler>();
builder.Services.AddScoped<AcceptInvitationHandler>();
builder.Services.AddScoped<DeclineInvitationHandler>();
builder.Services.AddScoped<CreatePageHandler>();
builder.Services.AddScoped<GetPageHandler>();
builder.Services.AddScoped<ListPagesHandler>();
builder.Services.AddScoped<UpdatePageTitleHandler>();
builder.Services.AddScoped<DeletePageHandler>();
builder.Services.AddScoped<AddBlockHandler>();
builder.Services.AddScoped<UpdateBlockHandler>();
builder.Services.AddScoped<RemoveBlockHandler>();
builder.Services.AddScoped<UpdateUserProfileHandler>();
builder.Services.AddScoped<UploadImageHandler>();
builder.Services.AddScoped<DeleteImageHandler>();

// JWT Authentication
var jwtSecret = builder.Configuration["Jwt:SecretKey"] ?? throw new InvalidOperationException("Jwt:SecretKey is required in production");
var jwtIssuer = builder.Configuration["Jwt:Issuer"] ?? "NotionClone";
var jwtAudience = builder.Configuration["Jwt:Audience"] ?? "NotionCloneUsers";

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = jwtIssuer,
            ValidAudience = jwtAudience,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSecret))
        };

        // Configure JWT to accept tokens from cookies (for EventSource/SSE and regular requests)
        // This is more secure than localStorage or query strings
        options.Events = new JwtBearerEvents
        {
            OnMessageReceived = context =>
            {
                // First, try to get token from HttpOnly cookie
                var token = context.Request.Cookies["auth_token"];

                // Fallback to query string for SignalR (WebSocket upgrade) if no cookie
                if (string.IsNullOrEmpty(token))
                {
                    var accessToken = context.Request.Query["access_token"];
                    var path = context.HttpContext.Request.Path;

                    // Only allow query string tokens for SignalR hub during WebSocket upgrade
                    if (!string.IsNullOrEmpty(accessToken) && path.StartsWithSegments("/hubs/document"))
                    {
                        token = accessToken;
                    }
                }

                if (!string.IsNullOrEmpty(token))
                {
                    context.Token = token;
                }

                return Task.CompletedTask;
            }
        };
    });

// CORS - SignalR requires specific origin with credentials
var allowedOrigins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>()
    ?? new[] { "http://localhost:3000", "http://localhost:5173" };

// Log configured origins for debugging
Console.WriteLine($"Configured CORS origins: {string.Join(", ", allowedOrigins)}");

builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowFrontend", policy =>
    {
        policy.WithOrigins(allowedOrigins)
              .AllowAnyMethod()
              .AllowAnyHeader()
              .AllowCredentials() // Required for SignalR
              .SetIsOriginAllowedToAllowWildcardSubdomains();
    });
});

// Health checks
builder.Services.AddHealthChecks();

var app = builder.Build();

// Configure the HTTP request pipeline
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

// CORS must be first to handle preflight requests
app.UseCors("AllowFrontend");

// Global exception handler - must be early in pipeline to catch all exceptions
app.UseMiddleware<GlobalExceptionHandlerMiddleware>();

// Skip HTTPS redirection in production when behind a reverse proxy (Traefik handles HTTPS)
if (app.Environment.IsDevelopment())
{
    app.UseHttpsRedirection();
}

// HSTS for production environments
if (!app.Environment.IsDevelopment())
{
    app.UseHsts();
}

app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

// Map SignalR hub
app.MapHub<DocumentHub>("/hubs/document");

// Health check endpoint
app.MapHealthChecks("/health");

// Run database migrations on startup
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
    db.Database.Migrate();
}

app.Run();
