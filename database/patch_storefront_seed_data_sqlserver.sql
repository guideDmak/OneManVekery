USE [OneManVekeryDB];
GO

/*
    Storefront data patch

    ใช้หลังจากรัน schema patch แล้ว เพื่อเติมข้อมูลจริงที่หน้า storefront ใช้งาน:
    - โปรอัตโนมัติ 5 รายการ
    - promo code ใช้งานจริง 2 รายการ
    - คะแนนสะสมเริ่มต้นของผู้ใช้ตัวอย่าง
*/

DECLARE @RewardProductId INT = (
    SELECT TOP 1 id
    FROM dbo.products
    WHERE sku = N'CH-VANI-03' AND is_active = 1
);

IF EXISTS (SELECT 1 FROM dbo.promotions WHERE promotion_key = N'tuesday-bogo')
BEGIN
    UPDATE dbo.promotions
    SET title = N'ซื้อ 1 แถม 1 ทุกวันอังคาร',
        description = N'ซื้อสินค้าภายในตะกร้าครบตามชุด รับฟรีเพิ่มอัตโนมัติทุกวันอังคาร',
        campaign_type = N'flash',
        rule_type = N'buy_get',
        benefit_type = N'discount',
        target_scope = N'order',
        reward_scope = N'same_item',
        priority = 10,
        can_stack = 1,
        auto_apply = 1,
        requires_code = 0,
        buy_qty = 1,
        get_qty = 1,
        discount_percent = NULL,
        discount_amount = NULL,
        max_discount_amount = NULL,
        free_shipping = 0,
        min_order_amount = NULL,
        min_item_qty = NULL,
        spend_step_amount = NULL,
        points_awarded = NULL,
        points_cost = NULL,
        reward_qty = NULL,
        reward_product_id = NULL,
        reward_category_id = NULL,
        weekday_mask = POWER(2, 2),
        daily_start_time = NULL,
        daily_end_time = NULL,
        starts_at = NULL,
        expires_at = NULL,
        status = N'active',
        note = N'Seeded storefront promotion'
    WHERE promotion_key = N'tuesday-bogo';
END
ELSE
BEGIN
    INSERT INTO dbo.promotions (
        promotion_key, title, description, campaign_type, rule_type, benefit_type,
        target_scope, reward_scope, priority, can_stack, auto_apply, requires_code,
        buy_qty, get_qty, weekday_mask, status, note
    )
    VALUES (
        N'tuesday-bogo', N'ซื้อ 1 แถม 1 ทุกวันอังคาร',
        N'ซื้อสินค้าภายในตะกร้าครบตามชุด รับฟรีเพิ่มอัตโนมัติทุกวันอังคาร',
        N'flash', N'buy_get', N'discount',
        N'order', N'same_item', 10, 1, 1, 0,
        1, 1, POWER(2, 2), N'active', N'Seeded storefront promotion'
    );
END
GO

IF EXISTS (SELECT 1 FROM dbo.promotions WHERE promotion_key = N'three-items-15-percent')
BEGIN
    UPDATE dbo.promotions
    SET title = N'ซื้อครบ 3 ชิ้น ลด 15%',
        description = N'เมื่อจำนวนสินค้าในตะกร้าครบ 3 ชิ้นขึ้นไป รับส่วนลดทันที 15%',
        campaign_type = N'cart',
        rule_type = N'min_item_qty',
        benefit_type = N'discount',
        target_scope = N'order',
        reward_scope = NULL,
        priority = 20,
        can_stack = 1,
        auto_apply = 1,
        requires_code = 0,
        buy_qty = NULL,
        get_qty = NULL,
        discount_percent = 15,
        discount_amount = NULL,
        max_discount_amount = NULL,
        free_shipping = 0,
        min_order_amount = NULL,
        min_item_qty = 3,
        spend_step_amount = NULL,
        points_awarded = NULL,
        points_cost = NULL,
        reward_qty = NULL,
        reward_product_id = NULL,
        reward_category_id = NULL,
        weekday_mask = NULL,
        daily_start_time = NULL,
        daily_end_time = NULL,
        starts_at = NULL,
        expires_at = NULL,
        status = N'active',
        note = N'Seeded storefront promotion'
    WHERE promotion_key = N'three-items-15-percent';
