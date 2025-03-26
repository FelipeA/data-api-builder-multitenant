namespace Azure.DataApiBuilder.Config.ObjectModel;

public record MultiTenancy
{
    public bool Enabled { get; init; }
    public string? TenantEncryptKey { get; init; }
}
