-- ==============================================================================
-- 001_CreateDatabase.sql
-- สร้าง Database และ Configure Performance Settings
-- ==============================================================================

USE [master];
GO

-- ==============================================================================
-- 1. Create Database with Best Practice File Allocation
-- ==============================================================================

CREATE DATABASE [EnterpriseWeb_DB]
CONTAINMENT = NONE
ON PRIMARY
(
    NAME = N'EnterpriseWeb_Data',
    -- เปลี่ยน Path เป็น Drive SSD/NVMe ที่เตรียมไว้สำหรับ Data Files
    FILENAME = N'YOUR_SSD_DRIVE_PATH\EnterpriseWeb_Data\SQLData\EnterpriseWeb_Data.mdf',
    SIZE = 1024MB,          -- Initial Size เริ่มต้นที่ 1GB
    MAXSIZE = UNLIMITED,
    FILEGROWTH = 512MB      -- Fixed Growth ทีละ 512MB (ห้ามใช้ % เด็ดขาด)
)
LOG ON
(
    NAME = N'EnterpriseWeb_Log',
    -- เปลี่ยน Path เป็น Drive SSD/NVMe ที่เตรียมไว้สำหรับ Log Files (ควรแยก Drive กับ Data)
    FILENAME = N'YOUR_SSD_DRIVE_PATH\EnterpriseWeb_Data\SQLLog\EnterpriseWeb_Log.ldf',
    SIZE = 512MB,            -- Initial Size เริ่มต้นที่ 512MB
    MAXSIZE = 2048GB,
    FILEGROWTH = 256MB       -- Fixed Growth ทีละ 256MB
)
-- ตั้งค่า Collation ให้รองรับการเรียงลำดับและการค้นหาภาษาไทยได้แม่นยำที่สุด
COLLATE Thai_100_CI_AS;
GO

-- ==============================================================================
-- 2. Performance & Concurrency Tuning (สำคัญมากสำหรับ Web App)
-- ==============================================================================

USE [master];
GO

-- 2.1 เปิดการทำงานของ RCSI (Read Committed Snapshot Isolation)
-- ช่วยให้คนที่กำลัง SELECT ข้อมูล จะไม่ถูก Block จากคนที่กำลัง UPDATE ข้อมูลอยู่ (อ่านข้อมูลจาก TempDB แทน)
-- WITH ROLLBACK IMMEDIATE ใช้เพื่อเตะ Connection ที่ค้างอยู่ออกไปก่อนชั่วคราวเพื่อให้รันคำสั่งนี้ผ่าน
ALTER DATABASE [EnterpriseWeb_DB] SET READ_COMMITTED_SNAPSHOT ON WITH ROLLBACK IMMEDIATE;
GO

-- 2.2 ตั้ง Recovery Model เป็น FULL
-- จำเป็นสำหรับการทำ Point-in-Time Restore (กู้ข้อมูลย้อนหลังระดับวินาที) และการทำ Always On Availability Groups
ALTER DATABASE [EnterpriseWeb_DB] SET RECOVERY FULL;
GO

-- ==============================================================================
-- 3. Monitoring & Diagnostics Setup
-- ==============================================================================

-- 3.1 เปิดใช้งาน Query Store (เหมือนกล่องดำเครื่องบิน ไว้เก็บสถิติ Query ที่ช้า)
ALTER DATABASE [EnterpriseWeb_DB] SET QUERY_STORE = ON
(
    OPERATION_MODE = READ_WRITE,
    CLEANUP_POLICY = (STALE_QUERY_THRESHOLD_DAYS = 30), -- เก็บข้อมูลไว้ 30 วัน
    DATA_FLUSH_INTERVAL_SECONDS = 900,
    MAX_STORAGE_SIZE_MB = 1024,                         -- จองพื้นที่ให้ Query Store 1GB
    INTERVAL_LENGTH_MINUTES = 60
);
GO

-- 3.2 บังคับให้ใช้พฤติกรรมของ SQL Server เวอร์ชันล่าสุดเสมอ (Compatibility Level)
-- ตัวเลข 160 คือ SQL Server 2022 (ปรับลดได้ตามเวอร์ชันที่ใช้จริง เช่น 150 = 2019)
ALTER DATABASE [EnterpriseWeb_DB] SET COMPATIBILITY_LEVEL = 160;
GO

PRINT 'Database [EnterpriseWeb_DB] has been successfully created with Enterprise configurations.';
GO
