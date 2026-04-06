USE [OneManVekeryDB];
GO

/*
    One Man Vekery
    Seed data for SQL Server

    Run this file after:
    database/create_onemanvekerydb_sqlserver.sql
*/

-- Safety: ensure roles exist
IF NOT EXISTS (SELECT 1 FROM dbo.roles WHERE role_key = N'owner')
    INSERT INTO dbo.roles (role_key, role_name) VALUES (N'owner', N'Owner');
GO

IF NOT EXISTS (SELECT 1 FROM dbo.roles WHERE role_key = N'admin')
    INSERT INTO dbo.roles (role_key, role_name) VALUES (N'admin', N'Admin');
GO

IF NOT EXISTS (SELECT 1 FROM dbo.roles WHERE role_key = N'manager')
    INSERT INTO dbo.roles (role_key, role_name) VALUES (N'manager', N'Manager');
GO

IF NOT EXISTS (SELECT 1 FROM dbo.roles WHERE role_key = N'staff')
    INSERT INTO dbo.roles (role_key, role_name) VALUES (N'staff', N'Staff');
GO

IF NOT EXISTS (SELECT 1 FROM dbo.roles WHERE role_key = N'support')
    INSERT INTO dbo.roles (role_key, role_name) VALUES (N'support', N'Support');
GO

IF NOT EXISTS (SELECT 1 FROM dbo.roles WHERE role_key = N'user')
    INSERT INTO dbo.roles (role_key, role_name) VALUES (N'user', N'User');
GO

-- Safety: ensure categories exist
IF NOT EXISTS (SELECT 1 FROM dbo.categories WHERE name = N'Cake')
    INSERT INTO dbo.categories (name) VALUES (N'Cake');
GO

IF NOT EXISTS (SELECT 1 FROM dbo.categories WHERE name = N'Macaron')
    INSERT INTO dbo.categories (name) VALUES (N'Macaron');
GO

IF NOT EXISTS (SELECT 1 FROM dbo.categories WHERE name = N'Choux Cream')
    INSERT INTO dbo.categories (name) VALUES (N'Choux Cream');
GO

IF NOT EXISTS (SELECT 1 FROM dbo.categories WHERE name = N'Bakery')
    INSERT INTO dbo.categories (name) VALUES (N'Bakery');
GO

-- Users
IF NOT EXISTS (SELECT 1 FROM dbo.users WHERE email = N'admin@onemanvekery.com')
BEGIN
    INSERT INTO dbo.users (full_name, email, password_hash, phone, role_id, status, notes, last_active_at)
    VALUES (
        N'One Man Admin',
        N'admin@onemanvekery.com',
        N'admin12345',
        N'0890000001',
        (SELECT TOP 1 id FROM dbo.roles WHERE role_key = N'admin'),
        N'active',
        N'Initial admin account',
        SYSUTCDATETIME()
    );
END
GO

IF NOT EXISTS (SELECT 1 FROM dbo.users WHERE email = N'mild@example.com')
BEGIN
    INSERT INTO dbo.users (full_name, email, password_hash, phone, role_id, status, notes, last_active_at)
    VALUES (
        N'Mild Patisserie',
        N'mild@example.com',
        N'user12345',
        N'0890000002',
        (SELECT TOP 1 id FROM dbo.roles WHERE role_key = N'user'),
        N'active',
        N'Registered storefront user',
        DATEADD(DAY, -1, SYSUTCDATETIME())
    );
END
GO

IF NOT EXISTS (SELECT 1 FROM dbo.users WHERE email = N'beam@example.com')
BEGIN
    INSERT INTO dbo.users (full_name, email, password_hash, phone, role_id, status, notes, last_active_at)
    VALUES (
        N'Beam Bakery Lover',
        N'beam@example.com',
        N'user12345',
        N'0890000003',
        (SELECT TOP 1 id FROM dbo.roles WHERE role_key = N'user'),
        N'active',
        N'Registered storefront user',
        DATEADD(DAY, -2, SYSUTCDATETIME())
    );
END
GO

