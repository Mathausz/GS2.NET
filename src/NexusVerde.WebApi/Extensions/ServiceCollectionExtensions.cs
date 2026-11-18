using System.Globalization;
using System.Reflection;
using System.Text;
using System.Text.Json;
using System.Threading.RateLimiting;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Cors.Infrastructure;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using NexusVerde.Api.HealthChecks;
using NexusVerde.Api.Middleware;
using NexusVerde.Api.Observability;
using NexusVerde.Api.Options;
using NexusVerde.Api.Security;
using NexusVerde.Api.Swagger;
using NexusVerde.Application.Interfaces;
using NexusVerde.Application.Services.Implementations;
using NexusVerde.Application.Services.Interfaces;
using NexusVerde.Application.Telemetry;
using NexusVerde.Infrastructure;
using NexusVerde.Infrastructure.Data;

namespace NexusVerde.Api.Extensions;

public static class ServiceCollectionExtensions
{
    /// <summary>Registra toda a API: camadas, segurança, rate limiting, health checks, Swagger e tratamento de erros.</summary>
    public static IServiceCollection AddNexusVerdeApi(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddInfrastructure();
        services.AddApplicationServices();
        services.AddJwtSecurity();
        services.AddRateLimitingPolicies();
        services.AddCorsSetup();
        services.AddHealthChecksSetup();
        services.AddSwaggerSetup();
        services.AddErrorHandling();
        services.AddOptions<IdempotencyOptions>().BindConfiguration(IdempotencyOptions.Section);
        services.AddNexusVerdeObservability(configuration);

        services.Configure<ForwardedHeadersOptions>(o =>
        {
            // Atrás de proxy/PaaS (ex.: Azure) o IP real do cliente vem em X-Forwarded-For.
            o.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
            o.KnownNetworks.Clear();
            o.KnownProxies.Clear();
        });

        services.AddHostedService<DatabaseInitializerHostedService>();
        return services;
    }

    public static IServiceCollection AddApplicationServices(this IServiceCollection services)
    {
        services.AddScoped<IAuthService, AuthService>();
        services.AddScoped<IUsuarioService, UsuarioService>();
        services.AddScoped<IRegiaoService, RegiaoService>();
        services.AddScoped<IDesastreService, DesastreService>();
        services.AddScoped<IPontoAguaService, PontoAguaService>();
        services.AddScoped<ISolicitacaoService, SolicitacaoService>();
        services.AddScoped<IAtendimentoService, AtendimentoService>();
        services.AddScoped<IOrientacaoService, OrientacaoService>();
        services.AddScoped<ITimelineService, TimelineService>();
        return services;
    }

    public static IServiceCollection AddJwtSecurity(this IServiceCollection services)
    {
        services.AddOptions<JwtOptions>()
            .BindConfiguration(JwtOptions.Section)
            .PostConfigure<IHostEnvironment>((jwt, environment) =>
            {
                // Development sem Jwt:Key → chave aleatória temporária (a API sobe sem configuração).
                // Em qualquer outro ambiente a chave continua obrigatória (validada abaixo).
                if (environment.IsDevelopment() && string.IsNullOrWhiteSpace(jwt.Key))
                    jwt.Key = ChaveJwtDesenvolvimento.Obter();
            })
            .ValidateDataAnnotations()
            .ValidateOnStart();

        services.AddSingleton<ITokenGenerator, JwtTokenGenerator>();

        services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme).AddJwtBearer();

