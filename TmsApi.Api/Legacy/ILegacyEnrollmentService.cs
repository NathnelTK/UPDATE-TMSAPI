namespace TmsApi.Api.Legacy;

public interface ILegacyEnrollmentService
{
    Task<List<string>> GetAllAsync();
    Task ProcessBatchAsync();
}
