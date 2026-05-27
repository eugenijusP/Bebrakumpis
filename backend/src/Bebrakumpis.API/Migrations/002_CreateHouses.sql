CREATE TABLE houses (
    id              UNIQUEIDENTIFIER NOT NULL DEFAULT NEWID() PRIMARY KEY,
    name            NVARCHAR(100)    NOT NULL UNIQUE,
    booking_color   NVARCHAR(7)      NOT NULL DEFAULT '#3b82f6',
    reserved_color  NVARCHAR(7)      NOT NULL DEFAULT '#ef4444',
    created_at      DATETIME2        NOT NULL DEFAULT SYSUTCDATETIME()
);

INSERT INTO houses (id, name, booking_color, reserved_color)
VALUES
    (NEWID(), 'Namas 1', '#3b82f6', '#ef4444'),
    (NEWID(), 'Namas 2', '#10b981', '#f59e0b');
