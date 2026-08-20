namespace TmsApi.Domain.Entities;

/// <summary>
/// Lifecycle state of an <see cref="Enrollment"/>. A new enrollment starts as
/// <see cref="Pending"/> and a registrar approves or rejects it. Stored as an int
/// (Pending = 0) so the column has a natural DB default and existing rows created
/// before this field was introduced read back as Pending.
/// </summary>
public enum EnrollmentStatus
{
    Pending = 0,
    Approved = 1,
    Rejected = 2
}
