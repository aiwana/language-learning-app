USE EnglishShadowingDB;
GO

SET NOCOUNT ON;
SET XACT_ABORT ON;
GO

-- Tạo (hoặc khôi phục) tài khoản quản trị dùng cho môi trường phát triển.
-- Script chạy lại nhiều lần được: đã tồn tại email thì cập nhật, chưa có thì tạo mới.
--
-- Mật khẩu mặc định: Admin@12345
-- @PasswordHash bên dưới là chuỗi do Microsoft.AspNetCore.Identity.PasswordHasher<T>
-- sinh ra (định dạng v3: PBKDF2-HMACSHA512, 100.000 vòng lặp, salt 128-bit), đúng thuật
-- toán mà AuthService.LoginAsync dùng để xác thực. KHÔNG tự gõ mật khẩu thô vào đây.
--
-- Muốn đổi sang mật khẩu khác thì sinh hash mới bằng đoạn C# sau rồi dán vào @PasswordHash:
--     var hasher = new PasswordHasher<object>();
--     Console.WriteLine(hasher.HashPassword(new object(), "mat-khau-moi"));
--
-- CẢNH BÁO: đây là tài khoản dành riêng cho máy local. Tuyệt đối không chạy script này
-- trên môi trường production.

DECLARE @Email        varchar(255) = 'admin@webshadowing.local';
DECLARE @Username     varchar(50)  = 'admin';
DECLARE @FullName     varchar(255) = 'Administrator';
DECLARE @PasswordHash varchar(255) = 'AQAAAAIAAYagAAAAEB3nBNkG8ayihbYD4SYq34yDshguCsKs8rptd90pV+ly9OaO3c4mr1T2x+aaXDV9iw==';

BEGIN TRANSACTION;

IF EXISTS (SELECT 1 FROM dbo.Users WHERE email = @Email)
BEGIN
    UPDATE dbo.Users
    SET password_hash        = @PasswordHash,
        role                 = 'admin',
        is_active            = 1,
        onboarding_completed = 1,
        disabled_at          = NULL,
        disabled_reason      = NULL,
        disabled_by_user_id  = NULL,
        updated_at           = SYSUTCDATETIME()
    WHERE email = @Email;
END
ELSE
BEGIN
    -- is_vip = 1 để tài khoản admin test được cả các tính năng dành cho VIP.
    -- Đổi thành 0 nếu muốn admin là tài khoản thường.
    INSERT INTO dbo.Users (username, email, password_hash, full_name, learning_mode,
                           pronunciation_target, accent, is_vip, onboarding_completed,
                           role, is_active, created_at, updated_at)
    VALUES (@Username, @Email, @PasswordHash, @FullName, 'casual',
            70, 'en-us', 1, 1,
            'admin', 1, SYSUTCDATETIME(), SYSUTCDATETIME());
END

DECLARE @UserId bigint = (SELECT user_id FROM dbo.Users WHERE email = @Email);

-- Luồng đăng ký thật luôn tạo kèm một dòng thống kê; thiếu dòng này thì các màn hình
-- gamification (tim, exp, streak) sẽ lỗi khi admin đăng nhập.
IF NOT EXISTS (SELECT 1 FROM dbo.User_Statistics WHERE user_id = @UserId)
BEGIN
    INSERT INTO dbo.User_Statistics (user_id, total_sessions, average_score, streak_days, hearts, exp)
    VALUES (@UserId, 0, 0, 0, 5, 0);
END

COMMIT TRANSACTION;
GO

SELECT u.user_id,
       u.username,
       u.email,
       u.role,
       u.is_active,
       u.is_vip,
       u.onboarding_completed,
       (SELECT COUNT(*) FROM dbo.User_Statistics s WHERE s.user_id = u.user_id) AS stats_rows
FROM dbo.Users AS u
WHERE u.email = 'admin@webshadowing.local';
GO
