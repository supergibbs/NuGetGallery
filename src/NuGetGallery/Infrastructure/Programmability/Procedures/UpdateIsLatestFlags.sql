-- Copyright (c) .NET Foundation. All rights reserved.
-- Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.
IF (OBJECT_ID(N'[dbo].[UpdateIsLatestFlags]') IS NOT NULL)
    DROP PROCEDURE [dbo].[UpdateIsLatestFlags]
GO

CREATE PROCEDURE [dbo].[UpdateIsLatestFlags]
  @PackageRegistrationKey INT
AS
BEGIN
	DECLARE @orderedPackages [dbo].[PackageVersionHierarchyTableType],
			@lock INT

	EXEC @lock = sp_getapplock @Resource='UpdateIsLatestFlags', @LockMode='Exclusive', @LockTimeout=5000
	IF ( @lock >= 0 )
	BEGIN
        -- clear IsLatest flags first since OrderPackagesByVersion filters out unlisted and deleted packages
		UPDATE [dbo].[Packages]
		SET [IsLatest] = 0, [IsLatestStable] = 0, [LastUpdated] = GETUTCDATE()
		WHERE [PackageRegistrationKey] = @PackageRegistrationKey AND
            ([IsLatest] = 1 OR [IsLatestStable] = 1)

        -- order listed and undeleted packages and update IsLatest flags
		INSERT INTO @orderedPackages EXEC [dbo].[OrderPackagesByVersion] @PackageRegistrationKey

		UPDATE [dbo].[Packages]
		SET [IsLatest] = op.[IsLatest], [IsLatestStable] = op.[IsLatestStable], [LastUpdated] = GETUTCDATE()
		FROM @orderedPackages AS op
		WHERE [Key] = op.[PackageKey] AND
              [dbo].[Packages].[IsLatest] != op.[IsLatest] AND
              [dbo].[Packages].[IsLatestStable] != op.[IsLatestStable]
	END
END
GO