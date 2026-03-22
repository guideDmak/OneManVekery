IF DB_ID(N'OneManVekeryDB') IS NULL
BEGIN
    CREATE DATABASE [OneManVekeryDB];
END
GO

USE [OneManVekeryDB];
GO

/*
    One Man Vekery
    Simple SQL Server schema

    Database: OneManVekeryDB
    Core tables:
    - roles
    - users
    - categories
    - products
    - orders
    - order_items
    - contact_messages
*/

IF OBJECT_ID(N'dbo.roles', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.roles (
        id INT IDENTITY(1,1) PRIMARY KEY,
        role_key NVARCHAR(30) NOT NULL UNIQUE,
        role_name NVARCHAR(50) NOT NULL,
        created_at DATETIME2 NOT NULL CONSTRAINT DF_roles_created_at DEFAULT SYSUTCDATETIME()
    );
END
GO

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

IF OBJECT_ID(N'dbo.users', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.users (
        id INT IDENTITY(1,1) PRIMARY KEY,
        full_name NVARCHAR(100) NOT NULL,
        email NVARCHAR(120) NOT NULL UNIQUE,
        password_hash NVARCHAR(255) NOT NULL,
        phone NVARCHAR(20) NULL,
        role_id INT NOT NULL,
        status NVARCHAR(20) NOT NULL CONSTRAINT DF_users_status DEFAULT N'active',
        created_at DATETIME2 NOT NULL CONSTRAINT DF_users_created_at DEFAULT SYSUTCDATETIME(),
        CONSTRAINT FK_users_roles
            FOREIGN KEY (role_id) REFERENCES dbo.roles(id)
    );
END
GO

IF OBJECT_ID(N'dbo.categories', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.categories (
        id INT IDENTITY(1,1) PRIMARY KEY,
        name NVARCHAR(50) NOT NULL UNIQUE
    );
END
GO

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

IF OBJECT_ID(N'dbo.products', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.products (
        id INT IDENTITY(1,1) PRIMARY KEY,
        category_id INT NULL,
        sku NVARCHAR(50) NOT NULL UNIQUE,
        name NVARCHAR(100) NOT NULL,
        description NVARCHAR(MAX) NULL,
        price DECIMAL(10,2) NOT NULL,
        stock_qty INT NOT NULL CONSTRAINT DF_products_stock_qty DEFAULT 0,
        image_url NVARCHAR(255) NULL,
        is_active BIT NOT NULL CONSTRAINT DF_products_is_active DEFAULT 1,
        created_at DATETIME2 NOT NULL CONSTRAINT DF_products_created_at DEFAULT SYSUTCDATETIME(),
        CONSTRAINT FK_products_categories
            FOREIGN KEY (category_id) REFERENCES dbo.categories(id)
    );
END
GO

IF OBJECT_ID(N'dbo.orders', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.orders (
        id INT IDENTITY(1,1) PRIMARY KEY,
        order_no NVARCHAR(30) NOT NULL UNIQUE,
        user_id INT NULL,
        customer_name NVARCHAR(100) NOT NULL,
        phone NVARCHAR(20) NOT NULL,
        address NVARCHAR(MAX) NOT NULL,
        payment_method NVARCHAR(30) NOT NULL,
        payment_status NVARCHAR(20) NOT NULL CONSTRAINT DF_orders_payment_status DEFAULT N'paid',
        order_status NVARCHAR(20) NOT NULL CONSTRAINT DF_orders_order_status DEFAULT N'paid',
        subtotal DECIMAL(10,2) NOT NULL CONSTRAINT DF_orders_subtotal DEFAULT 0,
        delivery_fee DECIMAL(10,2) NOT NULL CONSTRAINT DF_orders_delivery_fee DEFAULT 0,
        total_amount DECIMAL(10,2) NOT NULL CONSTRAINT DF_orders_total_amount DEFAULT 0,
        note NVARCHAR(MAX) NULL,
        created_at DATETIME2 NOT NULL CONSTRAINT DF_orders_created_at DEFAULT SYSUTCDATETIME(),
        CONSTRAINT FK_orders_users
            FOREIGN KEY (user_id) REFERENCES dbo.users(id)
    );
END
GO

IF OBJECT_ID(N'dbo.order_items', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.order_items (
        id INT IDENTITY(1,1) PRIMARY KEY,
        order_id INT NOT NULL,
        product_id INT NULL,
        product_name NVARCHAR(100) NOT NULL,
        price DECIMAL(10,2) NOT NULL,
        qty INT NOT NULL,
        line_total DECIMAL(10,2) NOT NULL,
        CONSTRAINT FK_order_items_orders
            FOREIGN KEY (order_id) REFERENCES dbo.orders(id) ON DELETE CASCADE,
        CONSTRAINT FK_order_items_products
            FOREIGN KEY (product_id) REFERENCES dbo.products(id)
    );
END
GO

IF OBJECT_ID(N'dbo.contact_messages', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.contact_messages (
        id INT IDENTITY(1,1) PRIMARY KEY,
        name NVARCHAR(100) NOT NULL,
        email NVARCHAR(120) NOT NULL,
        phone NVARCHAR(20) NULL,
        subject NVARCHAR(100) NULL,
        message NVARCHAR(MAX) NOT NULL,
        status NVARCHAR(20) NOT NULL CONSTRAINT DF_contact_messages_status DEFAULT N'new',
        created_at DATETIME2 NOT NULL CONSTRAINT DF_contact_messages_created_at DEFAULT SYSUTCDATETIME()
    );
END
GO
