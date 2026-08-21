-- OrdersAndInventoryService - Database Initialization Script
-- Target: Microsoft SQL Server 2019+ / Azure SQL Database

IF NOT EXISTS (SELECT * FROM sys.databases WHERE name = 'OrdersAndInventoryDb')
BEGIN
    CREATE DATABASE [OrdersAndInventoryDb];
END
GO

USE [OrdersAndInventoryDb];
GO

-- 1. Create Products Table
IF OBJECT_ID(N'dbo.Products', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.Products (
        Id UNIQUEIDENTIFIER NOT NULL CONSTRAINT PK_Products PRIMARY KEY,
        Sku NVARCHAR(50) NOT NULL,
        Name NVARCHAR(200) NOT NULL,
        Price DECIMAL(18, 2) NOT NULL,
        Stock INT NOT NULL,
        CreatedAtUtc DATETIME2(7) NOT NULL,
        UpdatedAtUtc DATETIME2(7) NOT NULL,
        CONSTRAINT CK_Products_Stock_NonNegative CHECK (Stock >= 0),
        CONSTRAINT CK_Products_Price_NonNegative CHECK (Price >= 0)
    );

    CREATE UNIQUE NONCLUSTERED INDEX IX_Products_Sku ON dbo.Products(Sku);
END
GO

-- 2. Create Orders Table
IF OBJECT_ID(N'dbo.Orders', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.Orders (
        Id UNIQUEIDENTIFIER NOT NULL CONSTRAINT PK_Orders PRIMARY KEY,
        ExternalOrderId NVARCHAR(100) NOT NULL,
        PlacedAtUtc DATETIME2(7) NOT NULL,
        CreatedAtUtc DATETIME2(7) NOT NULL,
        TotalAmount DECIMAL(18, 2) NOT NULL,
        Status INT NOT NULL
    );

    CREATE UNIQUE NONCLUSTERED INDEX IX_Orders_ExternalOrderId ON dbo.Orders(ExternalOrderId);
    CREATE NONCLUSTERED INDEX IX_Orders_PlacedAtUtc ON dbo.Orders(PlacedAtUtc);
END
GO

-- 3. Create OrderItems Table
IF OBJECT_ID(N'dbo.OrderItems', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.OrderItems (
        Id UNIQUEIDENTIFIER NOT NULL CONSTRAINT PK_OrderItems PRIMARY KEY,
        OrderId UNIQUEIDENTIFIER NOT NULL,
        ProductSku NVARCHAR(50) NOT NULL,
        Quantity INT NOT NULL,
        UnitPrice DECIMAL(18, 2) NOT NULL,
        TotalPrice DECIMAL(18, 2) NOT NULL,
        CONSTRAINT FK_OrderItems_Orders_OrderId FOREIGN KEY (OrderId) 
            REFERENCES dbo.Orders(Id) ON DELETE CASCADE
    );

    CREATE NONCLUSTERED INDEX IX_OrderItems_ProductSku ON dbo.OrderItems(ProductSku);
    CREATE NONCLUSTERED INDEX IX_OrderItems_OrderId_ProductSku ON dbo.OrderItems(OrderId, ProductSku);
END
GO
