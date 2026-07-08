using FluxGrid.Api.Modules.WMS.Domain.Enums;

namespace FluxGrid.Api.Modules.WMS.Domain.Entities;

public class Location
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Code { get; set; } = string.Empty;
    public LocationType Type { get; set; }
    public Guid TenantId { get; set; }
}
