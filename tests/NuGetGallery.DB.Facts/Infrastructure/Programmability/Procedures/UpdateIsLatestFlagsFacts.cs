// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.
using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Data.SqlClient;
using System.Linq;
using System.Threading.Tasks;
using NuGet.Versioning;
using Xunit;

namespace NuGetGallery.Migrations
{
    // note: NuGetGallery.DB.Facts not yet included in CI / build.ps1
    public class UpdateIsLatestFlagsFacts
    {
        public static IEnumerable<object[]> UpdateIsLatestFlags_VersionPartNumeric
        {
            get
            {
                yield return new object[] { /*latest*/"10.0.0", /*latestStable*/"10.0.0",
                    new Package[]
                    {
                        InitializePackage("1.1.1"),
                        InitializePackage("1.0.0"),
                        InitializePackage("1.10.0"),
                        InitializePackage("1.0.1"),
                        InitializePackage("10.0.0"),
                        InitializePackage("1.1.0"),
                        InitializePackage("1.0.10")
                    }
                };
            }
        }

        public static IEnumerable<object[]> UpdateIsLatestFlags_LabelsPartAlphaNumeric
        {
            get
            {
                yield return new object[] { /*latest*/"2.0.0-x.y.z", /*latestStable*/"1.0.0",
                    new Package[]
                    {
                        InitializePackage("2.0.0-x.y.10"),
                        InitializePackage("2.0.0-X.y.2"),
                        InitializePackage("2.0.0-x"),
                        InitializePackage("1.0.0"),
                        InitializePackage("2.0.0-X.y.z"),
                        InitializePackage("2.0.0-beta"),
                        InitializePackage("2.0.0-beta-2")
                    }
                };
            }
        }

        public static IEnumerable<object[]> UpdateIsLatestFlags_LabelsPartNumeric
        {
            get
            {
                yield return new object[] { /*latest*/"1.0.0-rc.2", /*latestStable*/"",
                    new Package[]
                    {
                        InitializePackage("1.0.0-rc.1"),
                        InitializePackage("1.0.0-rc.2")
                    }
                };
            }
        }

        public static IEnumerable<object[]> UpdateIsLatestFlags_LabelsPartPrefersMore
        {
            get
            {
                yield return new object[] { /*latest*/"1.0.0-x.y", /*latestStable*/"",
                    new Package[]
                    {
                        InitializePackage("1.0.0-x"),
                        InitializePackage("1.0.0-x.y")
                    }
                };
            }
        }

        public static IEnumerable<object[]> UpdateIsLatestFlags_LabelsPartPrefersNone
        {
            get
            {
                yield return new object[] { /*latest*/"1.0.0", /*latestStable*/"1.0.0",
                    new Package[] {
                        InitializePackage("1.0.0-rc"),
                        InitializePackage("1.0.0")
                    }
                };
            }
        }

        public static IEnumerable<object[]> UpdateIsLatestFlags_LabelsPartPrefersNumeric
        {
            get
            {
                yield return new object[] { /*latest*/"1.0.0-1x", /*latestStable*/"",
                    new Package[]
                    {
                        InitializePackage("1.0.0-21"),
                        InitializePackage("1.0.0-1x")
                    }
                };
            }
        }

        public static IEnumerable<object[]> UpdateIsLatestFlags_BuildPartAlphaNumeric
        {
            get
            {
                // review: build part (metadata) excluded from IsLatest calculation
                yield return new object[] { /*latest*/"1.0.0+abc", /*latestStable*/"",
                    new Package[] {
                        InitializePackage("1.0.0+Ab", listed: true, deleted: false),
                        InitializePackage("1.0.0+aA", listed: true, deleted: false),
                        InitializePackage("1.0.0+abc", listed: true, deleted: false)
                    }
                };
            }
        }

        public static IEnumerable<object[]> UpdateIsLatestFlags_IgnoresUnlistedAndDeleted
        {
            get
            {
                yield return new object[] { /*latest*/"1.0.0", /*latestStable*/"1.0.0",
                    new Package[] {
                        InitializePackage("2.0.0", listed: false, deleted: false),
                        InitializePackage("1.0.0", listed: true,  deleted: false),
                        InitializePackage("3.0.0", listed: true,  deleted: true)
                    }
                };
            }
        }

