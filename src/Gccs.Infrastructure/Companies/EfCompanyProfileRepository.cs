using System.Text.Json;
using Gccs.Application.Companies;
using Gccs.Application.Security;
using Gccs.Domain.Common;
using Gccs.Domain.Companies;
using Gccs.Domain.Compliance;
using Gccs.Infrastructure.Persistence;
using Gccs.Infrastructure.Persistence.Models;
using Microsoft.EntityFrameworkCore;

namespace Gccs.Infrastructure.Companies;

public sealed class EfCompanyProfileRepository(GccsDbContext dbContext, ICurrentTenantContext tenantContext) : ICompanyProfileRepository
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    public async Task<CompanyProfileDto?> FindCurrentTenantProfileAsync(CancellationToken cancellationToken = default)
    {
        var entity = await QueryProfiles()
            .AsNoTracking()
            .SingleOrDefaultAsync(profile => profile.TenantId == tenantContext.TenantId, cancellationToken);

        return entity is null ? null : ToDto(entity);
    }

    public async Task<CompanyProfileDto> UpsertCurrentTenantProfileAsync(
        UpsertCompanyProfileRequest request,
        Guid actorUserId,
        CancellationToken cancellationToken = default)
    {
        var entity = await QueryProfiles()
            .SingleOrDefaultAsync(profile => profile.TenantId == tenantContext.TenantId, cancellationToken);
        var now = DateTimeOffset.UtcNow;
        var isNew = entity is null;

        if (entity is null)
        {
            entity = new CompanyProfileEntity
            {
                Id = Guid.NewGuid(),
                TenantId = tenantContext.TenantId,
                CreatedAt = now,
                CreatedByUserId = actorUserId
            };
            dbContext.CompanyProfiles.Add(entity);
        }
        else
        {
            entity.UpdatedAt = now;
            entity.UpdatedByUserId = actorUserId;
        }

        entity.LegalEntityName = request.LegalEntityName;
        entity.DoingBusinessAs = request.DoingBusinessAs;
        entity.Uei = request.Uei;
        entity.CageCode = request.CageCode;
        entity.SamRegistrationExpiresAt = request.SamRegistrationExpiresAt;
        entity.ContractorRole = request.ContractorRole;
        entity.ProductsAndServices = request.ProductsAndServices;
        entity.EmployeeRange = request.EmployeeRange;
        entity.RevenueRange = request.RevenueRange;
        entity.ItEnvironmentDescription = request.ItEnvironment.Description;
        entity.UsesExternalServiceProvider = request.ItEnvironment.UsesExternalServiceProvider;
        entity.ExternalServiceProviderName = request.ItEnvironment.ExternalServiceProviderName;
        entity.KeySystemsJson = JsonSerializer.Serialize(request.ItEnvironment.KeySystems, JsonOptions);
        entity.AgencyCustomersJson = JsonSerializer.Serialize(request.AgencyCustomers, JsonOptions);
        entity.DataHandlingPosture = request.DataHandlingPosture;
        if (isNew)
        {
            entity.NaicsCodes = request.NaicsCodes.Select(naics => CreateNaics(entity.Id, naics)).ToList();
            entity.Certifications = request.Certifications.Select(certification => CreateCertification(entity.Id, certification)).ToList();
            entity.Locations = request.Locations.Select(location => CreateLocation(entity.Id, location)).ToList();
        }
        else
        {
            SyncNaics(entity, request.NaicsCodes);
            SyncCertifications(entity, request.Certifications);
            SyncLocations(entity, request.Locations);
        }

        await SyncCertificationRenewalTasksAsync(request.Certifications, actorUserId, now, cancellationToken);

        try
        {
            await dbContext.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateConcurrencyException) when (dbContext.Database.ProviderName == "Microsoft.EntityFrameworkCore.InMemory")
        {
            dbContext.ChangeTracker.Clear();
            dbContext.CompanyProfiles.Add(entity);
            try
            {
                await dbContext.SaveChangesAsync(cancellationToken);
            }
            catch (ArgumentException)
            {
                // EF InMemory can report a false concurrency miss after a tracked aggregate update.
                // If the key already exists, return the composed aggregate used for the attempted save.
                dbContext.ChangeTracker.Clear();
            }
        }

        return ToDto(entity);
    }

    private IQueryable<CompanyProfileEntity> QueryProfiles() =>
        dbContext.CompanyProfiles
            .Include(profile => profile.NaicsCodes)
            .Include(profile => profile.Certifications)
            .Include(profile => profile.Locations);

    private static void SyncNaics(CompanyProfileEntity entity, IReadOnlyList<CompanyNaicsCodeDto> requested)
    {
        var requestedCodes = requested.Select(naics => naics.Code).ToHashSet(StringComparer.OrdinalIgnoreCase);
        foreach (var removed in entity.NaicsCodes.Where(naics => !requestedCodes.Contains(naics.Code)).ToArray())
        {
            entity.NaicsCodes.Remove(removed);
        }

        foreach (var requestedNaics in requested)
        {
            var existing = entity.NaicsCodes.FirstOrDefault(naics => string.Equals(naics.Code, requestedNaics.Code, StringComparison.OrdinalIgnoreCase));
            if (existing is null)
            {
                entity.NaicsCodes.Add(CreateNaics(entity.Id, requestedNaics));
                continue;
            }

            existing.Title = requestedNaics.Title;
            existing.IsPrimary = requestedNaics.IsPrimary;
            existing.SizeStandard = requestedNaics.SizeStandard;
            existing.QualifiesAsSmall = requestedNaics.QualifiesAsSmall;
            existing.LastCheckedAt = requestedNaics.LastCheckedAt;
        }
    }

    private static void SyncCertifications(CompanyProfileEntity entity, IReadOnlyList<CompanyCertificationDto> requested)
    {
        var requestedKeys = requested.Select(CertificationKey).ToHashSet(StringComparer.OrdinalIgnoreCase);
        foreach (var removed in entity.Certifications.Where(certification => !requestedKeys.Contains(CertificationKey(certification))).ToArray())
        {
            entity.Certifications.Remove(removed);
        }

        foreach (var requestedCertification in requested)
        {
            var key = CertificationKey(requestedCertification);
            var existing = entity.Certifications.FirstOrDefault(certification => string.Equals(CertificationKey(certification), key, StringComparison.OrdinalIgnoreCase));
            if (existing is null)
            {
                entity.Certifications.Add(CreateCertification(entity.Id, requestedCertification));
                continue;
            }

            existing.Status = requestedCertification.Status;
            existing.Issuer = requestedCertification.Issuer;
            existing.EffectiveAt = requestedCertification.EffectiveAt;
            existing.ExpiresAt = requestedCertification.ExpiresAt;
            existing.ReferenceNumber = requestedCertification.ReferenceNumber;
        }
    }

    private static void SyncLocations(CompanyProfileEntity entity, IReadOnlyList<CompanyLocationDto> requested)
    {
        var requestedNames = requested.Select(location => location.Name).ToHashSet(StringComparer.OrdinalIgnoreCase);
        foreach (var removed in entity.Locations.Where(location => !requestedNames.Contains(location.Name)).ToArray())
        {
            entity.Locations.Remove(removed);
        }

        foreach (var requestedLocation in requested)
        {
            var existing = entity.Locations.FirstOrDefault(location => string.Equals(location.Name, requestedLocation.Name, StringComparison.OrdinalIgnoreCase));
            if (existing is null)
            {
                entity.Locations.Add(CreateLocation(entity.Id, requestedLocation));
                continue;
            }

            existing.Street1 = requestedLocation.Street1;
            existing.Street2 = requestedLocation.Street2;
            existing.City = requestedLocation.City;
            existing.StateOrProvince = requestedLocation.StateOrProvince;
            existing.PostalCode = requestedLocation.PostalCode;
            existing.Country = requestedLocation.Country;
            existing.IsPlaceOfPerformance = requestedLocation.IsPlaceOfPerformance;
        }
    }

    private static CompanyNaicsCodeEntity CreateNaics(Guid profileId, CompanyNaicsCodeDto naics) =>
        new()
        {
            Id = Guid.NewGuid(),
            CompanyProfileId = profileId,
            Code = naics.Code,
            Title = naics.Title,
            IsPrimary = naics.IsPrimary,
            SizeStandard = naics.SizeStandard,
            QualifiesAsSmall = naics.QualifiesAsSmall,
            LastCheckedAt = naics.LastCheckedAt
        };

    private static CompanyCertificationEntity CreateCertification(Guid profileId, CompanyCertificationDto certification) =>
        new()
        {
            Id = certification.Id ?? Guid.NewGuid(),
            CompanyProfileId = profileId,
            Type = certification.Type,
            Status = certification.Status,
            Issuer = certification.Issuer,
            EffectiveAt = certification.EffectiveAt,
            ExpiresAt = certification.ExpiresAt,
            ReferenceNumber = certification.ReferenceNumber
        };

    private static CompanyLocationEntity CreateLocation(Guid profileId, CompanyLocationDto location) =>
        new()
        {
            Id = Guid.NewGuid(),
            CompanyProfileId = profileId,
            Name = location.Name,
            Street1 = location.Street1,
            Street2 = location.Street2,
            City = location.City,
            StateOrProvince = location.StateOrProvince,
            PostalCode = location.PostalCode,
            Country = location.Country,
            IsPlaceOfPerformance = location.IsPlaceOfPerformance
        };

    private static string CertificationKey(CompanyCertificationDto certification) =>
        $"{certification.Type}:{certification.Issuer}";

    private static string CertificationKey(CompanyCertificationEntity certification) =>
        $"{certification.Type}:{certification.Issuer}";

    private async Task SyncCertificationRenewalTasksAsync(
        IReadOnlyList<CompanyCertificationDto> certifications,
        Guid actorUserId,
        DateTimeOffset now,
        CancellationToken cancellationToken)
    {
        foreach (var certification in certifications.Where(ShouldCreateRenewalTask))
        {
            var title = $"Renew {FormatCertificationType(certification.Type)} certification";
            var exists = await dbContext.ComplianceTasks.AnyAsync(
                task =>
                    task.TenantId == tenantContext.TenantId &&
                    task.Type == ComplianceTaskType.Renewal &&
                    task.Title == title &&
                    task.DueAt == certification.ExpiresAt,
                cancellationToken);

            if (exists)
            {
                continue;
            }

            dbContext.ComplianceTasks.Add(new ComplianceTaskEntity
            {
                Id = Guid.NewGuid(),
                TenantId = tenantContext.TenantId,
                Title = title,
                Description = $"{FormatCertificationType(certification.Type)} certification issued by {certification.Issuer} expires on {certification.ExpiresAt:yyyy-MM-dd}.",
                Type = ComplianceTaskType.Renewal,
                Status = ComplianceTaskStatus.Open,
                RiskLevel = RiskLevel.Medium,
                OwnerFunction = "Compliance",
                DueAt = certification.ExpiresAt,
                CreatedAt = now,
                CreatedByUserId = actorUserId
            });
        }
    }

    private static bool ShouldCreateRenewalTask(CompanyCertificationDto certification)
    {
        if (certification.ExpiresAt is not { } expiresAt || certification.Status is CertificationStatus.Expired or CertificationStatus.Revoked)
        {
            return false;
        }

        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        return expiresAt >= today && expiresAt <= today.AddDays(90);
    }

    private static string FormatCertificationType(CertificationType certificationType) =>
        certificationType switch
        {
            CertificationType.EightA => "8(a)",
            CertificationType.Wosb => "WOSB",
            CertificationType.Edwosb => "EDWOSB",
            CertificationType.HubZone => "HUBZone",
            CertificationType.Sdvosb => "SDVOSB",
            CertificationType.Sdb => "SDB",
            _ => "custom"
        };

    private static CompanyProfileDto ToDto(CompanyProfileEntity entity) =>
        new(
            entity.Id,
            entity.TenantId,
            entity.LegalEntityName,
            entity.DoingBusinessAs,
            entity.Uei,
            entity.CageCode,
            entity.SamRegistrationExpiresAt,
            entity.NaicsCodes
                .OrderByDescending(naics => naics.IsPrimary)
                .ThenBy(naics => naics.Code)
                .Select(naics => new CompanyNaicsCodeDto(
                    naics.Code,
                    naics.Title,
                    naics.IsPrimary,
                    naics.SizeStandard,
                    naics.QualifiesAsSmall,
                    naics.LastCheckedAt))
                .ToArray(),
            entity.Certifications
                .OrderBy(certification => certification.Type)
                .Select(certification => new CompanyCertificationDto(
                    certification.Id,
                    certification.Type,
                    certification.Status,
                    certification.Issuer,
                    certification.EffectiveAt,
                    certification.ExpiresAt,
                    certification.ReferenceNumber))
                .ToArray(),
            ReadStringArray(entity.AgencyCustomersJson),
            entity.ContractorRole,
            entity.ProductsAndServices,
            entity.EmployeeRange,
            entity.RevenueRange,
            entity.Locations
                .OrderByDescending(location => location.IsPlaceOfPerformance)
                .ThenBy(location => location.Name)
                .Select(location => new CompanyLocationDto(
                    location.Name,
                    location.Street1,
                    location.Street2,
                    location.City,
                    location.StateOrProvince,
                    location.PostalCode,
                    location.Country,
                    location.IsPlaceOfPerformance))
                .ToArray(),
            new ItEnvironmentSummaryDto(
                entity.ItEnvironmentDescription,
                entity.UsesExternalServiceProvider,
                entity.ExternalServiceProviderName,
                ReadStringArray(entity.KeySystemsJson)),
            entity.DataHandlingPosture,
            0,
            false,
            new Dictionary<string, string[]>(),
            entity.CreatedAt,
            entity.UpdatedAt);

    private static IReadOnlyList<string> ReadStringArray(string json)
    {
        try
        {
            return JsonSerializer.Deserialize<string[]>(json, JsonOptions) ?? [];
        }
        catch (JsonException)
        {
            return [];
        }
    }
}
