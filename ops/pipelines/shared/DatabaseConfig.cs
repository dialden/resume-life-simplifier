using System.Text.RegularExpressions;

namespace DialDen.RLS.Pipelines;

public sealed record DatabaseConfig(
    string Version,
    string Edition,
    string Tier,
    string StorageType,
    int StorageSizeGb,
    string AvailabilityType,
    bool StorageAutoIncrease,
    bool BackupsEnabled,
    bool PointInTimeRecoveryEnabled,
    bool DeletionProtection)
{
    public void Validate()
    {
        if (!Regex.IsMatch(Version ?? "", @"\APOSTGRES_[0-9]+\z") ||
            Edition is not ("ENTERPRISE" or "ENTERPRISE_PLUS") ||
            string.IsNullOrWhiteSpace(Tier) ||
            StorageType is not ("HDD" or "SSD") ||
            StorageSizeGb < 10 ||
            AvailabilityType is not ("ZONAL" or "REGIONAL"))
        {
            throw new InvalidOperationException(
                $"The googleCloud.database section of {PipelineWorkspace.ConfigFile} requires a PostgreSQL version, " +
                "edition, tier, HDD or SSD storage of at least 10 GB, and ZONAL or REGIONAL availability.");
        }
        if (PointInTimeRecoveryEnabled && !BackupsEnabled)
        {
            throw new InvalidOperationException("Database point-in-time recovery requires backups to be enabled.");
        }
    }
}
