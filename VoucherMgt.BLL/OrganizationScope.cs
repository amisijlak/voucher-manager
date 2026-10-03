using VoucherMgt.BLL.Core;
using VoucherMgt.DAL.Contracts;
using VoucherMgt.DAL.Entities;
using Microsoft.EntityFrameworkCore;

namespace VoucherMgt.BLL;

public interface IOrganizationScope
{
    Task<Organization> GetRequiredAsync(CancellationToken cancellationToken = default);
}

public sealed class OrganizationScope : IOrganizationScope
{
    private readonly ICurrentOrganizationContext _organizationContext;
    private readonly IRepository _repository;

    public OrganizationScope(ICurrentOrganizationContext organizationContext, IRepository repository)
    {
        _organizationContext = organizationContext;
        _repository = repository;
    }

    public async Task<Organization> GetRequiredAsync(CancellationToken cancellationToken = default)
    {
        var code = _organizationContext.OrganizationCode;
        var organization = await _repository.Set<Organization>()
            .FirstOrDefaultAsync(o => o.Code == code, cancellationToken);

        if (organization is null)
        {
            throw new InvalidOperationException($"Organization '{code}' was not found.");
        }

        return organization;
    }
}
