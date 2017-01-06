-- Copyright (c) .NET Foundation. All rights reserved.
-- Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.
IF (OBJECT_ID(N'[dbo].[SplitVersion]') IS NOT NULL)
    DROP FUNCTION [dbo].[SplitVersion]
GO

CREATE FUNCTION [dbo].[SplitVersion]
(
	@Version NVARCHAR(64)
)
RETURNS NVARCHAR(96)
AS
BEGIN
	-- see http://semver.org (i.e., 'versionPart(-labelsPart)?(+buildPart)?'
	DECLARE @versionPart NVARCHAR(64),
	        @labelsPart NVARCHAR(64),
			@buildPart NVARCHAR(64),
			@temp NVARCHAR(64)

	-- split build part first in case '+' precedes '-'
	DECLARE @pos INT = CHARINDEX('+', @Version),
		    @len INT = LEN(@Version)

	SELECT @pos = IIF(@pos <= 0, @len+1, @pos)
	SELECT @temp = SUBSTRING(@Version,0,@pos),
		   @buildPart = SUBSTRING(@Version,@pos+1,@len)

	-- split version and labels parts
	SELECT @pos = CHARINDEX('-', @temp),
		   @len = LEN(@temp)
	SELECT @pos = IIF(@pos <= 0, @len+1, @pos)
	SELECT @versionPart = SUBSTRING(@temp,0,@pos),
		   @labelsPart = SUBSTRING(@temp,@pos+1,@len)

	-- verify version part is valid hierarchy
	IF ( TRY_CAST('/' + @versionPart + '/' AS HIERARCHYID) IS NULL )
	  SELECT @versionPart = '',
		     @labelsPart = @temp

	RETURN '<v vp="' + @versionPart + '" lp="' + @labelsPart + '" bp="' + @buildPart + '" />'
END
GO