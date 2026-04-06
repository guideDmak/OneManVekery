using Microsoft.EntityFrameworkCore;

namespace OneManVekery.Models.Db;

public partial class OneManVekeryDBContext : DbContext
{
    public OneManVekeryDBContext(DbContextOptions<OneManVekeryDBContext> options)
        : base(options)
    {
    }

    public virtual DbSet<Category> Categories { get; set; }

    public virtual DbSet<ContactMessage> ContactMessages { get; set; }

    public virtual DbSet<LoyaltyPointsLedger> LoyaltyPointsLedgers { get; set; }

    public virtual DbSet<LoyaltyWallet> LoyaltyWallets { get; set; }

    public virtual DbSet<Order> Orders { get; set; }

    public virtual DbSet<OrderItem> OrderItems { get; set; }

    public virtual DbSet<OrderPromotion> OrderPromotions { get; set; }

    public virtual DbSet<Product> Products { get; set; }

    public virtual DbSet<PromoCode> PromoCodes { get; set; }

    public virtual DbSet<Promotion> Promotions { get; set; }

    public virtual DbSet<PromotionTarget> PromotionTargets { get; set; }

    public virtual DbSet<Role> Roles { get; set; }

    public virtual DbSet<User> Users { get; set; }

    public virtual DbSet<UserAddress> UserAddresses { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Category>(entity =>
        {
            entity.ToTable("categories");

            entity.HasIndex(e => e.Name).IsUnique();

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.Name)
                .HasMaxLength(50)
                .HasColumnName("name");
        });

        modelBuilder.Entity<ContactMessage>(entity =>
        {
            entity.ToTable("contact_messages");

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("(sysutcdatetime())")
                .HasColumnName("created_at");
            entity.Property(e => e.Email)
                .HasMaxLength(120)
                .HasColumnName("email");
            entity.Property(e => e.Message).HasColumnName("message");
            entity.Property(e => e.Name)
                .HasMaxLength(100)
                .HasColumnName("name");
            entity.Property(e => e.Phone)
                .HasMaxLength(20)
                .HasColumnName("phone");
            entity.Property(e => e.Status)
                .HasMaxLength(20)
                .HasDefaultValue("new")
                .HasColumnName("status");
            entity.Property(e => e.Subject)
                .HasMaxLength(100)
                .HasColumnName("subject");
        });

