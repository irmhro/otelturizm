using Microsoft.Data.SqlClient;
using otelturizmnew.Models.Legal;
using otelturizmnew.Models.Paneller.Admin;

namespace otelturizmnew.Services.Abstractions;

public interface IContractContentService
{
    Task<ContractDetailPageViewModel?> GetPublicContractBySlugAsync(string slug, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<ContractLinkViewModel>> GetActiveContractsForAudienceAsync(string audience, CancellationToken cancellationToken = default);
    Task RecordRegistrationAcceptancesAsync(SqlConnection connection, SqlTransaction? transaction, ContractAcceptanceRegistrationRequest request, CancellationToken cancellationToken = default);
    Task FinalizeEmailVerificationAsync(SqlConnection connection, SqlTransaction? transaction, long userId, string email, string? ipAddress, string? userAgent, CancellationToken cancellationToken = default);
    Task<AdminContractManagementPageViewModel> GetAdminContractManagementAsync(string fullName, string email, string userRole, long? contractId, CancellationToken cancellationToken = default);
    Task<(bool Success, string Message)> SaveContractAsync(long adminUserId, AdminContractForm request, CancellationToken cancellationToken = default);
    Task<(bool Success, string Message)> ResendContractBundleAsync(long adminUserId, long contractId, CancellationToken cancellationToken = default);
}
