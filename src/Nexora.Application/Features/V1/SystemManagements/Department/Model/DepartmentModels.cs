namespace Nexora.Application.Features.V1.SystemManagements.Department.Model;

public sealed record DepartmentResponse
{
    public DepartmentResponse()
    {
    }

    public DepartmentResponse(Guid departmentId, Guid? parentDepartmentId, string code, string name, Guid? managerUserId, string status)
    {
        DepartmentId = departmentId;
        ParentDepartmentId = parentDepartmentId;
        Code = code;
        Name = name;
        ManagerUserId = managerUserId;
        Status = status;
    }

    public Guid DepartmentId { get; set; }
    public Guid? ParentDepartmentId { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public Guid? ManagerUserId { get; set; }
    public string Status { get; set; } = string.Empty;
}

public sealed record DepartmentMemberResponse
{
    public DepartmentMemberResponse()
    {
    }

    public DepartmentMemberResponse(Guid userId, string membershipType, DateTimeOffset joinedAt, DateTimeOffset? leftAt)
    {
        UserId = userId;
        MembershipType = membershipType;
        JoinedAt = joinedAt;
        LeftAt = leftAt;
    }

    public Guid UserId { get; set; }
    public string MembershipType { get; set; } = string.Empty;
    public DateTimeOffset JoinedAt { get; set; }
    public DateTimeOffset? LeftAt { get; set; }
}

public sealed record CreateDepartmentRequest
{
    public CreateDepartmentRequest()
    {
    }

    public CreateDepartmentRequest(string code, string name, Guid? parentDepartmentId = null, Guid? managerUserId = null)
    {
        Code = code;
        Name = name;
        ParentDepartmentId = parentDepartmentId;
        ManagerUserId = managerUserId;
    }

    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public Guid? ParentDepartmentId { get; set; } = null;
    public Guid? ManagerUserId { get; set; }
}

public sealed record UpdateDepartmentRequest
{
    public UpdateDepartmentRequest()
    {
    }

    public UpdateDepartmentRequest(string name, Guid? parentDepartmentId = null, Guid? managerUserId = null)
    {
        Name = name;
        ParentDepartmentId = parentDepartmentId;
        ManagerUserId = managerUserId;
    }

    public string Name { get; set; } = string.Empty;
    public Guid? ParentDepartmentId { get; set; }
    public Guid? ManagerUserId { get; set; }
}