IF NOT EXISTS (SELECT 1 FROM dbo.users WHERE email = N'prai@example.com')
BEGIN
    INSERT INTO dbo.users (full_name, email, password_hash, phone, role_id, status, notes, last_active_at)
    VALUES (
        N'ปราย ลูกค้าหน้าร้าน',
        N'prai@example.com',
        N'user12345',
        N'0890000004',
        (SELECT TOP 1 id FROM dbo.roles WHERE role_key = N'user'),
        N'active',
        N'Registered storefront user',
        DATEADD(DAY, -3, SYSUTCDATETIME())
    );
END
GO

IF OBJECT_ID(N'dbo.user_addresses', N'U') IS NOT NULL
    AND NOT EXISTS (
        SELECT 1
        FROM dbo.user_addresses
        WHERE user_id = (SELECT TOP 1 id FROM dbo.users WHERE email = N'mild@example.com')
          AND label = N'บ้าน'
    )
BEGIN
    INSERT INTO dbo.user_addresses (user_id, label, recipient_name, phone, address_line, postal_code, is_default)
    VALUES (
        (SELECT TOP 1 id FROM dbo.users WHERE email = N'mild@example.com'),
        N'บ้าน',
        N'Mild Patisserie',
        N'0890000002',
        N'89/14 ถนนสุขุมวิท แขวงคลองตันเหนือ เขตวัฒนา กรุงเทพมหานคร',
        N'10110',
        1
    );
END
GO

IF OBJECT_ID(N'dbo.user_addresses', N'U') IS NOT NULL
    AND NOT EXISTS (
        SELECT 1
        FROM dbo.user_addresses
        WHERE user_id = (SELECT TOP 1 id FROM dbo.users WHERE email = N'mild@example.com')
          AND label = N'ที่ทำงาน'
    )
BEGIN
    INSERT INTO dbo.user_addresses (user_id, label, recipient_name, phone, address_line, postal_code, is_default)
    VALUES (
        (SELECT TOP 1 id FROM dbo.users WHERE email = N'mild@example.com'),
        N'ที่ทำงาน',
        N'Mild Patisserie',
        N'0890000002',
        N'21 ซอยพหลโยธิน 9 แขวงสามเสนใน เขตพญาไท กรุงเทพมหานคร',
        N'10400',
        0
    );
END
GO

IF OBJECT_ID(N'dbo.user_addresses', N'U') IS NOT NULL
    AND NOT EXISTS (
        SELECT 1
        FROM dbo.user_addresses
        WHERE user_id = (SELECT TOP 1 id FROM dbo.users WHERE email = N'beam@example.com')
          AND label = N'บ้าน'
    )
BEGIN
    INSERT INTO dbo.user_addresses (user_id, label, recipient_name, phone, address_line, postal_code, is_default)
    VALUES (
        (SELECT TOP 1 id FROM dbo.users WHERE email = N'beam@example.com'),
        N'บ้าน',
        N'Beam Bakery Lover',
        N'0890000003',
        N'155/7 ถนนรามคำแหง แขวงหัวหมาก เขตบางกะปิ กรุงเทพมหานคร',
        N'10240',
        1
    );
END
GO

IF OBJECT_ID(N'dbo.user_addresses', N'U') IS NOT NULL
    AND NOT EXISTS (
        SELECT 1
        FROM dbo.user_addresses
        WHERE user_id = (SELECT TOP 1 id FROM dbo.users WHERE email = N'prai@example.com')
          AND label = N'บ้าน'
    )
BEGIN
    INSERT INTO dbo.user_addresses (user_id, label, recipient_name, phone, address_line, postal_code, is_default)
    VALUES (
        (SELECT TOP 1 id FROM dbo.users WHERE email = N'prai@example.com'),
        N'บ้าน',
        N'ปราย ลูกค้าหน้าร้าน',
        N'0890000004',
        N'44/9 ถนนเจริญนคร แขวงบางลำภูล่าง เขตคลองสาน กรุงเทพมหานคร',
        N'10600',
        1
    );
END
GO

