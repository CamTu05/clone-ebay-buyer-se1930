SET XACT_ABORT ON;
BEGIN TRAN;
IF COL_LENGTH('dbo.User','authVersion') IS NULL ALTER TABLE dbo.[User] ADD authVersion int NOT NULL CONSTRAINT DF_User_AuthVersion DEFAULT 0;
IF COL_LENGTH('dbo.User','usernameChangedAt') IS NULL ALTER TABLE dbo.[User] ADD usernameChangedAt datetime2(3) NULL;
IF COL_LENGTH('dbo.CheckoutGroup','taxAmount') IS NULL ALTER TABLE dbo.CheckoutGroup ADD taxAmount decimal(18,2) NOT NULL CONSTRAINT DF_Group_Tax DEFAULT 0;
IF COL_LENGTH('dbo.OrderTable','taxAmount') IS NULL ALTER TABLE dbo.OrderTable ADD taxAmount decimal(18,2) NOT NULL CONSTRAINT DF_Order_Tax DEFAULT 0;
ALTER TABLE dbo.CheckoutGroup DROP CONSTRAINT CK_CheckoutGroup_Money;
EXEC(N'ALTER TABLE dbo.CheckoutGroup ADD CONSTRAINT CK_CheckoutGroup_Money CHECK(subtotal>=0 AND discountAmount>=0 AND discountAmount<=subtotal AND shippingFee>=0 AND taxAmount>=0 AND totalAmount=subtotal-discountAmount+shippingFee+taxAmount)');
ALTER TABLE dbo.OrderTable DROP CONSTRAINT CK_Order_Money;
EXEC(N'ALTER TABLE dbo.OrderTable ADD CONSTRAINT CK_Order_Money CHECK(subtotal>=0 AND discountAmount>=0 AND discountAmount<=subtotal AND shippingFee>=0 AND taxAmount>=0 AND totalAmount=subtotal-discountAmount+shippingFee+taxAmount)');
IF OBJECT_ID('dbo.OperationReceipt') IS NULL
CREATE TABLE dbo.OperationReceipt(scope nvarchar(256) NOT NULL, operationKey nvarchar(100) NOT NULL, requestHash nvarchar(64) NOT NULL, responseJson nvarchar(max) NOT NULL, createdAt datetime2 NOT NULL DEFAULT SYSUTCDATETIME(), CONSTRAINT PK_OperationReceipt PRIMARY KEY(scope,operationKey));
IF OBJECT_ID('dbo.EmailOutbox') IS NULL
CREATE TABLE dbo.EmailOutbox(id bigint IDENTITY PRIMARY KEY, recipient nvarchar(255) NOT NULL, subject nvarchar(200) NOT NULL, body nvarchar(max) NOT NULL, createdAt datetime2 NOT NULL DEFAULT SYSUTCDATETIME(), sentAt datetime2 NULL, attempts int NOT NULL DEFAULT 0);
COMMIT;
