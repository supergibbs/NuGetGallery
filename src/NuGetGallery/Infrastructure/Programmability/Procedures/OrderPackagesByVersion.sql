-- Copyright (c) .NET Foundation. All rights reserved.
-- Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.
IF (OBJECT_ID(N'[dbo].[OrderPackagesByVersion]') IS NOT NULL)
    DROP PROCEDURE [dbo].[OrderPackagesByVersion]
GO

CREATE PROCEDURE OrderPackagesByVersion
  @PackageRegistrationKey INT
AS
BEGIN
	DECLARE @labels TABLE
    (
	  [Key] INT,
      [Version] NVARCHAR(64),
	  [VersionPart] NVARCHAR(64),
      [VersionPartHier] HIERARCHYID,
      [LabelsPart] NVARCHAR(64),
      [BuildPart] NVARCHAR(64),
	  [Label] NVARCHAR(64),
      [Node] INT,
      [DenseRank] INT
    )
	
    -- 1. Split version strings into parts and label parts into label identifiers

    -- q3: split labels part into label identifiers (split across new rows)
	INSERT INTO @labels
	SELECT q3.[Key], q3.[Version],
	       q3.[VersionPart], q3.[VersionPartHier], q3.[LabelsPart], q3.[BuildPart],
		   v.id.value('.', 'VARCHAR(64)') AS Label,
           ROW_NUMBER() OVER (PARTITION BY q3.[Key] ORDER BY q3.[Key]) AS Node,
           0 AS DenseRank
	FROM
    (
        -- q2: split labels part into label identifiers (xml)
		SELECT q2.*, CAST('<id>'+REPLACE(q2.LabelsPart, '.', '</id><id>')+'</id>' AS XML) AS Labels
		FROM
        (
			-- q1: split version string into parts (new columns)
			SELECT q1.*,
			  v.c.value('@vp', 'VARCHAR(64)') AS VersionPart,
			  TRY_CAST('/' + v.c.value('@vp', 'VARCHAR(64)') + '/' AS HIERARCHYID) AS VersionPartHier,
			  v.c.value('@lp', 'VARCHAR(64)') AS LabelsPart,
			  v.c.value('@bp', 'VARCHAR(64)') AS BuildPart
			FROM
            (
                -- split version string into parts (xml)
				SELECT [Key], [Version], CAST([dbo].[SplitVersion]([Version]) AS XML) AS VersionParts
				FROM [dbo].[Packages]
				WHERE [PackageRegistrationKey] = @PackageRegistrationKey AND
                      -- filter early to simplify IsLatest calculations
                      [Listed] = 1 AND [Deleted] = 0
			) AS q1
            OUTER APPLY q1.VersionParts.nodes('v') AS v(c)
        ) AS q2
    ) AS q3
	CROSS APPLY q3.Labels.nodes('id') AS v(id)

    -- 2. Calculate numeric rankings across all label identifiers in the same node index

	UPDATE T SET DenseRank = dr
	FROM (
	    -- collation is case insensitive by default
		SELECT DenseRank, DENSE_RANK() OVER (
            PARTITION BY Node ORDER BY
                -- no labels first
                IIF(NULLIF(Label, '') IS NULL, 1, 0),
                -- numeric over alphanumeric
                IIF(TRY_CAST(Label AS INT) IS NULL, 1, 0),
                -- numeric comparison when possible, else alphanumeric
                TRY_CAST(Label AS INT), Label
            ) AS dr
		FROM @labels
	) AS T

    -- 3. Calculate IsLatest flags and cache version order in case we want to use in future

    -- review: should IsLatestStable calc use BuildPart, and can there be duplicate versions with different build metadata?
	SELECT q3.*,
		CAST(IIF(ROW_NUMBER() OVER (
				ORDER BY q3.[VersionPartHier] DESC, q3.[LabelsPartHier] DESC, q3.[BuildPart] DESC
			)=1, 1, 0) AS BIT) as IsLatest,
		CAST(IIF(NULLIF(q3.[LabelsPart], '') IS NULL AND NULLIF(q3.[BuildPart], '') IS NULL AND ROW_NUMBER() OVER (
				ORDER BY NULLIF(q3.[LabelsPart], ''), NULLIF(q3.[BuildPart], ''), q3.[VersionPartHier] DESC, q3.[BuildPart] DESC
			)=1, 1, 0) AS BIT) as IsLatestStable,
		CAST(ROW_NUMBER() OVER (
                ORDER BY q3.[VersionPartHier] DESC, q3.[LabelsPartHier] DESC, q3.[BuildPart] DESC) AS INT) as VersionRank
	FROM (
        -- q2: join on DenseRanks to finish conversion of alphanumeric label parts into orderable hierarchy ids
		SELECT
			q2.[Key], q2.[Version],
			q2.[VersionPart], q2.[VersionPartHier], q2.[LabelsPart],
			STUFF((SELECT '.' + CAST([DenseRank] AS VARCHAR(64))
				   FROM @labels q1
				   WHERE q2.[Key] = q1.[Key] FOR XML PATH('')
                   ), 1, 1, '') AS LabelsPartHierStr,
			CAST('/' +
				STUFF((SELECT '.' + CAST([DenseRank] AS VARCHAR(64))
				       FROM @labels q1
					   WHERE q2.[Key] = q1.[Key] FOR XML PATH('')
                      ), 1, 1, '') +
                 '/' AS HIERARCHYID) AS LabelsPartHier,
			q2.[BuildPart]
		FROM @labels q2
		GROUP BY [Key], [Version], [VersionPart], [VersionPartHier], [LabelsPart], [BuildPart]
	) AS q3
    ORDER BY  VersionRank
END
GO