        [Theory]
        [MemberData("UpdateIsLatestFlags_VersionPartNumeric")]
        [MemberData("UpdateIsLatestFlags_LabelsPartAlphaNumeric")]
        [MemberData("UpdateIsLatestFlags_LabelsPartNumeric")]
        [MemberData("UpdateIsLatestFlags_LabelsPartPrefersMore")]
        [MemberData("UpdateIsLatestFlags_LabelsPartPrefersNone")]
        [MemberData("UpdateIsLatestFlags_LabelsPartPrefersNumeric")]
        [MemberData("UpdateIsLatestFlags_BuildPartAlphaNumeric")]
        [MemberData("UpdateIsLatestFlags_IgnoresUnlistedAndDeleted")]
        public async void UpdateIsLatestFlagsOrdersBySemanticVersion(string latest, string latestStable, Package[] packages)
        {
            var testName = "UpdateIsLatestFlagsOrdersBySemanticVersion";
            List<OrderPackagesByVersionResult> result;

            using (var context = new EntitiesContext())
            {
                try
                {
                    // Arrange & Act - UpdateIsLatest via triggers
                    var registrationKey = await PopulatePackagesAsync(context, testName, packages);
                    
                    // Arrange & Act - OrderPackagesByVersion
                    result = await OrderPackagesByVersionAsync(context, registrationKey);

                    // Assert - UpdateIsLatest via triggers
                    // Triggers on PopulatePackagesAsync, but verifying after OrderPackagesByVersion to avoid race condition
                    var registration = await GetPackageRegistrationAsync(context, testName);

                    // Disabled - need to investigate issue with triggers
                    //var dbLatest = registration.Packages.Where(p => p.IsLatest).ToList();
                    //Assert.Equal(1, dbLatest.Count);
                    //Assert.Equal(latest, dbLatest.First().Version, ignoreCase: true);

                    //var dbLatestStable = registration.Packages.Where(p => p.IsLatestStable).ToList();
                    //Assert.Equal(1, dbLatestStable.Count);
                    //Assert.Equal(latestStable, dbLatestStable.First().Version, ignoreCase: true);

                    // Assert - OrderPackagesByVersion
                    AssertIsLatest(latest, result);
                    AssertIsLatestStable(latestStable, result);
                    AssertDbOrderMatchesManagedOrder(packages, result);
                }
                finally
                {
                    await DeletePackageRegistrationAsync(context, testName);
                }
            }
        }

        private void AssertIsLatest(string expectedVersion, IEnumerable<OrderPackagesByVersionResult> actualSet)
        {
            var isLatest = actualSet.Where(pv => pv.IsLatest);
            if (string.IsNullOrEmpty(expectedVersion))
            {
                Assert.Equal(0, isLatest.Count());
            }
            else
            {
                Assert.Equal(1, isLatest.Count());
                Assert.Equal(expectedVersion, isLatest.First().Version, ignoreCase: true);
            }
        }

        private void AssertIsLatestStable(string expectedVersion, IEnumerable<OrderPackagesByVersionResult> actualSet)
        {
            var isLatestStable = actualSet.Where(pv => pv.IsLatestStable);
            if (string.IsNullOrEmpty(expectedVersion))
            {
                Assert.Equal(0, isLatestStable.Count());
            }
            else
            {
                var latest = isLatestStable.First();

                Assert.Equal(1, isLatestStable.Count());
                Assert.Equal(expectedVersion, latest.Version);
                Assert.True(string.IsNullOrEmpty(latest.BuildPart));
                Assert.True(string.IsNullOrEmpty(latest.LabelsPart));
            }
        }

        private void AssertDbOrderMatchesManagedOrder(IEnumerable<Package> input, List<OrderPackagesByVersionResult> actualSet)
        {
            input = input.Where(p => p.Listed && !p.Deleted).ToList();
            Assert.Equal(input.Count(), actualSet.Count);

            var expected = input.OrderByDescending(p => new NuGetVersion(p.Version),
                VersionComparer.VersionReleaseMetadata).ToList();

            for (int i = 0; i < actualSet.Count; i++)
            {
                var expectedNormalized = NuGetVersionNormalizer.Normalize(expected[i].Version);
                var actualNormalized = NuGetVersionNormalizer.Normalize(actualSet[i].Version);
                Assert.Equal(expectedNormalized, actualNormalized, ignoreCase: true);
            }
        }

        private async Task<List<OrderPackagesByVersionResult>> OrderPackagesByVersionAsync(EntitiesContext context, int registrationKey)
        {
            Assert.True(registrationKey > 0);

            return await context.Database.SqlQuery<OrderPackagesByVersionResult>(
                "exec OrderPackagesByVersion @p1", new SqlParameter("p1", registrationKey)
                ).ToListAsync();
        }

