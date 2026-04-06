USE [OneManVekeryDB];
GO

/*
    One Man Vekery
    Read-only select pack for SQL Server

    ใช้ไฟล์นี้สำหรับ:
    - ดูรายการ table ในฐานข้อมูล
    - ดู column ของแต่ละ table
    - ดู foreign key
    - ดูจำนวน row ของแต่ละ table
    - ดูข้อมูลจริงทุก table
    - ดูข้อมูลแบบ join ที่อ่านง่ายขึ้น
*/

PRINT N'=== DATABASE INFO ===';
SELECT
    DB_NAME() AS database_name,
    SUSER_SNAME() AS executed_by,
    SYSDATETIMEOFFSET() AS executed_at;
GO

PRINT N'=== TABLE LIST ===';
SELECT
    TABLE_SCHEMA,
    TABLE_NAME,
    TABLE_TYPE
FROM INFORMATION_SCHEMA.TABLES
WHERE TABLE_CATALOG = DB_NAME()
  AND TABLE_SCHEMA = N'dbo'
ORDER BY TABLE_NAME;
GO

PRINT N'=== COLUMN LIST ===';
SELECT
    TABLE_NAME,
    ORDINAL_POSITION,
    COLUMN_NAME,
    DATA_TYPE,
    CHARACTER_MAXIMUM_LENGTH,
    NUMERIC_PRECISION,
    NUMERIC_SCALE,
    IS_NULLABLE
FROM INFORMATION_SCHEMA.COLUMNS
WHERE TABLE_CATALOG = DB_NAME()
  AND TABLE_SCHEMA = N'dbo'
ORDER BY TABLE_NAME, ORDINAL_POSITION;
GO

PRINT N'=== FOREIGN KEYS ===';
SELECT
    fk.name AS foreign_key_name,
    OBJECT_NAME(fk.parent_object_id) AS from_table,
    c1.name AS from_column,
    OBJECT_NAME(fk.referenced_object_id) AS to_table,
    c2.name AS to_column
FROM sys.foreign_keys AS fk
INNER JOIN sys.foreign_key_columns AS fkc
    ON fk.object_id = fkc.constraint_object_id
INNER JOIN sys.columns AS c1
    ON fkc.parent_object_id = c1.object_id
   AND fkc.parent_column_id = c1.column_id
INNER JOIN sys.columns AS c2
    ON fkc.referenced_object_id = c2.object_id
   AND fkc.referenced_column_id = c2.column_id
WHERE SCHEMA_NAME(OBJECT_SCHEMA_ID(fk.parent_object_id)) = N'dbo'
ORDER BY from_table, foreign_key_name;
GO

PRINT N'=== ROW COUNTS ===';
SELECT N'categories' AS table_name, COUNT(*) AS row_count FROM dbo.categories
UNION ALL
SELECT N'contact_messages' AS table_name, COUNT(*) AS row_count FROM dbo.contact_messages
UNION ALL
SELECT N'loyalty_points_ledger' AS table_name, COUNT(*) AS row_count FROM dbo.loyalty_points_ledger
UNION ALL
SELECT N'loyalty_wallets' AS table_name, COUNT(*) AS row_count FROM dbo.loyalty_wallets
UNION ALL
SELECT N'order_items' AS table_name, COUNT(*) AS row_count FROM dbo.order_items
UNION ALL
SELECT N'order_promotions' AS table_name, COUNT(*) AS row_count FROM dbo.order_promotions
UNION ALL
SELECT N'orders' AS table_name, COUNT(*) AS row_count FROM dbo.orders
UNION ALL
SELECT N'products' AS table_name, COUNT(*) AS row_count FROM dbo.products
UNION ALL
SELECT N'promotion_targets' AS table_name, COUNT(*) AS row_count FROM dbo.promotion_targets
UNION ALL
SELECT N'promotions' AS table_name, COUNT(*) AS row_count FROM dbo.promotions
UNION ALL
SELECT N'promo_codes' AS table_name, COUNT(*) AS row_count FROM dbo.promo_codes
UNION ALL
SELECT N'roles' AS table_name, COUNT(*) AS row_count FROM dbo.roles
UNION ALL
SELECT N'users' AS table_name, COUNT(*) AS row_count FROM dbo.users
UNION ALL
SELECT N'user_addresses' AS table_name, COUNT(*) AS row_count FROM dbo.user_addresses
ORDER BY table_name;
GO

PRINT N'=== RAW: ROLES ===';
SELECT *
FROM dbo.roles
ORDER BY id;
GO

PRINT N'=== RAW: USERS ===';
SELECT *
FROM dbo.users
ORDER BY id;
GO

PRINT N'=== RAW: USER ADDRESSES ===';
SELECT *
FROM dbo.user_addresses
ORDER BY user_id, is_default DESC, id;
GO

