﻿using System.Collections.Generic;
using System.DirectoryServices.Protocols;
using CommonLib.LDAPQuery;

namespace CommonLib.Output
{
    public class ContainerProcessor
    {
        public static async IAsyncEnumerable<TypedPrincipal> GetContainerChildObjects(SearchResultEntry entry)
        {
            var filter = new LDAPFilter().AddComputers().AddUsers().AddGroups().AddOUs().AddContainers();
            foreach (var childEntry in LDAPUtils.QueryLDAP(filter.GetFilter(), SearchScope.OneLevel,
                CommonProperties.ObjectID, Helpers.DistinguishedNameToDomain(entry.DistinguishedName), adsPath: entry.DistinguishedName))
            {
                var dn = childEntry.DistinguishedName.ToUpper();
                
                if (dn.Contains("CN=SYSTEM") || dn.Contains("CN=POLICIES") || dn.Contains("CN=PROGRAM DATA"))
                    continue;

                var id = childEntry.GetObjectIdentifier();
                if (id == null)
                    continue;

                var res = LDAPUtils.ResolveIDAndType(id, Helpers.DistinguishedNameToDomain(childEntry.DistinguishedName));
                if (res == null)
                    continue;
                yield return res;
            }
        }

        public static async IAsyncEnumerable<GPLink> ReadContainerGPLinks(SearchResultEntry entry)
        {
            var gpLinkProp = entry.GetProperty("gplink");
            if (gpLinkProp == null)
                yield break;

            foreach (var link in Helpers.SplitGPLinkProperty(gpLinkProp, true))
            {
                var enforced = link.Status.Equals("2");

                var res = await LDAPUtils.ResolveDistinguishedName(link.DistinguishedName);
                    
                if (res == null)
                    continue;

                yield return new GPLink
                {
                    GUID = res.ObjectIdentifier,
                    IsEnforced = enforced
                };
            }
        }
        
        public static bool ReadBlocksInheritance(SearchResultEntry entry)
        {
            var opts = entry.GetProperty("gpoptions");
            return opts is "1";
        }
    }
}