        modelBuilder.Entity<LoyaltyPointsLedger>(entity =>
        {
            entity.ToTable("loyalty_points_ledger");

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.BalanceAfter).HasColumnName("balance_after");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("(sysutcdatetime())")
                .HasColumnName("created_at");
            entity.Property(e => e.EntryType)
                .HasMaxLength(20)
                .HasColumnName("entry_type");
            entity.Property(e => e.Note)
                .HasMaxLength(255)
                .HasColumnName("note");
            entity.Property(e => e.OrderId).HasColumnName("order_id");
            entity.Property(e => e.PointsDelta).HasColumnName("points_delta");
            entity.Property(e => e.PromotionId).HasColumnName("promotion_id");
            entity.Property(e => e.UserId).HasColumnName("user_id");

            entity.HasOne(d => d.Order).WithMany(p => p.LoyaltyPointsLedgers)
                .HasForeignKey(d => d.OrderId)
                .HasConstraintName("FK_loyalty_points_ledger_orders");

            entity.HasOne(d => d.Promotion).WithMany(p => p.LoyaltyPointsLedgers)
                .HasForeignKey(d => d.PromotionId)
                .HasConstraintName("FK_loyalty_points_ledger_promotions");

            entity.HasOne(d => d.User).WithMany()
                .HasForeignKey(d => d.UserId)
                .HasConstraintName("FK_loyalty_points_ledger_users");
        });

        modelBuilder.Entity<LoyaltyWallet>(entity =>
        {
            entity.ToTable("loyalty_wallets");
            entity.HasKey(e => e.UserId);

            entity.Property(e => e.UserId)
                .ValueGeneratedNever()
                .HasColumnName("user_id");
            entity.Property(e => e.CurrentPoints).HasColumnName("current_points");
            entity.Property(e => e.LifetimeEarned).HasColumnName("lifetime_earned");
            entity.Property(e => e.LifetimeRedeemed).HasColumnName("lifetime_redeemed");
            entity.Property(e => e.UpdatedAt)
                .HasDefaultValueSql("(sysutcdatetime())")
                .HasColumnName("updated_at");

            entity.HasOne(d => d.User).WithOne()
                .HasForeignKey<LoyaltyWallet>(d => d.UserId)
                .HasConstraintName("FK_loyalty_wallets_users");
        });

        modelBuilder.Entity<Order>(entity =>
        {
            entity.ToTable("orders");

            entity.HasIndex(e => e.OrderNo).IsUnique();

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.Address).HasColumnName("address");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("(sysutcdatetime())")
                .HasColumnName("created_at");
            entity.Property(e => e.CustomerName)
                .HasMaxLength(100)
                .HasColumnName("customer_name");
            entity.Property(e => e.DeliveryFee)
                .HasColumnType("decimal(10, 2)")
                .HasColumnName("delivery_fee");
            entity.Property(e => e.DiscountAmount)
                .HasColumnType("decimal(10, 2)")
                .HasColumnName("discount_amount");
            entity.Property(e => e.DiscountCode)
                .HasMaxLength(40)
                .HasColumnName("discount_code");
            entity.Property(e => e.Note).HasColumnName("note");
            entity.Property(e => e.OrderNo)
                .HasMaxLength(30)
                .HasColumnName("order_no");
            entity.Property(e => e.OrderStatus)
                .HasMaxLength(20)
                .HasDefaultValue("paid")
                .HasColumnName("order_status");
            entity.Property(e => e.PaymentMethod)
                .HasMaxLength(30)
                .HasColumnName("payment_method");
            entity.Property(e => e.PaymentStatus)
                .HasMaxLength(20)
                .HasDefaultValue("paid")
                .HasColumnName("payment_status");
            entity.Property(e => e.Phone)
                .HasMaxLength(20)
                .HasColumnName("phone");
            entity.Property(e => e.PointsEarned).HasColumnName("points_earned");
            entity.Property(e => e.PointsRedeemed).HasColumnName("points_redeemed");
            entity.Property(e => e.PromoCodeId).HasColumnName("promo_code_id");
            entity.Property(e => e.ShippingDiscountAmount)
                .HasColumnType("decimal(10, 2)")
                .HasColumnName("shipping_discount_amount");
            entity.Property(e => e.Subtotal)
                .HasColumnType("decimal(10, 2)")
                .HasColumnName("subtotal");
            entity.Property(e => e.TotalAmount)
                .HasColumnType("decimal(10, 2)")
                .HasColumnName("total_amount");
            entity.Property(e => e.UserId).HasColumnName("user_id");

            entity.HasOne(d => d.PromoCode).WithMany(p => p.Orders)
                .HasForeignKey(d => d.PromoCodeId)
                .HasConstraintName("FK_orders_promo_codes");

            entity.HasOne(d => d.User).WithMany(p => p.Orders)
                .HasForeignKey(d => d.UserId)
                .HasConstraintName("FK_orders_users");
        });

        modelBuilder.Entity<OrderItem>(entity =>
        {
            entity.ToTable("order_items");

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.LineTotal)
                .HasColumnType("decimal(10, 2)")
                .HasColumnName("line_total");
            entity.Property(e => e.OrderId).HasColumnName("order_id");
            entity.Property(e => e.Price)
                .HasColumnType("decimal(10, 2)")
                .HasColumnName("price");
            entity.Property(e => e.ProductId).HasColumnName("product_id");
            entity.Property(e => e.ProductName)
                .HasMaxLength(100)
                .HasColumnName("product_name");
            entity.Property(e => e.Qty).HasColumnName("qty");

            entity.HasOne(d => d.Order).WithMany(p => p.OrderItems)
                .HasForeignKey(d => d.OrderId)
                .HasConstraintName("FK_order_items_orders");

            entity.HasOne(d => d.Product).WithMany(p => p.OrderItems)
                .HasForeignKey(d => d.ProductId)
                .HasConstraintName("FK_order_items_products");
        });

        modelBuilder.Entity<OrderPromotion>(entity =>
        {
            entity.ToTable("order_promotions");

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.BenefitType)
                .HasMaxLength(30)
                .HasColumnName("benefit_type");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("(sysutcdatetime())")
                .HasColumnName("created_at");
            entity.Property(e => e.DiscountAmount)
                .HasColumnType("decimal(10, 2)")
                .HasColumnName("discount_amount");
            entity.Property(e => e.Note)
                .HasMaxLength(255)
                .HasColumnName("note");
            entity.Property(e => e.OrderId).HasColumnName("order_id");
            entity.Property(e => e.PointsEarned).HasColumnName("points_earned");
            entity.Property(e => e.PointsRedeemed).HasColumnName("points_redeemed");
            entity.Property(e => e.PromoCodeId).HasColumnName("promo_code_id");
            entity.Property(e => e.PromotionId).HasColumnName("promotion_id");
            entity.Property(e => e.PromotionTitle)
                .HasMaxLength(120)
                .HasColumnName("promotion_title");
            entity.Property(e => e.RewardProductId).HasColumnName("reward_product_id");
            entity.Property(e => e.RewardProductName)
                .HasMaxLength(100)
                .HasColumnName("reward_product_name");
            entity.Property(e => e.RewardQty).HasColumnName("reward_qty");
            entity.Property(e => e.ShippingDiscountAmount)
                .HasColumnType("decimal(10, 2)")
                .HasColumnName("shipping_discount_amount");

            entity.HasOne(d => d.Order).WithMany(p => p.OrderPromotions)
                .HasForeignKey(d => d.OrderId)
                .HasConstraintName("FK_order_promotions_orders");

            entity.HasOne(d => d.PromoCode).WithMany(p => p.OrderPromotions)
                .HasForeignKey(d => d.PromoCodeId)
                .HasConstraintName("FK_order_promotions_promo_codes");

            entity.HasOne(d => d.Promotion).WithMany(p => p.OrderPromotions)
                .HasForeignKey(d => d.PromotionId)
                .HasConstraintName("FK_order_promotions_promotions");

            entity.HasOne(d => d.RewardProduct).WithMany()
                .HasForeignKey(d => d.RewardProductId)
                .HasConstraintName("FK_order_promotions_reward_products");
        });

        modelBuilder.Entity<Product>(entity =>
        {
            entity.ToTable("products");

            entity.HasIndex(e => e.Sku).IsUnique();

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.CategoryId).HasColumnName("category_id");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("(sysutcdatetime())")
                .HasColumnName("created_at");
            entity.Property(e => e.Description).HasColumnName("description");
            entity.Property(e => e.ImageUrl)
                .HasMaxLength(255)
                .HasColumnName("image_url");
            entity.Property(e => e.IsActive)
                .HasDefaultValue(true)
                .HasColumnName("is_active");
            entity.Property(e => e.Name)
                .HasMaxLength(100)
                .HasColumnName("name");
            entity.Property(e => e.Price)
                .HasColumnType("decimal(10, 2)")
                .HasColumnName("price");
            entity.Property(e => e.Sku)
                .HasMaxLength(50)
                .HasColumnName("sku");
            entity.Property(e => e.StockQty).HasColumnName("stock_qty");

            entity.HasOne(d => d.Category).WithMany(p => p.Products)
                .HasForeignKey(d => d.CategoryId)
                .HasConstraintName("FK_products_categories");
        });

        modelBuilder.Entity<PromoCode>(entity =>
        {
            entity.ToTable("promo_codes");

            entity.HasIndex(e => e.Code).IsUnique();

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.Code)
                .HasMaxLength(40)
                .HasColumnName("code");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("(sysutcdatetime())")
                .HasColumnName("created_at");
            entity.Property(e => e.Description)
                .HasMaxLength(255)
                .HasColumnName("description");
            entity.Property(e => e.DiscountType)
                .HasMaxLength(20)
                .HasColumnName("discount_type");
            entity.Property(e => e.DiscountValue)
                .HasColumnType("decimal(10, 2)")
                .HasColumnName("discount_value");
            entity.Property(e => e.ExpiresAt).HasColumnName("expires_at");
            entity.Property(e => e.MaxDiscountAmount)
                .HasColumnType("decimal(10, 2)")
                .HasColumnName("max_discount_amount");
            entity.Property(e => e.MinOrderAmount)
                .HasColumnType("decimal(10, 2)")
                .HasColumnName("min_order_amount");
            entity.Property(e => e.Note)
                .HasMaxLength(255)
                .HasColumnName("note");
            entity.Property(e => e.PromotionId).HasColumnName("promotion_id");
            entity.Property(e => e.StartsAt).HasColumnName("starts_at");
            entity.Property(e => e.Status)
                .HasMaxLength(20)
                .HasDefaultValue("draft")
                .HasColumnName("status");
            entity.Property(e => e.Title)
                .HasMaxLength(120)
                .HasColumnName("title");
            entity.Property(e => e.UsageLimit).HasColumnName("usage_limit");
            entity.Property(e => e.UsedCount).HasColumnName("used_count");

            entity.HasOne(d => d.Promotion).WithMany(p => p.PromoCodes)
                .HasForeignKey(d => d.PromotionId)
                .HasConstraintName("FK_promo_codes_promotions");
        });

        modelBuilder.Entity<Promotion>(entity =>
        {
            entity.ToTable("promotions");

            entity.HasIndex(e => e.PromotionKey).IsUnique();

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.AutoApply)
                .HasDefaultValue(true)
                .HasColumnName("auto_apply");
            entity.Property(e => e.BenefitType)
                .HasMaxLength(30)
                .HasColumnName("benefit_type");
            entity.Property(e => e.BuyQty).HasColumnName("buy_qty");
            entity.Property(e => e.CampaignType)
                .HasMaxLength(30)
                .HasColumnName("campaign_type");
            entity.Property(e => e.CanStack).HasColumnName("can_stack");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("(sysutcdatetime())")
                .HasColumnName("created_at");
            entity.Property(e => e.DailyEndTime).HasColumnName("daily_end_time");
            entity.Property(e => e.DailyStartTime).HasColumnName("daily_start_time");
            entity.Property(e => e.Description)
                .HasMaxLength(255)
                .HasColumnName("description");
            entity.Property(e => e.DiscountAmount)
                .HasColumnType("decimal(10, 2)")
                .HasColumnName("discount_amount");
            entity.Property(e => e.DiscountPercent)
                .HasColumnType("decimal(5, 2)")
                .HasColumnName("discount_percent");
            entity.Property(e => e.ExpiresAt).HasColumnName("expires_at");
            entity.Property(e => e.FreeShipping).HasColumnName("free_shipping");
            entity.Property(e => e.GetQty).HasColumnName("get_qty");
            entity.Property(e => e.MaxDiscountAmount)
                .HasColumnType("decimal(10, 2)")
                .HasColumnName("max_discount_amount");
            entity.Property(e => e.MinItemQty).HasColumnName("min_item_qty");
            entity.Property(e => e.MinOrderAmount)
                .HasColumnType("decimal(10, 2)")
                .HasColumnName("min_order_amount");
            entity.Property(e => e.Note)
                .HasMaxLength(255)
                .HasColumnName("note");
            entity.Property(e => e.PointsAwarded).HasColumnName("points_awarded");
            entity.Property(e => e.PointsCost).HasColumnName("points_cost");
            entity.Property(e => e.Priority)
                .HasDefaultValue(100)
                .HasColumnName("priority");
            entity.Property(e => e.PromotionKey)
                .HasMaxLength(50)
                .HasColumnName("promotion_key");
            entity.Property(e => e.RequiresCode).HasColumnName("requires_code");
            entity.Property(e => e.RewardCategoryId).HasColumnName("reward_category_id");
            entity.Property(e => e.RewardProductId).HasColumnName("reward_product_id");
            entity.Property(e => e.RewardQty).HasColumnName("reward_qty");
            entity.Property(e => e.RewardScope)
                .HasMaxLength(30)
                .HasColumnName("reward_scope");
            entity.Property(e => e.RuleType)
                .HasMaxLength(30)
                .HasColumnName("rule_type");
            entity.Property(e => e.SpendStepAmount)
                .HasColumnType("decimal(10, 2)")
                .HasColumnName("spend_step_amount");
            entity.Property(e => e.StartsAt).HasColumnName("starts_at");
            entity.Property(e => e.Status)
                .HasMaxLength(20)
                .HasDefaultValue("draft")
                .HasColumnName("status");
            entity.Property(e => e.TargetScope)
                .HasMaxLength(30)
                .HasDefaultValue("order")
                .HasColumnName("target_scope");
            entity.Property(e => e.Title)
                .HasMaxLength(120)
                .HasColumnName("title");
            entity.Property(e => e.WeekdayMask).HasColumnName("weekday_mask");

            entity.HasOne(d => d.RewardCategory).WithMany()
                .HasForeignKey(d => d.RewardCategoryId)
                .HasConstraintName("FK_promotions_reward_category");

            entity.HasOne(d => d.RewardProduct).WithMany()
                .HasForeignKey(d => d.RewardProductId)
                .HasConstraintName("FK_promotions_reward_product");
        });

        modelBuilder.Entity<PromotionTarget>(entity =>
        {
            entity.ToTable("promotion_targets");

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.CategoryId).HasColumnName("category_id");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("(sysutcdatetime())")
                .HasColumnName("created_at");
            entity.Property(e => e.ProductId).HasColumnName("product_id");
            entity.Property(e => e.PromotionId).HasColumnName("promotion_id");
            entity.Property(e => e.TargetType)
                .HasMaxLength(20)
                .HasColumnName("target_type");

            entity.HasOne(d => d.Category).WithMany()
                .HasForeignKey(d => d.CategoryId)
                .HasConstraintName("FK_promotion_targets_categories");

            entity.HasOne(d => d.Product).WithMany()
                .HasForeignKey(d => d.ProductId)
                .HasConstraintName("FK_promotion_targets_products");

            entity.HasOne(d => d.Promotion).WithMany(p => p.PromotionTargets)
                .HasForeignKey(d => d.PromotionId)
                .HasConstraintName("FK_promotion_targets_promotions");
        });

        modelBuilder.Entity<Role>(entity =>
        {
            entity.ToTable("roles");

            entity.HasIndex(e => e.RoleKey).IsUnique();

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("(sysutcdatetime())")
                .HasColumnName("created_at");
            entity.Property(e => e.RoleKey)
                .HasMaxLength(30)
                .HasColumnName("role_key");
            entity.Property(e => e.RoleName)
                .HasMaxLength(50)
                .HasColumnName("role_name");
        });

        modelBuilder.Entity<User>(entity =>
        {
            entity.ToTable("users");

            entity.HasIndex(e => e.Email).IsUnique();

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("(sysutcdatetime())")
                .HasColumnName("created_at");
            entity.Property(e => e.Email)
                .HasMaxLength(120)
                .HasColumnName("email");
            entity.Property(e => e.FullName)
                .HasMaxLength(100)
                .HasColumnName("full_name");
            entity.Property(e => e.LastActiveAt).HasColumnName("last_active_at");
            entity.Property(e => e.Notes)
                .HasMaxLength(255)
                .HasColumnName("notes");
            entity.Property(e => e.PasswordHash)
                .HasMaxLength(255)
                .HasColumnName("password_hash");
            entity.Property(e => e.Phone)
                .HasMaxLength(20)
                .HasColumnName("phone");
            entity.Property(e => e.RoleId).HasColumnName("role_id");
            entity.Property(e => e.Status)
                .HasMaxLength(20)
                .HasDefaultValue("active")
                .HasColumnName("status");

            entity.HasOne(d => d.Role).WithMany(p => p.Users)
                .HasForeignKey(d => d.RoleId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_users_roles");
        });

        modelBuilder.Entity<UserAddress>(entity =>
        {
            entity.ToTable("user_addresses");

            entity.HasIndex(e => e.UserId)
                .HasDatabaseName("IX_user_addresses_user_id");

            entity.HasIndex(e => e.UserId)
                .HasDatabaseName("UX_user_addresses_default_per_user")
                .IsUnique()
                .HasFilter("([is_default]=(1))");

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.AddressLine).HasColumnName("address_line");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("(sysutcdatetime())")
                .HasColumnName("created_at");
            entity.Property(e => e.IsDefault).HasColumnName("is_default");
            entity.Property(e => e.Label)
                .HasMaxLength(50)
                .HasColumnName("label");
            entity.Property(e => e.Phone)
                .HasMaxLength(20)
                .HasColumnName("phone");
            entity.Property(e => e.PostalCode)
                .HasMaxLength(20)
                .HasColumnName("postal_code");
            entity.Property(e => e.RecipientName)
                .HasMaxLength(100)
                .HasColumnName("recipient_name");
            entity.Property(e => e.UpdatedAt)
                .HasDefaultValueSql("(sysutcdatetime())")
                .HasColumnName("updated_at");
            entity.Property(e => e.UserId).HasColumnName("user_id");

            entity.HasOne(d => d.User).WithMany(p => p.UserAddresses)
                .HasForeignKey(d => d.UserId)
                .OnDelete(DeleteBehavior.Cascade)
                .HasConstraintName("FK_user_addresses_users");
        });

        OnModelCreatingPartial(modelBuilder);
    }

    partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
}