-- Products
IF NOT EXISTS (SELECT 1 FROM dbo.products WHERE sku = N'MC-ROSE-01')
BEGIN
    INSERT INTO dbo.products (category_id, sku, name, description, price, stock_qty, image_url, is_active)
    VALUES (
        (SELECT TOP 1 id FROM dbo.categories WHERE name = N'Macaron'),
        N'MC-ROSE-01',
        N'Rose Macaron Box',
        N'Rose and vanilla macarons for soft pink gift sets',
        120.00,
        32,
        N'/images/theme-macaron.svg',
        1
    );
END

IF NOT EXISTS (SELECT 1 FROM dbo.products WHERE sku = N'CK-STRAW-02')
BEGIN
    INSERT INTO dbo.products (category_id, sku, name, description, price, stock_qty, image_url, is_active)
    VALUES (
        (SELECT TOP 1 id FROM dbo.categories WHERE name = N'Cake'),
        N'CK-STRAW-02',
        N'Strawberry Shortcake',
        N'Fresh cream cake with soft sponge and strawberry topping',
        145.00,
        9,
        N'/images/theme-cake.svg',
        1
    );
END

IF NOT EXISTS (SELECT 1 FROM dbo.products WHERE sku = N'CH-VANI-03')
BEGIN
    INSERT INTO dbo.products (category_id, sku, name, description, price, stock_qty, image_url, is_active)
    VALUES (
        (SELECT TOP 1 id FROM dbo.categories WHERE name = N'Choux Cream'),
        N'CH-VANI-03',
        N'Vanilla Choux Cream',
        N'Light pastry shell with smooth vanilla custard filling',
        55.00,
        24,
        N'/images/theme-cream.svg',
        1
    );
END

IF NOT EXISTS (SELECT 1 FROM dbo.products WHERE sku = N'BK-CROI-04')
BEGIN
    INSERT INTO dbo.products (category_id, sku, name, description, price, stock_qty, image_url, is_active)
    VALUES (
        (SELECT TOP 1 id FROM dbo.categories WHERE name = N'Bakery'),
        N'BK-CROI-04',
        N'Butter Croissant',
        N'Flaky layers with rich butter aroma from the morning batch',
        69.00,
        0,
        N'/images/theme-gold.svg',
        0
    );
END

IF NOT EXISTS (SELECT 1 FROM dbo.products WHERE sku = N'CK-BLUE-05')
BEGIN
    INSERT INTO dbo.products (category_id, sku, name, description, price, stock_qty, image_url, is_active)
    VALUES (
        (SELECT TOP 1 id FROM dbo.categories WHERE name = N'Cake'),
        N'CK-BLUE-05',
        N'Blueberry Cheesecake',
        N'Creamy cheesecake finished with blueberry glaze',
        159.00,
        18,
        N'/images/theme-berry.svg',
        1
    );
END

IF NOT EXISTS (SELECT 1 FROM dbo.products WHERE sku = N'BK-ECLA-06')
BEGIN
    INSERT INTO dbo.products (category_id, sku, name, description, price, stock_qty, image_url, is_active)
    VALUES (
        (SELECT TOP 1 id FROM dbo.categories WHERE name = N'Bakery'),
        N'BK-ECLA-06',
        N'Mini Eclair Set',
        N'Small eclair box for afternoon sharing and coffee time',
        89.00,
        21,
        N'/images/theme-cream.svg',
        1
    );
END

IF NOT EXISTS (SELECT 1 FROM dbo.products WHERE sku = N'CK-MILK-07')
BEGIN
    INSERT INTO dbo.products (category_id, sku, name, description, price, stock_qty, image_url, is_active)
    VALUES (
        (SELECT TOP 1 id FROM dbo.categories WHERE name = N'Cake'),
        N'CK-MILK-07',
        N'Milk Cloud Roll',
        N'Japanese style roll cake with soft milk whipped cream',
        135.00,
        14,
        N'/images/theme-milk.svg',
        1
    );
END

IF NOT EXISTS (SELECT 1 FROM dbo.products WHERE sku = N'BK-CHER-08')
BEGIN
    INSERT INTO dbo.products (category_id, sku, name, description, price, stock_qty, image_url, is_active)
    VALUES (
        (SELECT TOP 1 id FROM dbo.categories WHERE name = N'Bakery'),
        N'BK-CHER-08',
        N'Cherry Tart Slice',
        N'Buttery tart shell with cherry compote and almond cream',
        95.00,
        16,
        N'/images/theme-berry.svg',
        1
    );
