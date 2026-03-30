USE [OneManVekeryDB];
GO

/*
    Promotion + loyalty patch for existing SQL Server database

    ครอบคลุม:
    - promo code แบบเดิม
    - โปรอัตโนมัติ
    - โปรตามวัน/เวลา
    - ฟรีค่าส่ง
    - buy x get y
    - loyalty points / reward
*/

IF OBJECT_ID(N'dbo.promotions', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.promotions (
        id INT IDENTITY(1,1) PRIMARY KEY,
        promotion_key NVARCHAR(50) NOT NULL UNIQUE,
        title NVARCHAR(120) NOT NULL,
        description NVARCHAR(255) NULL,
        campaign_type NVARCHAR(30) NOT NULL,
        rule_type NVARCHAR(30) NOT NULL,
        benefit_type NVARCHAR(30) NOT NULL,
        target_scope NVARCHAR(30) NOT NULL CONSTRAINT DF_promotions_target_scope DEFAULT N'order',
        reward_scope NVARCHAR(30) NULL,
        priority INT NOT NULL CONSTRAINT DF_promotions_priority DEFAULT 100,
        can_stack BIT NOT NULL CONSTRAINT DF_promotions_can_stack DEFAULT 0,
        auto_apply BIT NOT NULL CONSTRAINT DF_promotions_auto_apply DEFAULT 1,
        requires_code BIT NOT NULL CONSTRAINT DF_promotions_requires_code DEFAULT 0,
        min_order_amount DECIMAL(10,2) NULL,
        min_item_qty INT NULL,
        spend_step_amount DECIMAL(10,2) NULL,
        buy_qty INT NULL,
        get_qty INT NULL,
        discount_percent DECIMAL(5,2) NULL,
        discount_amount DECIMAL(10,2) NULL,
        max_discount_amount DECIMAL(10,2) NULL,
        free_shipping BIT NOT NULL CONSTRAINT DF_promotions_free_shipping DEFAULT 0,
        points_awarded INT NULL,
        points_cost INT NULL,
        reward_qty INT NULL,
        reward_product_id INT NULL,
        reward_category_id INT NULL,
        weekday_mask INT NULL,
        daily_start_time TIME NULL,
        daily_end_time TIME NULL,
        starts_at DATETIME2 NULL,
        expires_at DATETIME2 NULL,
        status NVARCHAR(20) NOT NULL CONSTRAINT DF_promotions_status DEFAULT N'draft',
        note NVARCHAR(255) NULL,
        created_at DATETIME2 NOT NULL CONSTRAINT DF_promotions_created_at DEFAULT SYSUTCDATETIME()
    );
END
GO

IF NOT EXISTS (
    SELECT 1
    FROM sys.foreign_keys
    WHERE name = N'FK_promotions_reward_product'
      AND parent_object_id = OBJECT_ID(N'dbo.promotions')
)
BEGIN
    ALTER TABLE dbo.promotions
    ADD CONSTRAINT FK_promotions_reward_product
        FOREIGN KEY (reward_product_id) REFERENCES dbo.products(id);
END
GO

IF NOT EXISTS (
    SELECT 1
    FROM sys.foreign_keys
    WHERE name = N'FK_promotions_reward_category'
      AND parent_object_id = OBJECT_ID(N'dbo.promotions')
)
BEGIN
    ALTER TABLE dbo.promotions
    ADD CONSTRAINT FK_promotions_reward_category
        FOREIGN KEY (reward_category_id) REFERENCES dbo.categories(id);
END
GO

IF OBJECT_ID(N'dbo.promo_codes', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.promo_codes (
        id INT IDENTITY(1,1) PRIMARY KEY,
        promotion_id INT NULL,
        code NVARCHAR(40) NOT NULL UNIQUE,
        title NVARCHAR(120) NOT NULL,
        description NVARCHAR(255) NULL,
        discount_type NVARCHAR(20) NOT NULL,
        discount_value DECIMAL(10,2) NOT NULL,
        min_order_amount DECIMAL(10,2) NULL,
        max_discount_amount DECIMAL(10,2) NULL,
        usage_limit INT NULL,
        used_count INT NOT NULL CONSTRAINT DF_promo_codes_used_count DEFAULT 0,
        starts_at DATETIME2 NULL,
        expires_at DATETIME2 NULL,
        status NVARCHAR(20) NOT NULL CONSTRAINT DF_promo_codes_status DEFAULT N'draft',
        note NVARCHAR(255) NULL,
        created_at DATETIME2 NOT NULL CONSTRAINT DF_promo_codes_created_at DEFAULT SYSUTCDATETIME()
    );
END
GO

IF COL_LENGTH(N'dbo.promo_codes', N'promotion_id') IS NULL
BEGIN
    ALTER TABLE dbo.promo_codes
    ADD promotion_id INT NULL;
END
GO

IF NOT EXISTS (
    SELECT 1
    FROM sys.foreign_keys
    WHERE name = N'FK_promo_codes_promotions'
      AND parent_object_id = OBJECT_ID(N'dbo.promo_codes')
)
BEGIN
    ALTER TABLE dbo.promo_codes
    ADD CONSTRAINT FK_promo_codes_promotions
        FOREIGN KEY (promotion_id) REFERENCES dbo.promotions(id);
END
GO

IF OBJECT_ID(N'dbo.promotion_targets', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.promotion_targets (
        id INT IDENTITY(1,1) PRIMARY KEY,
        promotion_id INT NOT NULL,
        target_type NVARCHAR(20) NOT NULL,
        category_id INT NULL,
        product_id INT NULL,
        created_at DATETIME2 NOT NULL CONSTRAINT DF_promotion_targets_created_at DEFAULT SYSUTCDATETIME()
    );
END
GO

IF NOT EXISTS (
    SELECT 1
    FROM sys.foreign_keys
    WHERE name = N'FK_promotion_targets_promotions'
      AND parent_object_id = OBJECT_ID(N'dbo.promotion_targets')
)
BEGIN
    ALTER TABLE dbo.promotion_targets
    ADD CONSTRAINT FK_promotion_targets_promotions
        FOREIGN KEY (promotion_id) REFERENCES dbo.promotions(id) ON DELETE CASCADE;
END
GO

IF NOT EXISTS (
    SELECT 1
    FROM sys.foreign_keys
    WHERE name = N'FK_promotion_targets_categories'
      AND parent_object_id = OBJECT_ID(N'dbo.promotion_targets')
)
BEGIN
    ALTER TABLE dbo.promotion_targets
    ADD CONSTRAINT FK_promotion_targets_categories
        FOREIGN KEY (category_id) REFERENCES dbo.categories(id);
END
GO

IF NOT EXISTS (
    SELECT 1
    FROM sys.foreign_keys
    WHERE name = N'FK_promotion_targets_products'
      AND parent_object_id = OBJECT_ID(N'dbo.promotion_targets')
)
BEGIN
    ALTER TABLE dbo.promotion_targets
    ADD CONSTRAINT FK_promotion_targets_products
        FOREIGN KEY (product_id) REFERENCES dbo.products(id);
END
GO

IF COL_LENGTH(N'dbo.orders', N'promo_code_id') IS NULL
BEGIN
    ALTER TABLE dbo.orders
    ADD promo_code_id INT NULL;
END
GO

IF COL_LENGTH(N'dbo.orders', N'discount_code') IS NULL
BEGIN
    ALTER TABLE dbo.orders
    ADD discount_code NVARCHAR(40) NULL;
END
GO

IF COL_LENGTH(N'dbo.orders', N'discount_amount') IS NULL
BEGIN
    ALTER TABLE dbo.orders
    ADD discount_amount DECIMAL(10,2) NOT NULL CONSTRAINT DF_orders_discount_amount DEFAULT 0;
END
GO

IF COL_LENGTH(N'dbo.orders', N'shipping_discount_amount') IS NULL
BEGIN
    ALTER TABLE dbo.orders
    ADD shipping_discount_amount DECIMAL(10,2) NOT NULL CONSTRAINT DF_orders_shipping_discount_amount DEFAULT 0;
END
GO

IF COL_LENGTH(N'dbo.orders', N'points_earned') IS NULL
BEGIN
    ALTER TABLE dbo.orders
    ADD points_earned INT NOT NULL CONSTRAINT DF_orders_points_earned DEFAULT 0;
END
GO

IF COL_LENGTH(N'dbo.orders', N'points_redeemed') IS NULL
BEGIN
    ALTER TABLE dbo.orders
    ADD points_redeemed INT NOT NULL CONSTRAINT DF_orders_points_redeemed DEFAULT 0;
END
GO

IF NOT EXISTS (
    SELECT 1
    FROM sys.foreign_keys
    WHERE name = N'FK_orders_promo_codes'
      AND parent_object_id = OBJECT_ID(N'dbo.orders')
)
BEGIN
    ALTER TABLE dbo.orders
    ADD CONSTRAINT FK_orders_promo_codes
        FOREIGN KEY (promo_code_id) REFERENCES dbo.promo_codes(id);
END
GO

IF OBJECT_ID(N'dbo.order_promotions', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.order_promotions (
        id INT IDENTITY(1,1) PRIMARY KEY,
        order_id INT NOT NULL,
        promotion_id INT NULL,
        promo_code_id INT NULL,
        promotion_title NVARCHAR(120) NOT NULL,
        benefit_type NVARCHAR(30) NOT NULL,
        discount_amount DECIMAL(10,2) NOT NULL CONSTRAINT DF_order_promotions_discount_amount DEFAULT 0,
        shipping_discount_amount DECIMAL(10,2) NOT NULL CONSTRAINT DF_order_promotions_shipping_discount_amount DEFAULT 0,
        points_earned INT NOT NULL CONSTRAINT DF_order_promotions_points_earned DEFAULT 0,
        points_redeemed INT NOT NULL CONSTRAINT DF_order_promotions_points_redeemed DEFAULT 0,
        reward_product_id INT NULL,
        reward_product_name NVARCHAR(100) NULL,
        reward_qty INT NULL,
        note NVARCHAR(255) NULL,
        created_at DATETIME2 NOT NULL CONSTRAINT DF_order_promotions_created_at DEFAULT SYSUTCDATETIME()
    );
END
GO

IF NOT EXISTS (
    SELECT 1
    FROM sys.foreign_keys
    WHERE name = N'FK_order_promotions_orders'
      AND parent_object_id = OBJECT_ID(N'dbo.order_promotions')
)
BEGIN
    ALTER TABLE dbo.order_promotions
    ADD CONSTRAINT FK_order_promotions_orders
        FOREIGN KEY (order_id) REFERENCES dbo.orders(id) ON DELETE CASCADE;
END
GO

IF NOT EXISTS (
    SELECT 1
    FROM sys.foreign_keys
    WHERE name = N'FK_order_promotions_promotions'
      AND parent_object_id = OBJECT_ID(N'dbo.order_promotions')
)
BEGIN
    ALTER TABLE dbo.order_promotions
    ADD CONSTRAINT FK_order_promotions_promotions
        FOREIGN KEY (promotion_id) REFERENCES dbo.promotions(id);
END
GO

IF NOT EXISTS (
    SELECT 1
    FROM sys.foreign_keys
    WHERE name = N'FK_order_promotions_promo_codes'
      AND parent_object_id = OBJECT_ID(N'dbo.order_promotions')
)
BEGIN
    ALTER TABLE dbo.order_promotions
    ADD CONSTRAINT FK_order_promotions_promo_codes
        FOREIGN KEY (promo_code_id) REFERENCES dbo.promo_codes(id);
END
GO

IF NOT EXISTS (
    SELECT 1
    FROM sys.foreign_keys
    WHERE name = N'FK_order_promotions_reward_products'
      AND parent_object_id = OBJECT_ID(N'dbo.order_promotions')
)
BEGIN
    ALTER TABLE dbo.order_promotions
    ADD CONSTRAINT FK_order_promotions_reward_products
        FOREIGN KEY (reward_product_id) REFERENCES dbo.products(id);
END
GO

IF OBJECT_ID(N'dbo.loyalty_wallets', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.loyalty_wallets (
        user_id INT PRIMARY KEY,
        current_points INT NOT NULL CONSTRAINT DF_loyalty_wallets_current_points DEFAULT 0,
        lifetime_earned INT NOT NULL CONSTRAINT DF_loyalty_wallets_lifetime_earned DEFAULT 0,
        lifetime_redeemed INT NOT NULL CONSTRAINT DF_loyalty_wallets_lifetime_redeemed DEFAULT 0,
        updated_at DATETIME2 NOT NULL CONSTRAINT DF_loyalty_wallets_updated_at DEFAULT SYSUTCDATETIME()
    );
END
GO

IF NOT EXISTS (
    SELECT 1
    FROM sys.foreign_keys
    WHERE name = N'FK_loyalty_wallets_users'
      AND parent_object_id = OBJECT_ID(N'dbo.loyalty_wallets')
)
BEGIN
    ALTER TABLE dbo.loyalty_wallets
    ADD CONSTRAINT FK_loyalty_wallets_users
        FOREIGN KEY (user_id) REFERENCES dbo.users(id) ON DELETE CASCADE;
END
GO

IF OBJECT_ID(N'dbo.loyalty_points_ledger', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.loyalty_points_ledger (
        id INT IDENTITY(1,1) PRIMARY KEY,
        user_id INT NOT NULL,
        order_id INT NULL,
        promotion_id INT NULL,
        entry_type NVARCHAR(20) NOT NULL,
        points_delta INT NOT NULL,
        balance_after INT NOT NULL,
        note NVARCHAR(255) NULL,
        created_at DATETIME2 NOT NULL CONSTRAINT DF_loyalty_points_ledger_created_at DEFAULT SYSUTCDATETIME()
    );
END
GO

IF NOT EXISTS (
    SELECT 1
    FROM sys.foreign_keys
    WHERE name = N'FK_loyalty_points_ledger_users'
      AND parent_object_id = OBJECT_ID(N'dbo.loyalty_points_ledger')
)
BEGIN
    ALTER TABLE dbo.loyalty_points_ledger
    ADD CONSTRAINT FK_loyalty_points_ledger_users
        FOREIGN KEY (user_id) REFERENCES dbo.users(id);
END
GO

IF NOT EXISTS (
    SELECT 1
    FROM sys.foreign_keys
    WHERE name = N'FK_loyalty_points_ledger_orders'
      AND parent_object_id = OBJECT_ID(N'dbo.loyalty_points_ledger')
)
BEGIN
    ALTER TABLE dbo.loyalty_points_ledger
    ADD CONSTRAINT FK_loyalty_points_ledger_orders
        FOREIGN KEY (order_id) REFERENCES dbo.orders(id);
END
GO

IF NOT EXISTS (
    SELECT 1
    FROM sys.foreign_keys
    WHERE name = N'FK_loyalty_points_ledger_promotions'
      AND parent_object_id = OBJECT_ID(N'dbo.loyalty_points_ledger')
)
BEGIN
    ALTER TABLE dbo.loyalty_points_ledger
    ADD CONSTRAINT FK_loyalty_points_ledger_promotions
        FOREIGN KEY (promotion_id) REFERENCES dbo.promotions(id);
END
GO