END
ELSE
BEGIN
    INSERT INTO dbo.promotions (
        promotion_key, title, description, campaign_type, rule_type, benefit_type,
        target_scope, priority, can_stack, auto_apply, requires_code,
        min_item_qty, discount_percent, status, note
    )
    VALUES (
        N'three-items-15-percent', N'ซื้อครบ 3 ชิ้น ลด 15%',
        N'เมื่อจำนวนสินค้าในตะกร้าครบ 3 ชิ้นขึ้นไป รับส่วนลดทันที 15%',
        N'cart', N'min_item_qty', N'discount',
        N'order', 20, 1, 1, 0,
        3, 15, N'active', N'Seeded storefront promotion'
    );
END
GO

IF EXISTS (SELECT 1 FROM dbo.promotions WHERE promotion_key = N'free-shipping-100')
BEGIN
    UPDATE dbo.promotions
    SET title = N'ซื้อครบ 100 ฿ ส่งฟรี',
        description = N'เมื่อยอดสินค้าครบ 100 บาทขึ้นไป ระบบจะยกเว้นค่าส่งให้อัตโนมัติ',
        campaign_type = N'delivery',
        rule_type = N'min_order',
        benefit_type = N'shipping',
        target_scope = N'order',
        reward_scope = NULL,
        priority = 30,
        can_stack = 1,
        auto_apply = 1,
        requires_code = 0,
        buy_qty = NULL,
        get_qty = NULL,
        discount_percent = NULL,
        discount_amount = NULL,
        max_discount_amount = NULL,
        free_shipping = 1,
        min_order_amount = 100,
        min_item_qty = NULL,
        spend_step_amount = NULL,
        points_awarded = NULL,
        points_cost = NULL,
        reward_qty = NULL,
        reward_product_id = NULL,
        reward_category_id = NULL,
        weekday_mask = NULL,
        daily_start_time = NULL,
        daily_end_time = NULL,
        starts_at = NULL,
        expires_at = NULL,
        status = N'active',
        note = N'Seeded storefront promotion'
    WHERE promotion_key = N'free-shipping-100';
END
ELSE
BEGIN
    INSERT INTO dbo.promotions (
        promotion_key, title, description, campaign_type, rule_type, benefit_type,
        target_scope, priority, can_stack, auto_apply, requires_code,
        min_order_amount, free_shipping, status, note
    )
    VALUES (
        N'free-shipping-100', N'ซื้อครบ 100 ฿ ส่งฟรี',
        N'เมื่อยอดสินค้าครบ 100 บาทขึ้นไป ระบบจะยกเว้นค่าส่งให้อัตโนมัติ',
        N'delivery', N'min_order', N'shipping',
        N'order', 30, 1, 1, 0,
        100, 1, N'active', N'Seeded storefront promotion'
    );
END
GO

IF EXISTS (SELECT 1 FROM dbo.promotions WHERE promotion_key = N'happy-hour-50')
BEGIN
    UPDATE dbo.promotions
    SET title = N'ลดทั้งร้าน 50% เวลา 19:00 - 20:00',
        description = N'Happy hour ลดทั้งร้าน 50% ทุกวันในช่วงเวลา 19:00 - 20:00',
        campaign_type = N'flash',
        rule_type = N'time_window',
        benefit_type = N'discount',
        target_scope = N'order',
        reward_scope = NULL,
        priority = 40,
        can_stack = 1,
        auto_apply = 1,
        requires_code = 0,
        buy_qty = NULL,
        get_qty = NULL,
        discount_percent = 50,
        discount_amount = NULL,
        max_discount_amount = NULL,
        free_shipping = 0,
        min_order_amount = NULL,
        min_item_qty = NULL,
        spend_step_amount = NULL,
        points_awarded = NULL,
        points_cost = NULL,
        reward_qty = NULL,
        reward_product_id = NULL,
        reward_category_id = NULL,
        weekday_mask = NULL,
        daily_start_time = '19:00',
        daily_end_time = '20:00',
        starts_at = NULL,
        expires_at = NULL,
        status = N'active',
        note = N'Seeded storefront promotion'
    WHERE promotion_key = N'happy-hour-50';