END
GO

-- Orders
DECLARE @UserMild INT = (SELECT TOP 1 id FROM dbo.users WHERE email = N'mild@example.com');
DECLARE @UserBeam INT = (SELECT TOP 1 id FROM dbo.users WHERE email = N'beam@example.com');
DECLARE @UserPrai INT = (SELECT TOP 1 id FROM dbo.users WHERE email = N'prai@example.com');

IF NOT EXISTS (SELECT 1 FROM dbo.orders WHERE order_no = N'OVK-0001')
BEGIN
    INSERT INTO dbo.orders (
        order_no, user_id, customer_name, phone, address,
        payment_method, payment_status, order_status,
        subtotal, delivery_fee, total_amount, note
    )
    VALUES (
        N'OVK-0001', @UserMild, N'Mild Patisserie', N'0890000002', N'123 ถนนสุขุมวิท กรุงเทพฯ',
        N'promptpay', N'paid', N'paid',
        265.00, 45.00, 310.00, N'ขอการ์ดอวยพรสีชมพู'
    );
END

IF NOT EXISTS (SELECT 1 FROM dbo.orders WHERE order_no = N'OVK-0002')
BEGIN
    INSERT INTO dbo.orders (
        order_no, user_id, customer_name, phone, address,
        payment_method, payment_status, order_status,
        subtotal, delivery_fee, total_amount, note
    )
    VALUES (
        N'OVK-0002', @UserBeam, N'Beam Bakery Lover', N'0890000003', N'88 ถนนลาดพร้าว กรุงเทพฯ',
        N'card', N'paid', N'shipping',
        304.00, 0.00, 304.00, N'ส่งช่วงบ่าย'
    );
END

IF NOT EXISTS (SELECT 1 FROM dbo.orders WHERE order_no = N'OVK-0003')
BEGIN
    INSERT INTO dbo.orders (
        order_no, user_id, customer_name, phone, address,
        payment_method, payment_status, order_status,
        subtotal, delivery_fee, total_amount, note
    )
    VALUES (
        N'OVK-0003', @UserPrai, N'ปราย ลูกค้าหน้าร้าน', N'0890000004', N'45 ถนนพระราม 9 กรุงเทพฯ',
        N'bank-transfer', N'paid', N'delivered',
        240.00, 45.00, 285.00, N'รับหน้าบ้านได้เลย'
    );
END
GO

-- Order items
DECLARE @Order1Id INT = (SELECT TOP 1 id FROM dbo.orders WHERE order_no = N'OVK-0001');
DECLARE @Order2Id INT = (SELECT TOP 1 id FROM dbo.orders WHERE order_no = N'OVK-0002');
DECLARE @Order3Id INT = (SELECT TOP 1 id FROM dbo.orders WHERE order_no = N'OVK-0003');

DECLARE @RoseMacaronId INT = (SELECT TOP 1 id FROM dbo.products WHERE sku = N'MC-ROSE-01');
DECLARE @StrawberryCakeId INT = (SELECT TOP 1 id FROM dbo.products WHERE sku = N'CK-STRAW-02');
DECLARE @BlueberryCakeId INT = (SELECT TOP 1 id FROM dbo.products WHERE sku = N'CK-BLUE-05');
DECLARE @MiniEclairId INT = (SELECT TOP 1 id FROM dbo.products WHERE sku = N'BK-ECLA-06');
DECLARE @MilkRollId INT = (SELECT TOP 1 id FROM dbo.products WHERE sku = N'CK-MILK-07');
DECLARE @CherryTartId INT = (SELECT TOP 1 id FROM dbo.products WHERE sku = N'BK-CHER-08');

IF NOT EXISTS (SELECT 1 FROM dbo.order_items WHERE order_id = @Order1Id AND product_name = N'Rose Macaron Box')
BEGIN
    INSERT INTO dbo.order_items (order_id, product_id, product_name, price, qty, line_total)
    VALUES (@Order1Id, @RoseMacaronId, N'Rose Macaron Box', 120.00, 1, 120.00);