        // Configurado de forma preguiçosa a partir de JwtOptions (validado na subida).
        services.AddOptions<JwtBearerOptions>(JwtBearerDefaults.AuthenticationScheme)
            .Configure<IOptions<JwtOptions>>((bearer, jwt) =>
            {
                var j = jwt.Value;
                bearer.MapInboundClaims = false;
                bearer.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidIssuer = j.Issuer,
                    ValidateAudience = true,
                    ValidAudience = j.Audience,
                    ValidateIssuerSigningKey = true,
                    IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(j.Key)),
                    ValidateLifetime = true,
                    ClockSkew = TimeSpan.FromMinutes(1),
                    NameClaimType = "sub",
                    RoleClaimType = "role"
                };
            });

        services.AddAuthorizationBuilder()
            .AddPolicy(Policies.Cidadao, p => p.RequireRole("Cidadao"))
            .AddPolicy(Policies.Gestor, p => p.RequireRole("Gestor", "Admin"))
            .AddPolicy(Policies.Admin, p => p.RequireRole("Admin"));

        return services;
    }

    public static IServiceCollection AddRateLimitingPolicies(this IServiceCollection services)
    {
        services.AddOptions<RateLimitingOptions>().BindConfiguration(RateLimitingOptions.Section);
        services.AddRateLimiter(_ => { });

        services.AddOptions<RateLimiterOptions>()
            .Configure<IOptions<RateLimitingOptions>>((limiter, settings) =>
            {
                var cfg = settings.Value;
                limiter.RejectionStatusCode = StatusCodes.Status429TooManyRequests;

                static FixedWindowRateLimiterOptions Janela(LimitPolicyOptions o) => new()
                {
                    PermitLimit = o.PermitLimit,
                    Window = TimeSpan.FromSeconds(o.WindowSeconds),
                    QueueLimit = 0,
                    AutoReplenishment = true
                };

                static string Ip(HttpContext ctx) => ctx.Connection.RemoteIpAddress?.ToString() ?? "desconhecido";
                static string Usuario(HttpContext ctx) =>
                    ctx.User.FindFirst("sub")?.Value is { } sub ? $"user:{sub}" : $"ip:{Ip(ctx)}";

                limiter.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(ctx =>
                    RateLimitPartition.GetFixedWindowLimiter(Usuario(ctx), _ => Janela(cfg.Global)));

                limiter.AddPolicy(RateLimitPolicies.Auth, ctx =>
                    RateLimitPartition.GetFixedWindowLimiter($"auth:{Ip(ctx)}", _ => Janela(cfg.Auth)));

                limiter.AddPolicy(RateLimitPolicies.Criacao, ctx =>
                    RateLimitPartition.GetFixedWindowLimiter($"criacao:{Usuario(ctx)}", _ => Janela(cfg.Criacao)));

                limiter.OnRejected = async (context, ct) =>
                {
                    var response = context.HttpContext.Response;
                    response.StatusCode = StatusCodes.Status429TooManyRequests;

                    NexusVerdeTelemetry.RateLimitRejeitados.Add(1,
                        new KeyValuePair<string, object?>("rota",
                            (context.HttpContext.GetEndpoint() as RouteEndpoint)?.RoutePattern.RawText ?? "nao-mapeada"));

                    if (context.Lease.TryGetMetadata(MetadataName.RetryAfter, out var retryAfter))
                        response.Headers["Retry-After"] =
                            ((int)Math.Ceiling(retryAfter.TotalSeconds)).ToString(CultureInfo.InvariantCulture);

                    var problem = new ProblemDetails
                    {
                        Status = StatusCodes.Status429TooManyRequests,
                        Title = "Muitas requisições",
                        Detail = "Limite de requisições excedido. Aguarde e tente novamente.",
                        Instance = context.HttpContext.Request.Path
                    };
                    await response.WriteAsJsonAsync(problem, (JsonSerializerOptions?)null, "application/problem+json", ct);
                };
            });

        return services;
    }

    /// <summary>
    /// CORS para clientes web (ex.: app Expo rodando no navegador). Origens vêm de <c>Cors:AllowedOrigins</c>;
    /// em Development, sem origens configuradas, qualquer origem é aceita. Apps nativos não usam CORS.
    /// </summary>
    public static IServiceCollection AddCorsSetup(this IServiceCollection services)
    {
        services.AddCors();
        services.AddOptions<CorsOptions>()
            .Configure<IConfiguration, IHostEnvironment>((cors, configuration, environment) =>
            {
                var origens = configuration.GetSection("Cors:AllowedOrigins").Get<string[]>() ?? Array.Empty<string>();
                var expostos = new[] { CorrelationIdMiddleware.HeaderName, IdempotencyMiddleware.ReplayHeaderName, "Retry-After" };

                cors.AddPolicy(CorsPolicies.App, policy =>
                {
                    if (origens.Length > 0)
                        policy.WithOrigins(origens).AllowAnyHeader().AllowAnyMethod().WithExposedHeaders(expostos);
                    else if (environment.IsDevelopment())
                        policy.AllowAnyOrigin().AllowAnyHeader().AllowAnyMethod().WithExposedHeaders(expostos);
                    // Sem origens fora de Development: nenhuma origem web externa é liberada.
                });
            });

        return services;
    }

    public static IServiceCollection AddHealthChecksSetup(this IServiceCollection services)
    {
        services.AddHealthChecks()
            .AddCheck("api", () => HealthCheckResult.Healthy("API em execução."), tags: new[] { "live" })
            .AddDbContextCheck<NexusVerdeDbContext>(
                name: "database",
                failureStatus: HealthStatus.Unhealthy,
                tags: new[] { "ready", "db" })
            .AddCheck<WeatherHealthCheck>(
                name: "open-meteo",
                failureStatus: HealthStatus.Degraded,
                tags: new[] { "ready", "external" });

        return services;
    }

    public static IServiceCollection AddSwaggerSetup(this IServiceCollection services)
    {
        services.AddEndpointsApiExplorer();
        services.AddSwaggerGen(c =>
        {
            c.SwaggerDoc("v1", new OpenApiInfo
            {
                Title = "Nexus Verde API",
                Version = "v1",
                Description = "Backend do Nexus Verde (Global Solution 2026/2 - CitizenTech): conecta cidadãos, órgãos públicos e " +
                              "equipes de resgate para garantir acesso à água potável em crises. Autentique-se em /api/v1/auth/login " +
                              "e informe o token no botão Authorize."
            });

            c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
            {
                Name = "Authorization",
                Type = SecuritySchemeType.Http,
                Scheme = "bearer",
                BearerFormat = "JWT",
                In = ParameterLocation.Header,
                Description = "Informe apenas o token JWT (sem o prefixo 'Bearer')."
            });

            c.OperationFilter<NexusVerdeOperationFilter>();
            c.CustomSchemaIds(t => t.FullName?.Replace("+", "."));

            var xml = Path.Combine(AppContext.BaseDirectory, $"{Assembly.GetExecutingAssembly().GetName().Name}.xml");
            if (File.Exists(xml)) c.IncludeXmlComments(xml);
        });

        return services;
    }

    public static IServiceCollection AddErrorHandling(this IServiceCollection services)
    {
        services.AddExceptionHandler<GlobalExceptionHandler>();
        services.AddProblemDetails();
        return services;
    }
}
