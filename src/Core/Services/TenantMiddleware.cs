using Azure.DataApiBuilder.Config.ObjectModel;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;

namespace Azure.DataApiBuilder.Core.Services;
public class TenantMiddleware
{
    private readonly RequestDelegate _next;
    private readonly RuntimeConfig _runtimeConfig;

    public TenantMiddleware(RequestDelegate next, RuntimeConfig runtimeConfig)
    {
        _next = next;
        _runtimeConfig = runtimeConfig;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        if (context.Request is null)
        {
            await _next(context);
            return; // Add return to avoid further processing
        }

        if (!context.Request.Headers.ContainsKey("Tenant-Id") || context.Request.Headers["Tenant-Id"].Count == 0)
        {
            context.Response.StatusCode = StatusCodes.Status400BadRequest;
            await context.Response.WriteAsync("Tenant-Id header is missing.");
            return;
        }

        // Resolve the tenant and set the connection string in a scoped object
        string tenantId = context.Request.Headers["Tenant-Id"].FirstOrDefault() ?? "";
        if (tenantId is null)
        {
            context.Response.StatusCode = StatusCodes.Status400BadRequest;
            await context.Response.WriteAsync("Tenant-Id header is missing.");
            return;
        }

        string connectionString = GetConnectionStringForTenant(tenantId);
        TenantContext tenantContext = context.RequestServices.GetRequiredService<TenantContext>();
        tenantContext.ConnectionString = connectionString;

        await _next(context);
    }

    private string GetConnectionStringForTenant(string tenantId)
    {
        if (_runtimeConfig.Tenants != null && _runtimeConfig.Tenants.TryGetValue(tenantId, out DataSource? dataSource))
        {
            return dataSource.ConnectionString;
        }

        throw new KeyNotFoundException($"Tenant ID '{tenantId}' not found in the configuration.");
    }
}

public static class TenantMiddlewareExtensions
{
    public static IApplicationBuilder UseTenantMiddleware(this IApplicationBuilder builder)
    {
        return builder.UseMiddleware<TenantMiddleware>();
    }
}

public class TenantContext
{
    public string ConnectionString { get; set; } = string.Empty;
}
