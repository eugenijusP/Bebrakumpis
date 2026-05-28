CREATE TABLE bookings (
    id           UNIQUEIDENTIFIER NOT NULL PRIMARY KEY DEFAULT NEWID(),
    house_id     UNIQUEIDENTIFIER NOT NULL REFERENCES houses(id),
    type         NVARCHAR(1)      NOT NULL,
    start_date   DATE             NOT NULL,
    end_date     DATE             NOT NULL,
    display_text NVARCHAR(50)     NOT NULL,
    notes        NVARCHAR(1000)   NULL,
    created_by   UNIQUEIDENTIFIER NOT NULL REFERENCES users(id),
    created_at   DATETIME2        NOT NULL DEFAULT GETUTCDATE()
);