PRINT N'=== RAW: CATEGORIES ===';
SELECT *
FROM dbo.categories
ORDER BY id;
GO

PRINT N'=== RAW: PRODUCTS ===';
SELECT *
FROM dbo.products
ORDER BY id;
GO

PRINT N'=== RAW: PROMO CODES ===';
SELECT *
FROM dbo.promo_codes
ORDER BY id;
GO

PRINT N'=== RAW: PROMOTIONS ===';
SELECT *
FROM dbo.promotions
ORDER BY id;
GO

PRINT N'=== RAW: PROMOTION TARGETS ===';
SELECT *
FROM dbo.promotion_targets
ORDER BY id;
GO

PRINT N'=== RAW: ORDERS ===';
SELECT *
FROM dbo.orders
ORDER BY created_at DESC, id DESC;
GO

PRINT N'=== RAW: ORDER PROMOTIONS ===';
SELECT *
FROM dbo.order_promotions
ORDER BY id DESC;
GO

PRINT N'=== RAW: ORDER ITEMS ===';
SELECT *
FROM dbo.order_items
ORDER BY id DESC;
GO

PRINT N'=== RAW: LOYALTY WALLETS ===';
SELECT *
FROM dbo.loyalty_wallets
ORDER BY user_id;
GO

PRINT N'=== RAW: LOYALTY POINTS LEDGER ===';
SELECT *
FROM dbo.loyalty_points_ledger
ORDER BY id DESC;
GO

PRINT N'=== RAW: CONTACT MESSAGES ===';
SELECT *
FROM dbo.contact_messages
ORDER BY created_at DESC, id DESC;
GO

PRINT N'=== READABLE: USERS WITH ROLE ===';
SELECT
    u.id,
    u.full_name,
    u.email,
    u.phone,
    r.role_key,
    r.role_name,
    u.status,
    u.created_at,
    u.last_active_at,
    u.notes
FROM dbo.users AS u
LEFT JOIN dbo.roles AS r
    ON u.role_id = r.id
ORDER BY u.id;
GO

PRINT N'=== READABLE: USER ADDRESSES ===';
SELECT
    ua.id,
    ua.user_id,
    u.full_name,
    u.email,
    ua.label,
    ua.recipient_name,
    ua.phone,
    ua.address_line,
    ua.postal_code,
    ua.is_default,
    ua.created_at,
    ua.updated_at
FROM dbo.user_addresses AS ua
INNER JOIN dbo.users AS u
    ON ua.user_id = u.id
ORDER BY ua.user_id, ua.is_default DESC, ua.id;
GO

PRINT N'=== READABLE: PRODUCTS WITH CATEGORY ===';
SELECT
    p.id,
    p.sku,
    p.name,
    c.name AS category_name,
    p.price,
    p.stock_qty,
    p.is_active,
    p.image_url,
    p.created_at,
    p.description
FROM dbo.products AS p
LEFT JOIN dbo.categories AS c
    ON p.category_id = c.id
ORDER BY p.id;
GO

PRINT N'=== READABLE: PROMO CODES ===';
SELECT
    id,
    promotion_id,
    code,
    title,
    discount_type,
    discount_value,
    min_order_amount,
    max_discount_amount,
    usage_limit,
    used_count,
    starts_at,
    expires_at,
    status,
    note,
    created_at
FROM dbo.promo_codes
ORDER BY id;
GO

PRINT N'=== READABLE: PROMOTIONS ===';
SELECT
    p.id,
    p.promotion_key,
    p.title,
    p.campaign_type,
    p.rule_type,
    p.benefit_type,
    p.target_scope,
    p.reward_scope,
    p.min_order_amount,
    p.min_item_qty,
    p.spend_step_amount,
    p.buy_qty,
    p.get_qty,
    p.discount_percent,
    p.discount_amount,
    p.max_discount_amount,
    p.free_shipping,
    p.points_awarded,
    p.points_cost,
    p.reward_qty,
    rp.name AS reward_product_name,
    rc.name AS reward_category_name,
    p.weekday_mask,
    p.daily_start_time,
    p.daily_end_time,
    p.starts_at,
    p.expires_at,
    p.status,
    p.note,
    p.created_at
FROM dbo.promotions AS p
LEFT JOIN dbo.products AS rp
    ON p.reward_product_id = rp.id
LEFT JOIN dbo.categories AS rc
    ON p.reward_category_id = rc.id
ORDER BY p.id;
GO

PRINT N'=== READABLE: PROMOTION TARGETS ===';
SELECT
    pt.id,
    pt.promotion_id,
    p.title AS promotion_title,
    pt.target_type,
    c.name AS category_name,
    pr.name AS product_name,
    pr.sku,
    pt.created_at
