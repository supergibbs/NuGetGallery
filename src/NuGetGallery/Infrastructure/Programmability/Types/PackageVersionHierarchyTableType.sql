-- Copyright (c) .NET Foundation. All rights reserved.
-- Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.
IF (TYPE_ID(N'[dbo].[PackageVersionHierarchyTableType]') IS NOT NULL)
    DROP TYPE [dbo].[PackageVersionHierarchyTableType]
GO

CREATE TYPE [dbo].[PackageVersionHierarchyTableType] AS TABLE
(
	-- input from Packages table
	[PackageKey] INT,
	[Version] NVARCHAR(64),

	-- output from OrderPackagesByVersion sproc
	[VersionPart] NVARCHAR(64),
	[VersionPartHier] HIERARCHYID,
	[LabelsPart] NVARCHAR(64),
	[LabelsPartHierStr] NVARCHAR(64),
	[LabelsPartHier] HIERARCHYID,
	[BuildPart] NVARCHAR(64),
	[IsLatest] BIT,
	[IsLatestStable] BIT,
	[VersionRank] INT
)
GO