using System.DirectoryServices;
using System.DirectoryServices.ActiveDirectory;

namespace AutodeskSoftwareManager.Services;

public record AdComputer(string Name, string Description, string OU);

public static class AdDiscovery
{
    public static async Task<List<AdComputer>> GetComputersAsync(string ldapFilter)
    {
        return await Task.Run(() =>
        {
            var computers = new List<AdComputer>();
            string domainPath = GetDomainLdapPath();

            using var entry   = new DirectoryEntry($"LDAP://{domainPath}");
            using var searcher = new DirectorySearcher(entry)
            {
                Filter   = ldapFilter,
                PageSize = 1000
            };
            searcher.PropertiesToLoad.Add("cn");
            searcher.PropertiesToLoad.Add("description");
            searcher.PropertiesToLoad.Add("distinguishedName");

            using var results = searcher.FindAll();
            foreach (SearchResult result in results)
            {
                var cn = result.Properties["cn"];
                if (cn.Count == 0 || cn[0] is not string name) continue;

                var desc    = result.Properties["description"];
                var dn      = result.Properties["distinguishedName"];
                string description = desc.Count > 0 && desc[0] is string d ? d : string.Empty;
                string ou          = ExtractOU(dn.Count > 0 && dn[0] is string s ? s : string.Empty);

                computers.Add(new AdComputer(name, description, ou));
            }

            computers.Sort((a, b) => string.Compare(a.Name, b.Name, StringComparison.OrdinalIgnoreCase));
            return computers;
        });
    }

    private static string GetDomainLdapPath()
    {
        try
        {
            return Domain.GetCurrentDomain().Name;
        }
        catch
        {
            return string.Empty;
        }
    }

    // Pull the first OU component from a distinguished name like
    // CN=PC01,OU=Engineering,OU=Workstations,DC=corp,DC=local → "Engineering"
    private static string ExtractOU(string dn)
    {
        if (string.IsNullOrEmpty(dn)) return string.Empty;
        foreach (var part in dn.Split(','))
        {
            var trimmed = part.Trim();
            if (trimmed.StartsWith("OU=", StringComparison.OrdinalIgnoreCase))
                return trimmed.Substring(3);
        }
        return string.Empty;
    }
}