END
ELSE
BEGIN
    INSERT INTO dbo.promotions (
        promotion_key, title, description, campaign_type, rule_type, benefit_type,
        target_scope, priority, can_stack, auto_apply, requires_code,
        discount_percent, daily_start_time, daily_end_time, status, note
    )
    VALUES (
        N'happy-hour-50', N'ลดทั้งร้าน 50% เวลา 19:00 - 20:00',
        N'Happy hour ลดทั้งร้าน 50% ทุกวันในช่วงเวลา 19:00 - 20:00',
        N'flash', N'time_window', N'discount',
        N'order', 40, 1, 1, 0,
        50, '19:00', '20:00', N'active', N'Seeded storefront promotion'
    );
END
GO

IF EXISTS (SELECT 1 FROM dbo.promotions WHERE promotion_key = N'loyalty-points-program')
BEGIN
    UPDATE dbo.promotions
    SET title = N'สะสมครบแลกขนมฟรี',
        description = N'ทุกยอดซื้อครบ 20 บาท รับ 10 พอยต์ และแลกของรางวัลได้เมื่อครบ 100 พอยต์',
        campaign_type = N'loyalty',
        rule_type = N'spend_step',
        benefit_type = N'points_reward',
        target_scope = N'order',
        reward_scope = N'reward_product',
        priority = 50,
        can_stack = 1,
        auto_apply = 1,
        requires_code = 0,
        buy_qty = NULL,
        get_qty = NULL,
        discount_percent = NULL,
        discount_amount = NULL,
        max_discount_amount = NULL,
        free_shipping = 0,
        min_order_amount = NULL,
        min_item_qty = NULL,
        spend_step_amount = 20,
        points_awarded = 10,
        points_cost = 100,
        reward_qty = 1,
        reward_product_id = @RewardProductId,
        reward_category_id = NULL,
        weekday_mask = NULL,
        daily_start_time = NULL,
        daily_end_time = NULL,
        starts_at = NULL,
        expires_at = NULL,
        status = N'active',
        note = N'Seeded storefront promotion'
    WHERE promotion_key = N'loyalty-points-program';
END
ELSE
BEGIN
    INSERT INTO dbo.promotions (
        promotion_key, title, description, campaign_type, rule_type, benefit_type,
        target_scope, reward_scope, priority, can_stack, auto_apply, requires_code,
        spend_step_amount, points_awarded, points_cost, reward_qty, reward_product_id,
        status, note
    )
    VALUES (
        N'loyalty-points-program', N'สะสมครบแลกขนมฟรี',
        N'ทุกยอดซื้อครบ 20 บาท รับ 10 พอยต์ และแลกของรางวัลได้เมื่อครบ 100 พอยต์',
        N'loyalty', N'spend_step', N'points_reward',
        N'order', N'reward_product', 50, 1, 1, 0,
        20, 10, 100, 1, @RewardProductId,
        N'active', N'Seeded storefront promotion'
    );
END
GO

IF EXISTS (SELECT 1 FROM dbo.promotions WHERE promotion_key = N'welcome-10-code')
BEGIN
    UPDATE dbo.promotions
    SET title = N'WELCOME10 ส่วนลด 10%',
        description = N'ใช้ได้เมื่อสั่งซื้อครบ 150 บาท ลด 10% สูงสุด 80 บาท',
        campaign_type = N'coupon',
        rule_type = N'promo_code',
        benefit_type = N'discount',
        target_scope = N'order',
        reward_scope = NULL,
        priority = 60,
        can_stack = 1,
        auto_apply = 0,
        requires_code = 1,
        buy_qty = NULL,
        get_qty = NULL,
        discount_percent = 10,
        discount_amount = NULL,
        max_discount_amount = 80,
        free_shipping = 0,
        min_order_amount = 150,
        min_item_qty = NULL,
        spend_step_amount = NULL,
        points_awarded = NULL,
        points_cost = NULL,
        reward_qty = NULL,
        reward_product_id = NULL,
        reward_category_id = NULL,
        weekday_mask = NULL,
        daily_start_time = NULL,
        daily_end_time = NULL,
        starts_at = NULL,
        expires_at = NULL,
        status = N'active',
        note = N'Seeded storefront promotion'
    WHERE promotion_key = N'welcome-10-code';
