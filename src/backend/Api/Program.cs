using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Api.Authentication;
using Api.Audit;
using Api.Common.Authorization;
using Api.Common.Cors;
using Application.AuditLog.GetAuditLogEntries;
using Application.AuditLog.GetAuditLogEntryById;
using Application.Authentication.AuthenticateUser;
using Application.Common.Audit;
using Application.Common.Authentication;
using Application.Common.Persistence;
using Application.Common.Users;
using Application.Institutions;
using Application.Institutions.CreateInstitution;
using Application.Institutions.DeleteInstitution;
using Application.Institutions.GetInstitutionById;
using Application.Institutions.GetInstitutionFilterOptions;
using Application.Institutions.GetInstitutions;
using Application.Institutions.UpdateInstitution;
using Application.Users;
using Application.Users.ChangeUserPassword;
using Application.Users.CreateUser;
using Application.Users.DeleteUser;
using Application.Users.GetUserById;
using Application.Users.GetUsers;
using Application.Users.UpdateUser;
using Application.Requests;
using Application.Requests.AddComment;
using Application.Requests.AssignRequest;
using Application.Requests.ChangeRequestStatus;
using Application.Requests.CreateRequest;
using Application.Requests.Dashboard;
using Application.Requests.GetRequestDetail;
using Application.Requests.ListRequests;
using Application.Requests.Lookups;
using Application.Requests.Notifications;
using Application.Requests.ReopenRequest;
using Application.Requests.UpdateRequest;
using Infrastructure.Areas;
using Infrastructure.Audit;
using Infrastructure.AuditLog;
using Infrastructure.Authentication;
using Infrastructure.Institutions;
using Infrastructure.Persistence;
using Infrastructure.Requests;
using Infrastructure.Requests.Notifications;
using Infrastructure.RequestTypes;
using Infrastructure.Users;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;

var builder = WebApplication.CreateBuilder(args);

const string DataProtectionKeysPath = "/root/.aspnet/DataProtection-Keys";
const string DataProtectionApplicationName = "SB.Api";

var dataProtectionBuilder = builder.Services
    .AddDataProtection()
    .SetApplicationName(DataProtectionApplicationName);

try
{
    var keysDirectory = new DirectoryInfo(DataProtectionKeysPath);
    keysDirectory.Create();
    dataProtectionBuilder.PersistKeysToFileSystem(keysDirectory);
}
catch (Exception)
{
}

builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.Converters.Add(
            new System.Text.Json.Serialization.JsonStringEnumConverter());
    });
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "SB API",
        Version = "v1",
        Description = "REST API for institutions, authentication, request management, catalogs and dashboards."
    });

    var bearerScheme = new OpenApiSecurityScheme
    {
        Name = "Authorization",
        Description = "JWT Bearer token. Example: \"Bearer {token}\"",
        In = ParameterLocation.Header,
        Type = SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT",
        Reference = new OpenApiReference
        {
            Type = ReferenceType.SecurityScheme,
            Id = "Bearer"
        }
    };

    options.AddSecurityDefinition("Bearer", bearerScheme);

    options.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        [bearerScheme] = Array.Empty<string>()
    });
});

var jwtSettings = builder.Configuration
    .GetSection(JwtSettings.ConfigurationSectionName)
    .Get<JwtSettings>() ?? new JwtSettings();

if (!jwtSettings.IsValid)
{
    throw new InvalidOperationException(
        "JWT settings are missing or invalid at startup. "
        + "Configure Jwt:Issuer, Jwt:Audience, Jwt:SecretKey (>= 32 chars) "
        + "and Jwt:ExpirationMinutes (> 0) under the 'Jwt' section "
        + "(appsettings.json, appsettings.{Environment}.json, or environment variables "
        + "such as Jwt__Issuer, Jwt__SecretKey, ...).");
}

builder.Services.AddOptions<JwtSettings>()
    .Bind(builder.Configuration.GetSection(JwtSettings.ConfigurationSectionName));

builder.Services.AddSingleton(jwtSettings);

var corsSettings = builder.Configuration
    .GetSection(CorsSettings.ConfigurationSectionName)
    .Get<CorsSettings>() ?? new CorsSettings();

if (!corsSettings.IsValid)
{
    throw new InvalidOperationException(
        "CORS settings are missing at startup. "
        + "Configure Cors:AllowedOrigins (non-empty array) under the 'Cors' section "
        + "(appsettings.json, appsettings.{Environment}.json, or environment variables "
        + "such as Cors__AllowedOrigins__0).");
}

builder.Services.AddOptions<CorsSettings>()
    .Bind(builder.Configuration.GetSection(CorsSettings.ConfigurationSectionName));

builder.Services.AddSingleton(corsSettings);

builder.Services.AddCors(options =>
{
    options.AddPolicy(CorsPolicyNames.Default, policy =>
    {
        policy.WithOrigins(corsSettings.AllowedOrigins)
            .AllowAnyHeader()
            .AllowAnyMethod()
            .AllowCredentials();
    });
});

