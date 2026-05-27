using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Options;

namespace ManufacturingCostManagement.Web.Authorization
{
    public class PermissionRequirement : IAuthorizationRequirement
    {
        public string Code { get; }
        public PermissionRequirement(string code) { Code = code; }
    }

    public class PermissionHandler : AuthorizationHandler<PermissionRequirement>
    {
        protected override Task HandleRequirementAsync(
            AuthorizationHandlerContext context, PermissionRequirement requirement)
        {
            if (context.User.IsInRole("Admin"))
            {
                context.Succeed(requirement);
                return Task.CompletedTask;
            }
            if (context.User.HasClaim("permission", requirement.Code))
            {
                context.Succeed(requirement);
            }
            return Task.CompletedTask;
        }
    }

    /// Synthesizes a policy on-the-fly for any policy name shaped like "Module.Action".
    /// Falls back to the default provider for built-in policies (AdminOnly, etc.).
    public class PermissionPolicyProvider : IAuthorizationPolicyProvider
    {
        private readonly DefaultAuthorizationPolicyProvider _fallback;

        public PermissionPolicyProvider(IOptions<AuthorizationOptions> options)
        {
            _fallback = new DefaultAuthorizationPolicyProvider(options);
        }

        public Task<AuthorizationPolicy> GetDefaultPolicyAsync() => _fallback.GetDefaultPolicyAsync();
        public Task<AuthorizationPolicy?> GetFallbackPolicyAsync() => _fallback.GetFallbackPolicyAsync();

        public Task<AuthorizationPolicy?> GetPolicyAsync(string policyName)
        {
            if (!string.IsNullOrEmpty(policyName) && policyName.Contains('.'))
            {
                var policy = new AuthorizationPolicyBuilder()
                    .AddRequirements(new PermissionRequirement(policyName))
                    .Build();
                return Task.FromResult<AuthorizationPolicy?>(policy);
            }
            return _fallback.GetPolicyAsync(policyName!);
        }
    }

    public static class PermissionExtensions
    {
        public static bool HasPermission(this ClaimsPrincipal user, string code)
        {
            if (user.IsInRole("Admin")) return true;
            return user.HasClaim("permission", code);
        }
    }
}
