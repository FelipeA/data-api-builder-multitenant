// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Data.Common;
using Azure.DataApiBuilder.Core.Configurations;
using Microsoft.AspNetCore.Http;

namespace Azure.DataApiBuilder.Core.Services;

public class MultiTenantDataSourceService
{
    private readonly RuntimeConfigProvider _runtimeConfigProvider;

    public MultiTenantDataSourceService(RuntimeConfigProvider runtimeConfigProvider)
    {
        _runtimeConfigProvider = runtimeConfigProvider;
    }

    public bool GetConnectionString(HttpContext httpContext, string dataSourceName, IDictionary<string, DbConnectionStringBuilder> ConnectionStringBuilders, out string connectionString)
    {
        if (!_runtimeConfigProvider.GetConfig().IsEnabledMultiTenancy)
        {
            connectionString = string.Empty;
            return false;
        }

        // Extract the Tenant ID from request headers
        string tenantId = httpContext.Request.Headers["X-Tenant-ID--Decoded"].ToString();

        if (string.IsNullOrEmpty(tenantId))
        {
            connectionString = string.Empty;
            return false;
        }

        // Map tenant ID to connection string
        connectionString = GetTenantConnectionString(ConnectionStringBuilders, dataSourceName, tenantId);

        if (string.IsNullOrEmpty(connectionString))
        {
            return false;
        }

        return true;
    }

#pragma warning disable CA1822 // Mark members as static
    private string GetTenantConnectionString(IDictionary<string, DbConnectionStringBuilder> ConnectionStringBuilders, string dataSourceName, string tenantId)
#pragma warning restore CA1822 // Mark members as static
    {
        string tenantDataSourceKey = $"{dataSourceName}--{tenantId}";
        if (ConnectionStringBuilders.ContainsKey(tenantDataSourceKey))
        {
            return ConnectionStringBuilders[tenantDataSourceKey].ConnectionString;
        }

        throw new Exception("Invalid Tenant ID");
    }
}
