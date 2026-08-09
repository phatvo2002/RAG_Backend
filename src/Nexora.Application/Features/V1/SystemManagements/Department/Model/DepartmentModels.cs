namespace Nexora.Application.Features.V1.SystemManagements.Department.Model;

public sealed record DepartmentResponse(Guid DepartmentId, Guid? ParentDepartmentId, string Code, string Name, Guid? ManagerUserId, string Status);

public sealed record DepartmentMemberResponse(Guid UserId, string MembershipType, DateTimeOffset JoinedAt, DateTimeOffset? LeftAt);

public sealed record CreateDepartmentRequest(string Code, string Name, Guid? ParentDepartmentId = null, Guid? ManagerUserId = null);

public sealed record UpdateDepartmentRequest(string Name, Guid? ParentDepartmentId = null, Guid? ManagerUserId = null);
