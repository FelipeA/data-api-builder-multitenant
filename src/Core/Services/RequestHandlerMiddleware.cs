using Azure.DataApiBuilder.Core.Configurations;
using Microsoft.AspNetCore.Http;

namespace Azure.DataApiBuilder.Core.Services;

public class RequestHandlerMiddleware
{
    private readonly RequestDelegate _next;
    private readonly RuntimeConfigProvider _runtimeConfigProvider;
    private readonly MultiTenantTokenService _tokenService;

    public RequestHandlerMiddleware(RequestDelegate next, RuntimeConfigProvider runtimeConfigProvider, MultiTenantTokenService tokenService)
    {
        _next = next;
        _runtimeConfigProvider = runtimeConfigProvider;
        _tokenService = tokenService;
    }

    public async Task Invoke(HttpContext context, MultiTenantDataSourceService dataSourceOptions)
    {
        try
        {
            if (_runtimeConfigProvider.GetConfig().IsEnabledMultiTenancy)
            {
                // Skip tenant validation for swagger documentation.
                if (context.Request.Path == "/api/openapi")
                {
                    await _next(context);
                    return;
                }

                // Skip tenant validation for graphql documentation.
                // For graphql it is always skiping becuase there is not specific path for documentation.
                if (context.Request.Path.ToString().Contains("/graphql"))
                {
                    await _next(context);
                    return;
                }

                if (!context.Request.Headers.ContainsKey("X-Tenant-ID"))
                {
                    context.Response.StatusCode = 400;
                    await context.Response.WriteAsync("Error: X-Tenant-ID header is missing.");
                    return;
                }

                string tenantID = context.Request.Headers["X-Tenant-ID"].ToString();
                tenantID = _tokenService.DecodeToken(tenantID);
                context.Request.Headers["X-Tenant-ID--Decoded"] = tenantID;
            }

            await _next(context);
        }
        catch (Exception ex)
        {
            context.Response.StatusCode = 400;
            await context.Response.WriteAsync($"Error: {ex.Message}");
        }
    }
}
