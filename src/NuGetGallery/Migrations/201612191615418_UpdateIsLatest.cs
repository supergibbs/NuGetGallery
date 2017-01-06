// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.
namespace NuGetGallery.Migrations
{
    public partial class UpdateIsLatest : SqlResourceMigration
    {
        public UpdateIsLatest() :
            base (new []
            {
                // keeping SQL compliant with both SqlAzure and SQL 2012 (localdb) for self host
                "NuGetGallery.Infrastructure.Programmability.Types.PackageVersionHierarchyTableType.sql",
                "NuGetGallery.Infrastructure.Programmability.Functions.SplitVersion.sql",
                "NuGetGallery.Infrastructure.Programmability.Procedures.OrderPackagesByVersion.sql",
                "NuGetGallery.Infrastructure.Programmability.Procedures.UpdateIsLatestFlags.sql",
                "NuGetGallery.Infrastructure.Programmability.Triggers.UpdateIsLatestFlagsTriggers.sql"
            })
        {
        }

    }
}
