-- ---------------------------------------------------------------------------
-- Reference schema for the Smart Home IoT Platform database.
-- This matches exactly what SmartHomeDbContext (Data/SmartHomeDbContext.cs) expects
-- to find via EF Core's Fluent API mappings. Use this to create the database by hand,
-- OR skip it and let EF Core create/migrate it for you (see README "Getting the database ready").
-- Engine/collation chosen to match the .NET/MySQL stack from the tech spec (MySQL 8, InnoDB).
-- ---------------------------------------------------------------------------

CREATE DATABASE IF NOT EXISTS smarthome
    CHARACTER SET utf8mb4
    COLLATE utf8mb4_unicode_ci;

USE smarthome;

CREATE TABLE IF NOT EXISTS Room (
    RoomID INT AUTO_INCREMENT PRIMARY KEY,
    Name   VARCHAR(100) NOT NULL,
    INDEX IX_Room_Name (Name)
) ENGINE=InnoDB;

CREATE TABLE IF NOT EXISTS Device (
    DeviceID         INT AUTO_INCREMENT PRIMARY KEY,
    Name             VARCHAR(100) NOT NULL,
    Type             VARCHAR(50)  NOT NULL,
    RoomID           INT          NOT NULL,
    MACAddress       VARCHAR(17)  NOT NULL,
    IPv4Address      VARCHAR(45)  NULL,
    Status           VARCHAR(20)  NOT NULL DEFAULT 'Online',
    RegistrationDate DATETIME     NOT NULL,
    LastSeen         DATETIME     NULL,
    CONSTRAINT UQ_Device_MACAddress UNIQUE (MACAddress),
    CONSTRAINT FK_Device_Room FOREIGN KEY (RoomID) REFERENCES Room(RoomID)
        ON DELETE RESTRICT
) ENGINE=InnoDB;

CREATE TABLE IF NOT EXISTS SensorData (
    DataID     BIGINT AUTO_INCREMENT PRIMARY KEY,
    DeviceID   INT            NOT NULL,
    SensorType VARCHAR(50)    NOT NULL,
    Value      DECIMAL(8,2)   NOT NULL,
    Unit       VARCHAR(10)    NOT NULL,
    Timestamp  DATETIME       NOT NULL,
    CONSTRAINT FK_SensorData_Device FOREIGN KEY (DeviceID) REFERENCES Device(DeviceID)
        ON DELETE CASCADE,
    INDEX IX_SensorData_DeviceID_Timestamp (DeviceID, Timestamp)
) ENGINE=InnoDB;

CREATE TABLE IF NOT EXISTS EventLog (
    EventID     BIGINT AUTO_INCREMENT PRIMARY KEY,
    DeviceID    INT          NULL,
    Event       VARCHAR(100) NOT NULL,
    Description VARCHAR(500) NULL,
    Timestamp   DATETIME     NOT NULL,
    CONSTRAINT FK_EventLog_Device FOREIGN KEY (DeviceID) REFERENCES Device(DeviceID)
        ON DELETE SET NULL,
    INDEX IX_EventLog_Timestamp (Timestamp)
) ENGINE=InnoDB;

-- Optional: a couple of rooms to get started with.
-- INSERT INTO Room (Name) VALUES ('Stue'), ('Køkken'), ('Soveværelse');
