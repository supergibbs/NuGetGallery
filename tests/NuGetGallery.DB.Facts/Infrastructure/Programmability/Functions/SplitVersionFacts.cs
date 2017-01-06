// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.
using System.Data.SqlClient;
using Xunit;

namespace NuGetGallery.Migrations
{
    public class SplitVersionFacts
    {
        [Theory]
        [InlineData("1.0.0", "1.0.0", "", "")]
        [InlineData("1.0.0-beta", "1.0.0", "beta", "")]
        [InlineData("1.0.0-beta+AA", "1.0.0", "beta", "AA")]
        [InlineData("1.0.0-beta.X.y.5.77.0+aa", "1.0.0", "beta.X.y.5.77.0", "aa")]
        [InlineData("1.0.0+AA", "1.0.0", "", "AA")]
        [InlineData("1.0.0+AA-beta", "1.0.0", "", "AA-beta")]
        [InlineData("a.b.c-d+AA-beta", "", "a.b.c-d", "AA-beta")]
        public async void SplitVersionReturnsCorrectVersionPartsXml(string version, string versionPart, string labelsPart, string buildPart)
        {
            // Arrange & Act
            var context = new EntitiesContext();
            var actual = await context.Database.SqlQuery<string>(
                "SELECT dbo.SplitVersion(@p1)", new SqlParameter("p1", version)
                ).FirstOrDefaultAsync();

            // Assert
            Assert.Equal(string.Format(@"<v vp=""{0}"" lp=""{1}"" bp=""{2}"" />", versionPart, labelsPart, buildPart), actual);
        }
    }
}