builder.Services.AddScoped<GetInstitutionsQueryHandler>();
builder.Services.AddScoped<GetInstitutionFilterOptionsQueryHandler>();
builder.Services.AddScoped<GetInstitutionByIdHandler>();
builder.Services.AddScoped<CreateInstitutionHandler>();
builder.Services.AddScoped<UpdateInstitutionHandler>();
builder.Services.AddScoped<DeleteInstitutionHandler>();
builder.Services.AddScoped<IInstitutionReadRepository, InstitutionReadRepository>();
builder.Services.AddScoped<IInstitutionFilterOptionsReadRepository, InstitutionReadRepository>();
builder.Services.AddScoped<IInstitutionWriteRepository, InstitutionWriteRepository>();
builder.Services.AddScoped<Application.Institutions.Classifications.ICategoryReadRepository, CategoryReadRepository>();
builder.Services.AddScoped<Application.Institutions.Classifications.IStatePowerReadRepository, StatePowerReadRepository>();
builder.Services.AddScoped<Application.Institutions.Classifications.ISectorReadRepository, SectorReadRepository>();
builder.Services.AddScoped<IUserReadRepository, UserReadRepository>();
builder.Services.AddSingleton<IPasswordHasher, PasswordHasher>();
builder.Services.AddSingleton<IJwtTokenIssuer, JwtTokenIssuer>();
builder.Services.AddScoped<AuthenticateUserHandler>();

builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<ICurrentUserAccessor, HttpContextCurrentUserAccessor>();
builder.Services.AddScoped<IAuditContextAccessor, HttpContextAuditContextAccessor>();
builder.Services.AddScoped<IUserLookupRepository, UserLookupRepository>();
builder.Services.AddScoped<IRequestReadRepository, RequestReadRepository>();
builder.Services.AddScoped<IRequestWriteRepository, RequestWriteRepository>();
builder.Services.AddScoped<IAreaReadRepository, AreaReadRepository>();
builder.Services.AddScoped<IRequestTypeReadRepository, RequestTypeReadRepository>();
builder.Services.AddScoped<IRequestCodeGenerator, SqlServerRequestCodeGenerator>();
builder.Services.AddScoped<INotificationSender, DatabaseNotificationSender>();
builder.Services.AddScoped<CreateRequestHandler>();
builder.Services.AddScoped<ListRequestsQueryHandler>();
builder.Services.AddScoped<GetRequestDetailHandler>();
builder.Services.AddScoped<ChangeRequestStatusHandler>();
builder.Services.AddScoped<AssignRequestHandler>();
builder.Services.AddScoped<AddCommentHandler>();
builder.Services.AddScoped<ReopenRequestHandler>();
builder.Services.AddScoped<UpdateRequestHandler>();
builder.Services.AddScoped<ListActiveAreasHandler>();
builder.Services.AddScoped<ListActiveRequestTypesHandler>();
builder.Services.AddScoped<ListStaffCandidatesHandler>();
builder.Services.AddScoped<IDashboardReadRepository, DashboardReadRepository>();
builder.Services.AddScoped<IStaffCandidateRepository, Infrastructure.Requests.StaffCandidateRepository>();
builder.Services.AddScoped<GetDashboardSummaryHandler>();
builder.Services.AddScoped<IAuditLogger, AuditLogger>();
builder.Services.AddScoped<IAuditLogReadRepository, AuditLogReadRepository>();
builder.Services.AddScoped<GetAuditLogEntriesQueryHandler>();
builder.Services.AddScoped<GetAuditLogEntryByIdHandler>();
builder.Services.AddScoped<IUserAdministrationReadRepository, UserAdministrationReadRepository>();
builder.Services.AddScoped<IUserAdministrationWriteRepository, UserAdministrationWriteRepository>();
builder.Services.AddScoped<GetUsersQueryHandler>();
builder.Services.AddScoped<GetUserByIdHandler>();
builder.Services.AddScoped<CreateUserHandler>();
builder.Services.AddScoped<UpdateUserHandler>();
builder.Services.AddScoped<ChangeUserPasswordHandler>();
builder.Services.AddScoped<DeleteUserHandler>();

var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");

if (string.IsNullOrWhiteSpace(connectionString))
{
    throw new InvalidOperationException(
        "Database connection string 'ConnectionStrings:DefaultConnection' is missing. "
        + "Configure it in appsettings.json, appsettings.{Environment}.json, or via the "
        + "environment variable ConnectionStrings__DefaultConnection.");
}

builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(connectionString));

builder.Services
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer();

builder.Services
    .AddOptions<JwtBearerOptions>(JwtBearerDefaults.AuthenticationScheme)
    .Configure<IOptions<JwtSettings>>((bearerOptions, settingsAccessor) =>
    {
        var settings = settingsAccessor.Value;

        bearerOptions.RequireHttpsMetadata = !builder.Environment.IsDevelopment();
        bearerOptions.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = settings.Issuer,
            ValidAudience = settings.Audience,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(settings.SecretKey)),
            ClockSkew = TimeSpan.FromSeconds(30),
            NameClaimType = JwtRegisteredClaimNames.UniqueName,
            RoleClaimType = ClaimTypes.Role
        };
    });

builder.Services.AddAuthorization(options =>
{
    options.AddPolicy(AuthorizationPolicyNames.AdminOnly, policy =>
        policy.RequireAuthenticatedUser().RequireRole(nameof(Domain.Enums.UserRole.Admin)));
});

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();

    app.UseHttpsRedirection();
}

app.UseCors(CorsPolicyNames.Default);

app.UseAuthentication();
app.UseAuthorization();

app.UseMiddleware<Api.Audit.AuthorizationAuditMiddleware>();

app.MapControllers();

app.Run();

public partial class Program;
