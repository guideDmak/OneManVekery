USE [OneManVekeryDB];
GO

IF COL_LENGTH('dbo.users', 'last_active_at') IS NULL
BEGIN
    ALTER TABLE dbo.users
    ADD last_active_at DATETIME2 NULL;
END
GO

IF COL_LENGTH('dbo.users', 'notes') IS NULL
BEGIN
    ALTER TABLE dbo.users
    ADD notes NVARCHAR(255) NULL;
END
GO

UPDATE dbo.users
SET password_hash = N'admin12345',
    notes = COALESCE(notes, N'Initial admin account'),
    last_active_at = COALESCE(last_active_at, SYSUTCDATETIME())
WHERE email = N'admin@onemanvekery.com';
GO

UPDATE dbo.users
SET password_hash = N'user12345',
    notes = COALESCE(notes, N'Registered storefront user'),
    last_active_at = COALESCE(last_active_at, DATEADD(DAY, -1, SYSUTCDATETIME()))
WHERE email IN (N'mild@example.com', N'beam@example.com', N'prai@example.com');
GO
