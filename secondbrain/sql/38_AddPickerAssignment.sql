-- Migration 38: Kommissionierer-Zuweisung
-- Users: IsPicker Flag
IF COL_LENGTH('Users', 'IsPicker') IS NULL
BEGIN
    ALTER TABLE Users ADD IsPicker BIT NOT NULL CONSTRAINT DF_Users_IsPicker DEFAULT 0;
END
GO

-- ProductionOrders: Zugewiesener Kommissionierer
IF COL_LENGTH('ProductionOrders', 'AssignedPickerId') IS NULL
BEGIN
    ALTER TABLE ProductionOrders ADD AssignedPickerId INT NULL;
END
GO

IF COL_LENGTH('ProductionOrders', 'AssignedPickerName') IS NULL
BEGIN
    ALTER TABLE ProductionOrders ADD AssignedPickerName NVARCHAR(200) NULL;
END
GO

-- FK-Constraint
IF OBJECT_ID('FK_ProductionOrders_AssignedPicker', 'F') IS NULL
BEGIN
    ALTER TABLE ProductionOrders
        ADD CONSTRAINT FK_ProductionOrders_AssignedPicker
        FOREIGN KEY (AssignedPickerId) REFERENCES Users(Id) ON DELETE SET NULL;
END
GO

-- Index fuer Picking-Liste Filter
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_ProductionOrders_AssignedPickerId')
BEGIN
    CREATE NONCLUSTERED INDEX IX_ProductionOrders_AssignedPickerId
        ON ProductionOrders (AssignedPickerId)
        WHERE AssignedPickerId IS NOT NULL;
END
GO

-- Bestehenden Release-Index erweitern um AssignedPickerId
DROP INDEX IF EXISTS IX_ProductionOrders_IsReleasedForPicking_IsDone ON ProductionOrders;
CREATE NONCLUSTERED INDEX IX_ProductionOrders_IsReleasedForPicking_IsDone
    ON ProductionOrders (IsReleasedForPicking, IsDone)
    INCLUDE (PickingPriority, ProductionDate, AssignedPickerId);
GO

-- AppSetting: Kommissionierung mit Anwenderzuweisung
IF NOT EXISTS (SELECT 1 FROM AppSettings WHERE [Key] = 'KommissionierungMitZuweisung')
BEGIN
    INSERT INTO AppSettings ([Key], Value, Description)
    VALUES ('KommissionierungMitZuweisung', 'false', 'Kommissionierung mit Anwenderzuweisung aktivieren');
END
GO

PRINT '38_AddPickerAssignment.sql abgeschlossen.';
GO
