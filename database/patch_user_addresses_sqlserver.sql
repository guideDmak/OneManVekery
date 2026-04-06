USE [OneManVekeryDB];
GO

/*
    User addresses patch for existing SQL Server database

    ครอบคลุม:
    - ตาราง user_addresses สำหรับหลายที่อยู่ต่อ user
    - default address ต่อ user ได้เพียง 1 รายการ
    - backfill ที่อยู่หลักจากออเดอร์ล่าสุดของ user ถ้ามี
*/

IF OBJECT_ID(N'dbo.user_addresses', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.user_addresses (
        id INT IDENTITY(1,1) PRIMARY KEY,
        user_id INT NOT NULL,
        label NVARCHAR(50) NULL,
        recipient_name NVARCHAR(100) NOT NULL,
        phone NVARCHAR(20) NOT NULL,
        address_line NVARCHAR(MAX) NOT NULL,
        postal_code NVARCHAR(20) NULL,
        is_default BIT NOT NULL CONSTRAINT DF_user_addresses_is_default DEFAULT 0,
        created_at DATETIME2 NOT NULL CONSTRAINT DF_user_addresses_created_at DEFAULT SYSUTCDATETIME(),
        updated_at DATETIME2 NOT NULL CONSTRAINT DF_user_addresses_updated_at DEFAULT SYSUTCDATETIME()
    );
END
GO

IF NOT EXISTS (
    SELECT 1
    FROM sys.foreign_keys
    WHERE name = N'FK_user_addresses_users'
      AND parent_object_id = OBJECT_ID(N'dbo.user_addresses')
)
BEGIN
    ALTER TABLE dbo.user_addresses
    ADD CONSTRAINT FK_user_addresses_users
        FOREIGN KEY (user_id) REFERENCES dbo.users(id) ON DELETE CASCADE;
END
GO

IF NOT EXISTS (
    SELECT 1
    FROM sys.indexes
    WHERE name = N'IX_user_addresses_user_id'
      AND object_id = OBJECT_ID(N'dbo.user_addresses')
)
BEGIN
    CREATE INDEX IX_user_addresses_user_id
        ON dbo.user_addresses (user_id, id);
END
GO

IF NOT EXISTS (
    SELECT 1
    FROM sys.indexes
    WHERE name = N'UX_user_addresses_default_per_user'
      AND object_id = OBJECT_ID(N'dbo.user_addresses')
)
BEGIN
    CREATE UNIQUE INDEX UX_user_addresses_default_per_user
        ON dbo.user_addresses (user_id)
        WHERE is_default = 1;
END
GO

;WITH latest_order_address AS
(
    SELECT
        o.user_id,
        o.customer_name,
        o.phone,
        o.address,
        ROW_NUMBER() OVER (
            PARTITION BY o.user_id
            ORDER BY o.created_at DESC, o.id DESC
        ) AS row_num
    FROM dbo.orders AS o
    WHERE o.user_id IS NOT NULL
      AND LTRIM(RTRIM(ISNULL(o.address, N''))) <> N''
)
INSERT INTO dbo.user_addresses (
    user_id,
    label,
    recipient_name,
    phone,
    address_line,
    is_default
)
SELECT
    source.user_id,
    N'ที่อยู่หลัก',
    source.customer_name,
    source.phone,
    source.address,
    1
FROM latest_order_address AS source
WHERE source.row_num = 1
  AND NOT EXISTS (
      SELECT 1
      FROM dbo.user_addresses AS existing
      WHERE existing.user_id = source.user_id
  );
GO