        private static Package InitializePackage(string version, bool listed = true, bool deleted = false)
        {
            bool preRelease = false;
            string normalizedVersion = version;
            try
            {
                var semVersion = NuGetVersion.Parse(version);
                preRelease = semVersion.IsPrerelease;
                normalizedVersion = semVersion.ToNormalizedString();
            }
            catch (Exception) { }
            return new Package()
            {
                Version = version,
                Listed = listed,
                Deleted = deleted,
                NormalizedVersion = normalizedVersion,
                IsPrerelease = preRelease,
                Hash = "Ignore"
            };
        }

        private static async Task<int> PopulatePackagesAsync(EntitiesContext context, string testName, IEnumerable<Package> packages)
        {
            await DeletePackageRegistrationAsync(context, testName);

            var user = await EnsureUserAsync(context);
            var registration = new PackageRegistration()
            {
                Id = testName
            };
            registration.Owners.Add(user);
            context.PackageRegistrations.Add(registration);
            await context.SaveChangesAsync();
            foreach (var package in packages)
            {
                package.User = user;
                registration.Packages.Add(package);
                await context.SaveChangesAsync();
            }
            return registration.Key;
        }

        private static async Task DeletePackageRegistrationAsync(EntitiesContext context, string testName)
        {
            var registration = await GetPackageRegistrationAsync(context, testName);
            if (registration != null)
            {
                foreach (var p in registration.Packages)
                {
                    await context.Database.ExecuteSqlCommandAsync(
                        "DELETE pa FROM PackageAuthors pa JOIN Packages p ON p.[Key] = pa.PackageKey WHERE p.[Key] = @key",
                        new SqlParameter("@key", p.Key));
                    await context.Database.ExecuteSqlCommandAsync(
                        "DELETE pd FROM PackageDependencies pd JOIN Packages p ON p.[Key] = pd.PackageKey WHERE p.[Key] = @key",
                        new SqlParameter("@key", p.Key));
                    await context.Database.ExecuteSqlCommandAsync(
                        "DELETE pf FROM PackageFrameworks pf JOIN Packages p ON p.[Key] = pf.Package_Key WHERE p.[Key] = @key",
                        new SqlParameter("@key", p.Key));
                }
                await context.Database.ExecuteSqlCommandAsync(
                    "DELETE p FROM Packages p JOIN PackageRegistrations pr ON pr.[Key] = p.PackageRegistrationKey WHERE pr.[Key] = @key",
                    new SqlParameter("@key", registration.Key));
                await context.Database.ExecuteSqlCommandAsync(
                    "DELETE po FROM PackageRegistrationOwners po JOIN PackageRegistrations pr ON pr.[Key] = po.PackageRegistrationKey WHERE pr.[Key] = @key",
                    new SqlParameter("@key", registration.Key));
                await context.Database.ExecuteSqlCommandAsync(
                    "DELETE FROM PackageRegistrations WHERE [Key] = @key",
                    new SqlParameter("@key", registration.Key));
            }
        }

        private static async Task<PackageRegistration> GetPackageRegistrationAsync(EntitiesContext context, string testName)
        {
            return await context.PackageRegistrations.Include(r => r.Packages).
                Where(r => r.Id.Equals(testName)).FirstOrDefaultAsync();
        }

        private static async Task<User> EnsureUserAsync(EntitiesContext context)
        {
            var user = await context.Users.Where(u => u.Username.Equals("testuser")).FirstOrDefaultAsync();
            if (user == null)
            {
                user = new User()
                {
                    EmailAddress = "testuser@example.com",
                    Username = "testuser"
                };
                context.Users.Add(user);
                await context.SaveChangesAsync();
            }
            return user;
        }

        // EF skips hierarchyId columns which aren't yet supported
        internal class OrderPackagesByVersionResult
        {
            public int Key { get; set; }
            public string Version { get; set; }
            public bool Listed { get; set; }
            public bool Deleted { get; set; }
            public string VersionPart { get; set; }
            //public object VersionPartHier { get; set; }
            public string LabelsPart { get; set; }
            public string LabelsPartHierStr { get; set; }
            //public object LabelsPartHier { get; set; }
            public string BuildPart { get; set; }
            public bool IsLatest { get; set; }
            public bool IsLatestStable { get; set; }
            public int VersionRank { get; set; }
        }
    }
}
