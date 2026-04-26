using System.IO;
using Dapper;
using Microsoft.Data.Sqlite;

namespace AutodeskSoftwareManager.Data;

public static class Database
{
    private static string _connectionString = string.Empty;

    public static string DbPath { get; private set; } = string.Empty;

    public static void Initialize(string dbPath)
    {
        DbPath = dbPath;
        _connectionString = $"Data Source={dbPath};";

        Directory.CreateDirectory(Path.GetDirectoryName(dbPath)!);

        using var conn = Open();
        conn.Execute("PRAGMA journal_mode=WAL;");
        conn.Execute("PRAGMA foreign_keys=ON;");
        ApplySchema(conn);
        SeedCatalog(conn);
    }

    public static SqliteConnection Open()
    {
        var conn = new SqliteConnection(_connectionString);
        conn.Open();
        return conn;
    }

    private static void ApplySchema(SqliteConnection conn)
    {
        conn.Execute(@"
            CREATE TABLE IF NOT EXISTS computers (
                id           INTEGER PRIMARY KEY AUTOINCREMENT,
                name         TEXT    NOT NULL UNIQUE COLLATE NOCASE,
                description  TEXT    NOT NULL DEFAULT '',
                ou           TEXT    NOT NULL DEFAULT '',
                notes        TEXT    NOT NULL DEFAULT '',
                is_online    INTEGER NOT NULL DEFAULT 0,
                logged_in_user TEXT  NOT NULL DEFAULT '',
                last_seen    TEXT,
                last_scan    TEXT
            );

            CREATE TABLE IF NOT EXISTS product_catalog (
                id              INTEGER PRIMARY KEY AUTOINCREMENT,
                product_family  TEXT    NOT NULL UNIQUE,
                registry_hint   TEXT    NOT NULL DEFAULT '',
                latest_version  TEXT    NOT NULL DEFAULT ''
            );

            CREATE TABLE IF NOT EXISTS installed_software (
                id               INTEGER PRIMARY KEY AUTOINCREMENT,
                computer_id      INTEGER NOT NULL REFERENCES computers(id) ON DELETE CASCADE,
                catalog_id       INTEGER REFERENCES product_catalog(id),
                display_name     TEXT    NOT NULL,
                product_family   TEXT    NOT NULL DEFAULT '',
                display_version  TEXT    NOT NULL DEFAULT '',
                latest_version   TEXT    NOT NULL DEFAULT '',
                install_date     TEXT    NOT NULL DEFAULT '',
                install_location TEXT    NOT NULL DEFAULT '',
                uninstall_key    TEXT    NOT NULL DEFAULT '',
                scanned_at       TEXT    NOT NULL
            );

            CREATE INDEX IF NOT EXISTS idx_installed_computer
                ON installed_software(computer_id);

            CREATE TABLE IF NOT EXISTS deployment_packages (
                id              INTEGER PRIMARY KEY AUTOINCREMENT,
                name            TEXT    NOT NULL,
                product_family  TEXT    NOT NULL DEFAULT '',
                target_version  TEXT    NOT NULL DEFAULT '',
                installer_path  TEXT    NOT NULL,
                silent_args     TEXT    NOT NULL DEFAULT '/qn /norestart',
                notes           TEXT    NOT NULL DEFAULT '',
                created_at      TEXT    NOT NULL,
                is_active       INTEGER NOT NULL DEFAULT 1
            );

            CREATE TABLE IF NOT EXISTS deployment_history (
                id           INTEGER PRIMARY KEY AUTOINCREMENT,
                package_id   INTEGER NOT NULL REFERENCES deployment_packages(id),
                computer_id  INTEGER NOT NULL REFERENCES computers(id),
                package_name TEXT    NOT NULL DEFAULT '',
                computer_name TEXT   NOT NULL DEFAULT '',
                computer_ou  TEXT    NOT NULL DEFAULT '',
                target_version TEXT  NOT NULL DEFAULT '',
                started_at   TEXT    NOT NULL,
                finished_at  TEXT,
                success      INTEGER NOT NULL DEFAULT 0,
                message      TEXT    NOT NULL DEFAULT ''
            );

            CREATE INDEX IF NOT EXISTS idx_history_started
                ON deployment_history(started_at DESC);

            CREATE TABLE IF NOT EXISTS app_settings (
                key   TEXT PRIMARY KEY,
                value TEXT NOT NULL DEFAULT ''
            );
        ");
    }

    private static void SeedCatalog(SqliteConnection conn)
    {
        var existing = conn.ExecuteScalar<int>("SELECT COUNT(*) FROM product_catalog;");
        if (existing > 0) return;

        var catalog = new (string Family, string Hint, string Latest)[]
        {
            ("AutoCAD",                        "AutoCAD",                        "25.1.0.0"),
            ("AutoCAD LT",                     "AutoCAD LT",                     "25.1.0.0"),
            ("AutoCAD Architecture",           "AutoCAD Architecture",           "25.1.0.0"),
            ("AutoCAD Civil 3D",               "Civil 3D",                       "25.0.60.0"),
            ("AutoCAD Electrical",             "AutoCAD Electrical",             "25.1.0.0"),
            ("AutoCAD Mechanical",             "AutoCAD Mechanical",             "25.1.0.0"),
            ("AutoCAD MEP",                    "AutoCAD MEP",                    "25.1.0.0"),
            ("AutoCAD Plant 3D",               "AutoCAD Plant 3D",               "25.1.0.0"),
            ("Revit",                          "Revit",                          "25.0.0.900"),
            ("Inventor",                       "Inventor",                       "29.0.0.0"),
            ("Inventor LT",                    "Inventor LT",                    "29.0.0.0"),
            ("3ds Max",                        "3ds Max",                        "27.2.0.0"),
            ("Maya",                           "Maya",                           "25.1.0.0"),
            ("Navisworks Manage",              "Navisworks Manage",              "25.0.0.0"),
            ("Navisworks Simulate",            "Navisworks Simulate",            "25.0.0.0"),
            ("Navisworks Freedom",             "Navisworks Freedom",             "25.0.0.0"),
            ("InfraWorks",                     "InfraWorks",                     "2025.0.0.1"),
            ("ReCap Pro",                      "ReCap",                          "23.2.0.0"),
            ("Robot Structural Analysis",      "Robot Structural",               "25.0.0.0"),
            ("Advance Steel",                  "Advance Steel",                  "25.0.0.0"),
            ("Vault Professional",             "Vault",                          "2025.0.0.0"),
            ("Fusion 360",                     "Fusion 360",                     ""),
            ("MotionBuilder",                  "MotionBuilder",                  "2025.0.0.0"),
            ("Mudbox",                         "Mudbox",                         "2025.0.0.0"),
            ("Arnold Renderer",                "Arnold",                         "5.3.1.0"),
            ("Autodesk Desktop Licensing",     "Autodesk Desktop Licensing",     "16.3.0.15409"),
            ("Autodesk Access",                "Autodesk Access",                ""),
            ("Autodesk Single Sign On",        "Autodesk Single Sign On",        ""),
        };

        using var tx = conn.BeginTransaction();
        foreach (var (family, hint, latest) in catalog)
        {
            conn.Execute(
                "INSERT OR IGNORE INTO product_catalog (product_family, registry_hint, latest_version) VALUES (@f,@h,@l);",
                new { f = family, h = hint, l = latest }, tx);
        }

        // Seed default app settings
        var defaults = new (string Key, string Value)[]
        {
            ("MaxScanThreads",   "20"),
            ("MaxDeployThreads", "10"),
            ("PingTimeoutMs",    "1500"),
            ("DeployTimeoutSec", "300"),
            ("DefaultSilentArgs","/qn /norestart"),
            ("AdFilter",         "(&(objectCategory=computer)(operatingSystem=Windows*))"),
        };

        foreach (var (key, val) in defaults)
        {
            conn.Execute(
                "INSERT OR IGNORE INTO app_settings (key, value) VALUES (@k,@v);",
                new { k = key, v = val }, tx);
        }

        tx.Commit();
    }
}
