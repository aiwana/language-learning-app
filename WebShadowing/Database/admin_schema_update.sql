/*
Idempotent database update for admin panel support.
Adds user role / active flags and a lightweight admin audit log.
Run on the target SQL Server instance before enabling /Admin routes.

Bootstrap first admin (run once after deploy, replace the email):
  UPDATE dbo.Users SET role = 'admin', updated_at = SYSUTCDATETIME()
  WHERE email = N'your-admin@example.com';
*/

SET NOCOUNT ON;
GO

IF COL_LENGTH('dbo.Users', 'role') IS NULL
BEGIN
    ALTER TABLE dbo.Users ADD role VARCHAR(20) NOT NULL CONSTRAINT DF_Users_Role DEFAULT ('user');
END
ELSE
BEGIN
    IF NOT EXISTS (SELECT 1 FROM sys.default_constraints WHERE name = 'DF_Users_Role' AND parent_object_id = OBJECT_ID('dbo.Users'))
    BEGIN
        ALTER TABLE dbo.Users ADD CONSTRAINT DF_Users_Role DEFAULT ('user') FOR role;
    END
END
GO

IF NOT EXISTS (SELECT 1 FROM sys.check_constraints WHERE name = 'CK_Users_Role')
BEGIN
    ALTER TABLE dbo.Users WITH NOCHECK ADD CONSTRAINT CK_Users_Role CHECK (role IN ('user','admin'));
END
GO

IF COL_LENGTH('dbo.Users', 'is_active') IS NULL
BEGIN
    ALTER TABLE dbo.Users ADD is_active BIT NOT NULL CONSTRAINT DF_Users_IsActive DEFAULT (1);
END
ELSE
BEGIN
    IF NOT EXISTS (SELECT 1 FROM sys.default_constraints WHERE name = 'DF_Users_IsActive' AND parent_object_id = OBJECT_ID('dbo.Users'))
    BEGIN
        ALTER TABLE dbo.Users ADD CONSTRAINT DF_Users_IsActive DEFAULT (1) FOR is_active;
    END
END
GO

IF COL_LENGTH('dbo.Users', 'disabled_at') IS NULL
BEGIN
    ALTER TABLE dbo.Users ADD disabled_at DATETIME2 NULL;
END
GO

IF COL_LENGTH('dbo.Users', 'disabled_reason') IS NULL
BEGIN
    ALTER TABLE dbo.Users ADD disabled_reason NVARCHAR(500) NULL;
END
GO

IF COL_LENGTH('dbo.Users', 'disabled_by_user_id') IS NULL
BEGIN
    ALTER TABLE dbo.Users ADD disabled_by_user_id BIGINT NULL;
END
GO

IF NOT EXISTS (
    SELECT 1 FROM sys.foreign_keys
    WHERE name = 'FK_Users_DisabledBy' AND parent_object_id = OBJECT_ID('dbo.Users'))
BEGIN
    ALTER TABLE dbo.Users
        ADD CONSTRAINT FK_Users_DisabledBy
        FOREIGN KEY (disabled_by_user_id) REFERENCES dbo.Users(user_id);
END
GO

IF NOT EXISTS (
    SELECT 1 FROM sys.indexes
    WHERE object_id = OBJECT_ID('dbo.Users') AND name = 'IX_Users_Role_IsActive')
BEGIN
    CREATE INDEX IX_Users_Role_IsActive ON dbo.Users(role, is_active);
END
GO

IF OBJECT_ID('dbo.Admin_Audit_Log', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.Admin_Audit_Log (
        audit_id BIGINT IDENTITY(1,1) NOT NULL CONSTRAINT PK_Admin_Audit_Log PRIMARY KEY,
        actor_user_id BIGINT NOT NULL,
        target_user_id BIGINT NOT NULL,
        action VARCHAR(40) NOT NULL,
        detail NVARCHAR(1000) NULL,
        created_at DATETIME2 NOT NULL CONSTRAINT DF_AdminAuditLog_CreatedAt DEFAULT (SYSUTCDATETIME()),
        CONSTRAINT FK_AdminAuditLog_Actor FOREIGN KEY (actor_user_id) REFERENCES dbo.Users(user_id),
        CONSTRAINT FK_AdminAuditLog_Target FOREIGN KEY (target_user_id) REFERENCES dbo.Users(user_id),
        CONSTRAINT CK_AdminAuditLog_Action CHECK (action IN (
            'disable_user','enable_user','grant_vip','revoke_vip','set_role'))
    );

    CREATE INDEX IX_AdminAuditLog_Target_Created ON dbo.Admin_Audit_Log(target_user_id, created_at DESC);
    CREATE INDEX IX_AdminAuditLog_Actor_Created ON dbo.Admin_Audit_Log(actor_user_id, created_at DESC);
END
GO