END
ELSE
BEGIN
    INSERT INTO dbo.promotions (
        promotion_key, title, description, campaign_type, rule_type, benefit_type,
        target_scope, priority, can_stack, auto_apply, requires_code,
        min_order_amount, discount_percent, max_discount_amount, status, note
    )
    VALUES (
        N'welcome-10-code', N'WELCOME10 ส่วนลด 10%',
        N'ใช้ได้เมื่อสั่งซื้อครบ 150 บาท ลด 10% สูงสุด 80 บาท',
        N'coupon', N'promo_code', N'discount',
        N'order', 60, 1, 0, 1,
        150, 10, 80, N'active', N'Seeded storefront promotion'
    );
END
GO

IF EXISTS (SELECT 1 FROM dbo.promotions WHERE promotion_key = N'shipping-code')
BEGIN
    UPDATE dbo.promotions
    SET title = N'SHIPFREE ส่งฟรี',
        description = N'ใช้ได้เมื่อสั่งซื้อครบ 80 บาท และกรอกโค้ดรับส่งฟรี',
        campaign_type = N'coupon',
        rule_type = N'promo_code',
        benefit_type = N'shipping',
        target_scope = N'order',
        reward_scope = NULL,
        priority = 70,
        can_stack = 1,
        auto_apply = 0,
        requires_code = 1,
        buy_qty = NULL,
        get_qty = NULL,
        discount_percent = NULL,
        discount_amount = NULL,
        max_discount_amount = NULL,
        free_shipping = 1,
        min_order_amount = 80,
        min_item_qty = NULL,
        spend_step_amount = NULL,
        points_awarded = NULL,
        points_cost = NULL,
        reward_qty = NULL,
        reward_product_id = NULL,
        reward_category_id = NULL,
        weekday_mask = NULL,
        daily_start_time = NULL,
        daily_end_time = NULL,
        starts_at = NULL,
        expires_at = NULL,
        status = N'active',
        note = N'Seeded storefront promotion'
    WHERE promotion_key = N'shipping-code';
END
ELSE
BEGIN
    INSERT INTO dbo.promotions (
        promotion_key, title, description, campaign_type, rule_type, benefit_type,
        target_scope, priority, can_stack, auto_apply, requires_code,
        min_order_amount, free_shipping, status, note
    )
    VALUES (
        N'shipping-code', N'SHIPFREE ส่งฟรี',
        N'ใช้ได้เมื่อสั่งซื้อครบ 80 บาท และกรอกโค้ดรับส่งฟรี',
        N'coupon', N'promo_code', N'shipping',
        N'order', 70, 1, 0, 1,
        80, 1, N'active', N'Seeded storefront promotion'
    );
END
GO

IF EXISTS (SELECT 1 FROM dbo.promo_codes WHERE code = N'WELCOME10')
BEGIN
    UPDATE dbo.promo_codes
    SET promotion_id = (SELECT TOP 1 id FROM dbo.promotions WHERE promotion_key = N'welcome-10-code'),
        title = N'WELCOME10 ส่วนลด 10%',
        description = N'สั่งซื้อครบ 150 บาท ลดเพิ่ม 10% สูงสุด 80 บาท',
        discount_type = N'percent',
        discount_value = 10,
        min_order_amount = 150,
        max_discount_amount = 80,
        usage_limit = 500,
        starts_at = NULL,
        expires_at = NULL,
        status = N'active',
        note = N'Seeded storefront promo code'
    WHERE code = N'WELCOME10';
