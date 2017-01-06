-- Copyright (c) .NET Foundation. All rights reserved.
-- Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

-- Update IsLatest flags when package listed or deleted state changes
IF (OBJECT_ID(N'[dbo].[tr_UpdateIsLatestFlags_Update]') IS NOT NULL)
    DROP TRIGGER [dbo].[tr_UpdateIsLatestFlags_Update]
GO

CREATE TRIGGER [dbo].[tr_UpdateIsLatestFlags_Update]
ON [dbo].[Packages]
  AFTER UPDATE
AS
  DECLARE @packageRegistrationKey INT

  SELECT @packageRegistrationKey=PackageRegistrationKey
  FROM INSERTED
  
  IF ( UPDATE (Listed) OR UPDATE(Deleted) )
    EXEC [dbo].[UpdateIsLatestFlags] @packageRegistrationKey
GO

-- Update IsLatest flags when latest package is deleted
IF (OBJECT_ID(N'[dbo].[tr_UpdateIsLatestFlags_Delete]') IS NOT NULL)
    DROP TRIGGER [dbo].[tr_UpdateIsLatestFlags_Delete]
GO

CREATE TRIGGER [dbo].[tr_UpdateIsLatestFlags_Delete]
ON [dbo].[Packages]
  AFTER DELETE
AS
  DECLARE @packageRegistrationKey INT,
          @isLatest BIT,
          @isLatestStable BIT
  
  SELECT @packageRegistrationKey=PackageRegistrationKey,
         @isLatest = IsLatest,
         @isLatestStable = IsLatestStable
  FROM DELETED

  IF ( (@isLatest | @isLatestStable) = 1 )
    EXEC [dbo].[UpdateIsLatestFlags] @packageRegistrationKey
GO

-- Update IsLatest flags when new package is inserted
IF (OBJECT_ID(N'[dbo].[tr_UpdateIsLatestFlags_Insert]') IS NOT NULL)
    DROP TRIGGER [dbo].[tr_UpdateIsLatestFlags_Insert]
GO

CREATE TRIGGER [dbo].[tr_UpdateIsLatestFlags_Insert]
ON [dbo].[Packages]
  AFTER INSERT
AS
  DECLARE @packageRegistrationKey INT

  SELECT @packageRegistrationKey=PackageRegistrationKey
  FROM INSERTED

  EXEC [dbo].[UpdateIsLatestFlags] @packageRegistrationKey
GO