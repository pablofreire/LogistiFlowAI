using System.Text;
using LogistiFlow.BuildingBlocks.Web.Options;
using LogistiFlow.BuildingBlocks.Web.Security;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace LogistiFlow.BuildingBlocks.Web.Configuration;

public static class JwtAuthenticationServiceCollectionExtensions
{
    public const string RoleClaimType = "role";
    public const string NameClaimType = "name";

    /// <summary>
    /// JWT bearer validation plus the permission policies. The fallback policy requires an authenticated user,
    /// so an endpoint is protected unless it explicitly opts out with <c>[AllowAnonymous]</c>.
    /// </summary>
    public static IServiceCollection AddLogistiFlowJwtAuthentication(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddOptions<JwtOptions>()
            .Bind(configuration.GetSection(JwtOptions.SectionName))
            .Validate(options => Encoding.UTF8.GetByteCount(options.SigningKey) >= 32, "Jwt:SigningKey must be at least 32 bytes.")
            .ValidateOnStart();

        services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme).AddJwtBearer();
        services.AddOptions<JwtBearerOptions>(JwtBearerDefaults.AuthenticationScheme)
            .Configure<IOptions<JwtOptions>>((bearer, jwt) =>
            {
                bearer.MapInboundClaims = false;
                bearer.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidIssuer = jwt.Value.Issuer,
                    ValidateAudience = true,
                    ValidAudience = jwt.Value.Audience,
                    ValidateIssuerSigningKey = true,
                    IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwt.Value.SigningKey)),
                    ValidateLifetime = true,
                    ClockSkew = TimeSpan.FromSeconds(30),
                    RoleClaimType = RoleClaimType,
                    NameClaimType = NameClaimType,
                };
            });

        services.AddAuthorizationBuilder()
            .SetFallbackPolicy(new AuthorizationPolicyBuilder().RequireAuthenticatedUser().Build())
            .AddPolicy(AuthorizationPolicies.ReadOperations, policy => policy.RequireRole(LogistiFlowRoles.All))
            .AddPolicy(AuthorizationPolicies.ManageOrders, policy => policy.RequireRole(LogistiFlowRoles.Admin, LogistiFlowRoles.OpsManager))
            .AddPolicy(AuthorizationPolicies.ManageInventory, policy => policy.RequireRole(LogistiFlowRoles.Admin, LogistiFlowRoles.OpsManager))
            .AddPolicy(
                AuthorizationPolicies.OperateWarehouse,
                policy => policy.RequireRole(LogistiFlowRoles.Admin, LogistiFlowRoles.OpsManager, LogistiFlowRoles.WarehouseOperator));

        return services;
    }
}