END
ELSE
BEGIN
    INSERT INTO dbo.promo_codes (
        promotion_id, code, title, description, discount_type, discount_value,
        min_order_amount, max_discount_amount, usage_limit, status, note
    )
    VALUES (
        (SELECT TOP 1 id FROM dbo.promotions WHERE promotion_key = N'welcome-10-code'),
        N'WELCOME10',
        N'WELCOME10 ส่วนลด 10%',
        N'สั่งซื้อครบ 150 บาท ลดเพิ่ม 10% สูงสุด 80 บาท',
        N'percent',
        10,
        150,
        80,
        500,
        N'active',
        N'Seeded storefront promo code'
    );
END
GO

IF EXISTS (SELECT 1 FROM dbo.promo_codes WHERE code = N'SHIPFREE')
BEGIN
    UPDATE dbo.promo_codes
    SET promotion_id = (SELECT TOP 1 id FROM dbo.promotions WHERE promotion_key = N'shipping-code'),
        title = N'SHIPFREE ส่งฟรี',
        description = N'สั่งซื้อครบ 80 บาท รับสิทธิ์ส่งฟรีทันที',
        discount_type = N'shipping',
        discount_value = 0,
        min_order_amount = 80,
        max_discount_amount = NULL,
        usage_limit = 500,
        starts_at = NULL,
        expires_at = NULL,
        status = N'active',
        note = N'Seeded storefront promo code'
    WHERE code = N'SHIPFREE';
END
ELSE
BEGIN
    INSERT INTO dbo.promo_codes (
        promotion_id, code, title, description, discount_type, discount_value,
        min_order_amount, usage_limit, status, note
    )
    VALUES (
        (SELECT TOP 1 id FROM dbo.promotions WHERE promotion_key = N'shipping-code'),
        N'SHIPFREE',
        N'SHIPFREE ส่งฟรี',
        N'สั่งซื้อครบ 80 บาท รับสิทธิ์ส่งฟรีทันที',
        N'shipping',
        0,
        80,
        500,
        N'active',
        N'Seeded storefront promo code'
    );
END
GO

IF OBJECT_ID(N'dbo.loyalty_wallets', N'U') IS NOT NULL
BEGIN
    IF EXISTS (
        SELECT 1
        FROM dbo.loyalty_wallets
        WHERE user_id = (SELECT TOP 1 id FROM dbo.users WHERE email = N'mild@example.com')
    )
    BEGIN
        UPDATE dbo.loyalty_wallets
        SET current_points = 120,
            lifetime_earned = 180,
            lifetime_redeemed = 60,
            updated_at = SYSUTCDATETIME()
        WHERE user_id = (SELECT TOP 1 id FROM dbo.users WHERE email = N'mild@example.com');
    END
    ELSE
    BEGIN
        INSERT INTO dbo.loyalty_wallets (user_id, current_points, lifetime_earned, lifetime_redeemed)
        VALUES (
            (SELECT TOP 1 id FROM dbo.users WHERE email = N'mild@example.com'),
            120,
            180,
            60
        );
    END

    IF EXISTS (
        SELECT 1
        FROM dbo.loyalty_wallets
        WHERE user_id = (SELECT TOP 1 id FROM dbo.users WHERE email = N'beam@example.com')
    )
    BEGIN
        UPDATE dbo.loyalty_wallets
        SET current_points = 40,
            lifetime_earned = 40,
            lifetime_redeemed = 0,
            updated_at = SYSUTCDATETIME()
        WHERE user_id = (SELECT TOP 1 id FROM dbo.users WHERE email = N'beam@example.com');
    END
    ELSE
    BEGIN
        INSERT INTO dbo.loyalty_wallets (user_id, current_points, lifetime_earned, lifetime_redeemed)
        VALUES (
            (SELECT TOP 1 id FROM dbo.users WHERE email = N'beam@example.com'),
            40,
            40,
            0
        );
    END
END
GO
