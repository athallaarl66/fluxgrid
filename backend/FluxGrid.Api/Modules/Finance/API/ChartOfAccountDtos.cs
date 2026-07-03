namespace FluxGrid.Api.Modules.Finance.API;

public sealed record CreateAccountRequest(
    string Code,
    string Name,
    Guid? ParentId,
    string Type,
    bool IsActive = true
);

public sealed record UpdateAccountRequest(
    string? Code,
    string? Name,
    Guid? ParentId,
    string? Type,
    bool? IsActive
);

public sealed record AccountResponse(
    Guid Id,
    string Code,
    string Name,
    Guid? ParentId,
    string Type,
    bool IsActive,
    Guid TenantId,
    DateTime CreatedAt,
    DateTime UpdatedAt,
    List<AccountResponse>? Children = null
);

public sealed record AccountTreeNode(
    Guid Id,
    string Code,
    string Name,
    Guid? ParentId,
    string Type,
    bool IsActive,
    int Depth,
    List<AccountTreeNode> Children
);