END

IF NOT EXISTS (SELECT 1 FROM dbo.order_items WHERE order_id = @Order1Id AND product_name = N'Strawberry Shortcake')
BEGIN
    INSERT INTO dbo.order_items (order_id, product_id, product_name, price, qty, line_total)
    VALUES (@Order1Id, @StrawberryCakeId, N'Strawberry Shortcake', 145.00, 1, 145.00);
END

IF NOT EXISTS (SELECT 1 FROM dbo.order_items WHERE order_id = @Order2Id AND product_name = N'Blueberry Cheesecake')
BEGIN
    INSERT INTO dbo.order_items (order_id, product_id, product_name, price, qty, line_total)
    VALUES (@Order2Id, @BlueberryCakeId, N'Blueberry Cheesecake', 159.00, 1, 159.00);
END

IF NOT EXISTS (SELECT 1 FROM dbo.order_items WHERE order_id = @Order2Id AND product_name = N'Mini Eclair Set')
BEGIN
    INSERT INTO dbo.order_items (order_id, product_id, product_name, price, qty, line_total)
    VALUES (@Order2Id, @MiniEclairId, N'Mini Eclair Set', 89.00, 1, 89.00);
END

IF NOT EXISTS (SELECT 1 FROM dbo.order_items WHERE order_id = @Order2Id AND product_name = N'Cherry Tart Slice')
BEGIN
    INSERT INTO dbo.order_items (order_id, product_id, product_name, price, qty, line_total)
    VALUES (@Order2Id, @CherryTartId, N'Cherry Tart Slice', 95.00, 1, 95.00);
END

IF NOT EXISTS (SELECT 1 FROM dbo.order_items WHERE order_id = @Order3Id AND product_name = N'Milk Cloud Roll')
BEGIN
    INSERT INTO dbo.order_items (order_id, product_id, product_name, price, qty, line_total)
    VALUES (@Order3Id, @MilkRollId, N'Milk Cloud Roll', 135.00, 1, 135.00);
END

IF NOT EXISTS (SELECT 1 FROM dbo.order_items WHERE order_id = @Order3Id AND product_name = N'Vanilla Choux Cream')
BEGIN
    INSERT INTO dbo.order_items (order_id, product_id, product_name, price, qty, line_total)
    VALUES (
        @Order3Id,
        (SELECT TOP 1 id FROM dbo.products WHERE sku = N'CH-VANI-03'),
        N'Vanilla Choux Cream',
        55.00,
        1,
        55.00
    );
END

IF NOT EXISTS (SELECT 1 FROM dbo.order_items WHERE order_id = @Order3Id AND product_name = N'Cherry Tart Slice')
BEGIN
    INSERT INTO dbo.order_items (order_id, product_id, product_name, price, qty, line_total)
    VALUES (@Order3Id, @CherryTartId, N'Cherry Tart Slice', 95.00, 1, 95.00);
END
GO

-- Contact messages
IF NOT EXISTS (SELECT 1 FROM dbo.contact_messages WHERE email = N'mild@example.com' AND subject = N'สอบถามเค้กวันเกิด')
BEGIN
    INSERT INTO dbo.contact_messages (name, email, phone, subject, message, status)
    VALUES (
        N'Mild Patisserie',
        N'mild@example.com',
        N'0890000002',
        N'สอบถามเค้กวันเกิด',
        N'ต้องการเค้กวันเกิดขนาด 2 ปอนด์ ส่งวันศุกร์นี้ค่ะ',
        N'new'
    );
END
GO

IF NOT EXISTS (SELECT 1 FROM dbo.contact_messages WHERE email = N'beam@example.com' AND subject = N'ถามเรื่องจัดส่ง')
BEGIN
    INSERT INTO dbo.contact_messages (name, email, phone, subject, message, status)
    VALUES (
        N'Beam Bakery Lover',
        N'beam@example.com',
        N'0890000003',
        N'ถามเรื่องจัดส่ง',
        N'หากสั่งวันนี้สามารถส่งถึงพรุ่งนี้ช่วงเช้าได้ไหม',
        N'read'
    );
END
GO
