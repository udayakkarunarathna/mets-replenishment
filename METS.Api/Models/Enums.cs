namespace METS.Api.Models;

public enum RequestStatus
{
    Draft,
    Submitted,
    Approved,
    Rejected,
    Fulfilled
}

public enum RequestPriority
{
    Low,
    Normal,
    Urgent
}

public enum ValidationStatus
{
    Pending,
    Passed,
    Failed,
    Unavailable
}

public enum UserRole
{
    Worker,
    Reviewer
}
