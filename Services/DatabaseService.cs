using AutodeskSoftwareManager.Data;
using AutodeskSoftwareManager.Models;
using Dapper;

namespace AutodeskSoftwareManager.Services;

public class DatabaseService
{
    // ── Computers ────────────────────────────────────────────────────────────

    public IEnumerable<Computer> GetAllComputers()
    {
        using var conn = Database.Open();
        return conn.Query<Computer>(@"
            SELECT
                c.id Id, c.name Name, c.description Description, c.ou OU,
                c.notes Notes, c.is_online IsOnline,
                c.logged_in_user LoggedInUser,
                c.last_seen LastSeen, c.last_scan LastScan,
                COALESCE(s.product_count, 0) ProductCount,
                COALESCE(s.outdated_count, 0) OutdatedCount
            FROM computers c
            LEFT JOIN (
                SELECT computer_id,
                       COUNT(*)                        AS product_count,
                       SUM(CASE WHEN latest_version != ''
                                 AND display_version != ''
                                 AND display_version < latest_version THEN 1 ELSE 0 END)
                                                       AS outdated_count
                FROM installed_software
                GROUP BY computer_id
            ) s ON s.computer_id = c.id
            ORDER BY c.name;");
    }

    // Returns true if inserted, false if updated.
    public bool UpsertComputer(Computer c)
    {
        using var conn = Database.Open();
        var existing = conn.ExecuteScalar<int>(
            "SELECT COUNT(*) FROM computers WHERE name = @Name COLLATE NOCASE;", c);

        if (existing == 0)
        {
            conn.Execute(@"
                INSERT INTO computers (name, description, ou, notes, is_online, logged_in_user, last_seen, last_scan)
                VALUES (@Name,@Description,@OU,@Notes,@IsOnline,@LoggedInUser,@LastSeen,@LastScan);", c);
            c.Id = (int)conn.ExecuteScalar<long>("SELECT last_insert_rowid();");
            return true;
        }

        conn.Execute(@"
            UPDATE computers
            SET description=@Description, ou=@OU, is_online=@IsOnline,
                logged_in_user=@LoggedInUser, last_seen=@LastSeen, last_scan=@LastScan
            WHERE name=@Name COLLATE NOCASE;", c);
        c.Id = conn.ExecuteScalar<int>(
            "SELECT id FROM computers WHERE name=@Name COLLATE NOCASE;", c);
        return false;
    }

    public void UpdateOnlineStatus(string computerName, bool isOnline, string loggedInUser, DateTime lastSeen)
    {
        using var conn = Database.Open();
        conn.Execute(@"
            UPDATE computers
            SET is_online=@o, logged_in_user=@u, last_seen=@s
            WHERE name=@n COLLATE NOCASE;",
            new { o = isOnline ? 1 : 0, u = loggedInUser, s = lastSeen.ToString("o"), n = computerName });
    }

    public void UpdateLastScan(int computerId)
    {
        using var conn = Database.Open();
        conn.Execute("UPDATE computers SET last_scan=@s WHERE id=@id;",
            new { s = DateTime.Now.ToString("o"), id = computerId });
    }

    public void DeleteComputer(int id)
    {
        using var conn = Database.Open();
        conn.Execute("DELETE FROM computers WHERE id=@id;", new { id });
    }

    // ── Product Catalog ───────────────────────────────────────────────────────

    public IEnumerable<ProductCatalog> GetCatalog()
    {
        using var conn = Database.Open();
        return conn.Query<ProductCatalog>(@"
            SELECT id Id, product_family ProductFamily,
                   registry_hint RegistryHint, latest_version LatestVersion
            FROM product_catalog ORDER BY product_family;");
    }

    // ── Installed Software ────────────────────────────────────────────────────

    public IEnumerable<InstalledProduct> GetAllInstalled()
    {
        using var conn = Database.Open();
        var products = conn.Query<InstalledProduct>(@"
            SELECT
                s.id Id, s.computer_id ComputerId, s.catalog_id CatalogId,
                c.name ComputerName, c.ou ComputerOU,
                s.display_name DisplayName, s.product_family ProductFamily,
                s.display_version DisplayVersion, s.latest_version LatestVersion,
                s.install_date InstallDate, s.install_location InstallLocation,
                s.uninstall_key UninstallKey, s.scanned_at ScannedAt
            FROM installed_software s
            JOIN computers c ON c.id = s.computer_id
            ORDER BY c.name, s.display_name;").ToList();

        foreach (var p in products)
            p.IsOutdated = ComputeIsOutdated(p.DisplayVersion, p.LatestVersion);

        return products;
    }

    public IEnumerable<InstalledProduct> GetInstalledForComputer(int computerId)
    {
        using var conn = Database.Open();
        var products = conn.Query<InstalledProduct>(@"
            SELECT
                s.id Id, s.computer_id ComputerId, s.catalog_id CatalogId,
                c.name ComputerName, c.ou ComputerOU,
                s.display_name DisplayName, s.product_family ProductFamily,
                s.display_version DisplayVersion, s.latest_version LatestVersion,
                s.install_date InstallDate, s.install_location InstallLocation,
                s.uninstall_key UninstallKey, s.scanned_at ScannedAt
            FROM installed_software s
            JOIN computers c ON c.id = s.computer_id
            WHERE s.computer_id = @id
            ORDER BY s.display_name;", new { id = computerId }).ToList();

        foreach (var p in products)
            p.IsOutdated = ComputeIsOutdated(p.DisplayVersion, p.LatestVersion);

        return products;
    }

    private static bool ComputeIsOutdated(string installed, string latest)
    {
        if (string.IsNullOrEmpty(installed) || string.IsNullOrEmpty(latest)) return false;
        if (!Version.TryParse(installed, out var v1)) return false;
        if (!Version.TryParse(latest,    out var v2)) return false;
        return v1 < v2;
    }

    public void ReplaceInstalledSoftware(int computerId, IEnumerable<InstalledProduct> products)
    {
        using var conn = Database.Open();
        using var tx = conn.BeginTransaction();
        conn.Execute("DELETE FROM installed_software WHERE computer_id=@id;",
            new { id = computerId }, tx);

        var now = DateTime.Now.ToString("o");
        foreach (var p in products)
        {
            conn.Execute(@"
                INSERT INTO installed_software
                    (computer_id, catalog_id, display_name, product_family,
                     display_version, latest_version, install_date,
                     install_location, uninstall_key, scanned_at)
                VALUES
                    (@ComputerId,@CatalogId,@DisplayName,@ProductFamily,
                     @DisplayVersion,@LatestVersion,@InstallDate,
                     @InstallLocation,@UninstallKey,@ScannedAt);",
                new
                {
                    p.ComputerId, p.CatalogId, p.DisplayName, p.ProductFamily,
                    p.DisplayVersion, p.LatestVersion, p.InstallDate,
                    p.InstallLocation, p.UninstallKey, ScannedAt = now
                }, tx);
        }
        tx.Commit();
    }

    // ── Deployment Packages ───────────────────────────────────────────────────

    public IEnumerable<DeploymentPackage> GetPackages(bool activeOnly = false)
    {
        using var conn = Database.Open();
        var where = activeOnly ? "WHERE is_active=1" : "";
        return conn.Query<DeploymentPackage>($@"
            SELECT id Id, name Name, product_family ProductFamily,
                   target_version TargetVersion, installer_path InstallerPath,
                   silent_args SilentArgs, notes Notes,
                   created_at CreatedAt, is_active IsActive
            FROM deployment_packages {where}
            ORDER BY name;");
    }

    public int InsertPackage(DeploymentPackage p)
    {
        using var conn = Database.Open();
        conn.Execute(@"
            INSERT INTO deployment_packages
                (name,product_family,target_version,installer_path,silent_args,notes,created_at,is_active)
            VALUES
                (@Name,@ProductFamily,@TargetVersion,@InstallerPath,@SilentArgs,@Notes,@CreatedAt,@IsActive);",
            new
            {
                p.Name, p.ProductFamily, p.TargetVersion, p.InstallerPath,
                p.SilentArgs, p.Notes, CreatedAt = DateTime.Now.ToString("o"), p.IsActive
            });
        return (int)conn.ExecuteScalar<long>("SELECT last_insert_rowid();");
    }

    public void UpdatePackage(DeploymentPackage p)
    {
        using var conn = Database.Open();
        conn.Execute(@"
            UPDATE deployment_packages
            SET name=@Name, product_family=@ProductFamily, target_version=@TargetVersion,
                installer_path=@InstallerPath, silent_args=@SilentArgs,
                notes=@Notes, is_active=@IsActive
            WHERE id=@Id;", p);
    }

    public void DeletePackage(int id)
    {
        using var conn = Database.Open();
        conn.Execute("DELETE FROM deployment_packages WHERE id=@id;", new { id });
    }

    // ── Deployment History ────────────────────────────────────────────────────

    public IEnumerable<DeploymentRecord> GetHistory(int limit = 500)
    {
        using var conn = Database.Open();
        return conn.Query<DeploymentRecord>($@"
            SELECT id Id, package_id PackageId, computer_id ComputerId,
                   package_name PackageName, computer_name ComputerName,
                   computer_ou ComputerOU, target_version TargetVersion,
                   started_at StartedAt, finished_at FinishedAt,
                   success Success, message Message
            FROM deployment_history
            ORDER BY started_at DESC
            LIMIT {limit};");
    }

    public int InsertDeploymentRecord(DeploymentRecord r)
    {
        using var conn = Database.Open();
        conn.Execute(@"
            INSERT INTO deployment_history
                (package_id,computer_id,package_name,computer_name,computer_ou,
                 target_version,started_at,finished_at,success,message)
            VALUES
                (@PackageId,@ComputerId,@PackageName,@ComputerName,@ComputerOU,
                 @TargetVersion,@StartedAt,@FinishedAt,@Success,@Message);",
            new
            {
                r.PackageId, r.ComputerId, r.PackageName, r.ComputerName, r.ComputerOU,
                r.TargetVersion,
                StartedAt  = r.StartedAt,
                FinishedAt = r.FinishedAt,
                Success    = r.Success ? 1 : 0,
                r.Message
            });
        return (int)conn.ExecuteScalar<long>("SELECT last_insert_rowid();");
    }

    public void FinalizeDeploymentRecord(int id, bool success, string message)
    {
        using var conn = Database.Open();
        conn.Execute(@"
            UPDATE deployment_history
            SET finished_at=@f, success=@s, message=@m
            WHERE id=@id;",
            new { f = DateTime.Now.ToString("o"), s = success ? 1 : 0, m = message, id });
    }

    // ── App Settings ──────────────────────────────────────────────────────────

    public string GetSetting(string key, string defaultValue = "")
    {
        using var conn = Database.Open();
        return conn.ExecuteScalar<string>(
            "SELECT value FROM app_settings WHERE key=@key;", new { key }) ?? defaultValue;
    }

    public void SetSetting(string key, string value)
    {
        using var conn = Database.Open();
        conn.Execute(
            "INSERT INTO app_settings(key,value) VALUES(@key,@value) ON CONFLICT(key) DO UPDATE SET value=excluded.value;",
            new { key, value });
    }

    // ── Stats ─────────────────────────────────────────────────────────────────

    public (int Total, int Online, int Offline, int Scanned, int Outdated) GetComputerStats()
    {
        using var conn = Database.Open();
        var total    = conn.ExecuteScalar<int>("SELECT COUNT(*) FROM computers;");
        var online   = conn.ExecuteScalar<int>("SELECT COUNT(*) FROM computers WHERE is_online=1;");
        var scanned  = conn.ExecuteScalar<int>("SELECT COUNT(*) FROM computers WHERE last_scan IS NOT NULL;");
        var outdated = conn.ExecuteScalar<int>(@"
            SELECT COUNT(DISTINCT computer_id) FROM installed_software
            WHERE latest_version != '' AND display_version != ''
              AND display_version < latest_version;");
        return (total, online, total - online, scanned, outdated);
    }
}