FROM dbo.promotion_targets AS pt
INNER JOIN dbo.promotions AS p
    ON pt.promotion_id = p.id
LEFT JOIN dbo.categories AS c
    ON pt.category_id = c.id
LEFT JOIN dbo.products AS pr
    ON pt.product_id = pr.id
ORDER BY pt.id;
GO

PRINT N'=== READABLE: ORDERS WITH USER AND PROMO ===';
SELECT
    o.id,
    o.order_no,
    o.customer_name,
    o.phone,
    o.address,
    o.payment_method,
    o.payment_status,
    o.order_status,
    o.subtotal,
    o.discount_amount,
    o.shipping_discount_amount,
    o.delivery_fee,
    o.total_amount,
    o.points_earned,
    o.points_redeemed,
    o.discount_code,
    pc.title AS promo_title,
    u.email AS user_email,
    u.full_name AS user_full_name,
    o.note,
    o.created_at
FROM dbo.orders AS o
LEFT JOIN dbo.users AS u
    ON o.user_id = u.id
LEFT JOIN dbo.promo_codes AS pc
    ON o.promo_code_id = pc.id
ORDER BY o.created_at DESC, o.id DESC;
GO

PRINT N'=== READABLE: ORDER PROMOTIONS ===';
SELECT
    op.id,
    o.order_no,
    op.promotion_title,
    op.benefit_type,
    op.discount_amount,
    op.shipping_discount_amount,
    op.points_earned,
    op.points_redeemed,
    op.reward_product_name,
    op.reward_qty,
    pc.code AS promo_code,
    p.promotion_key,
    op.note,
    op.created_at
FROM dbo.order_promotions AS op
INNER JOIN dbo.orders AS o
    ON op.order_id = o.id
LEFT JOIN dbo.promotions AS p
    ON op.promotion_id = p.id
LEFT JOIN dbo.promo_codes AS pc
    ON op.promo_code_id = pc.id
ORDER BY op.id DESC;
GO

PRINT N'=== READABLE: ORDER ITEMS WITH ORDER AND PRODUCT ===';
SELECT
    oi.id,
    o.order_no,
    oi.order_id,
    oi.product_id,
    p.sku,
    oi.product_name,
    oi.price,
    oi.qty,
    oi.line_total
FROM dbo.order_items AS oi
INNER JOIN dbo.orders AS o
    ON oi.order_id = o.id
LEFT JOIN dbo.products AS p
    ON oi.product_id = p.id
ORDER BY oi.id DESC;
GO

PRINT N'=== READABLE: LOYALTY WALLETS ===';
SELECT
    lw.user_id,
    u.full_name,
    u.email,
    lw.current_points,
    lw.lifetime_earned,
    lw.lifetime_redeemed,
    lw.updated_at
FROM dbo.loyalty_wallets AS lw
INNER JOIN dbo.users AS u
    ON lw.user_id = u.id
ORDER BY lw.user_id;
GO

PRINT N'=== READABLE: LOYALTY POINTS LEDGER ===';
SELECT
    lpl.id,
    u.full_name,
    u.email,
    o.order_no,
    p.title AS promotion_title,
    lpl.entry_type,
    lpl.points_delta,
    lpl.balance_after,
    lpl.note,
    lpl.created_at
FROM dbo.loyalty_points_ledger AS lpl
INNER JOIN dbo.users AS u
    ON lpl.user_id = u.id
LEFT JOIN dbo.orders AS o
    ON lpl.order_id = o.id
LEFT JOIN dbo.promotions AS p
    ON lpl.promotion_id = p.id
ORDER BY lpl.id DESC;
GO

PRINT N'=== READABLE: CONTACT MESSAGES ===';
SELECT
    id,
    name,
    email,
    phone,
    subject,
    status,
    created_at,
    message
FROM dbo.contact_messages
ORDER BY created_at DESC, id DESC;
GO

PRINT N'=== SUMMARY: SALES BY PRODUCT ===';
SELECT
    p.id,
    p.sku,
    p.name,
    ISNULL(SUM(oi.qty), 0) AS total_qty_sold,
    ISNULL(SUM(oi.line_total), 0) AS total_revenue
FROM dbo.products AS p
LEFT JOIN dbo.order_items AS oi
    ON p.id = oi.product_id
GROUP BY p.id, p.sku, p.name
ORDER BY total_revenue DESC, total_qty_sold DESC, p.name ASC;
GO

PRINT N'=== SUMMARY: ORDER TOTALS BY STATUS ===';
SELECT
    order_status,
    payment_status,
    COUNT(*) AS order_count,
    SUM(total_amount) AS total_amount_sum
FROM dbo.orders
GROUP BY order_status, payment_status
ORDER BY order_status, payment_status;
GO
