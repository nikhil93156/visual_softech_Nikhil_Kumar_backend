-- ====================================================================
-- Database Creation Script for Student Management System
-- Target Database: StudentDB (Must be created in SSMS first)
-- ====================================================================

-- 1. Specify the target database
USE StudentDB;
GO

-- 2. Drop existing objects if they exist to allow clean re-execution
-- Dropping in reverse dependency order
IF OBJECT_ID('Student_Detail', 'U') IS NOT NULL DROP TABLE Student_Detail;
IF OBJECT_ID('Student_Mast', 'U') IS NOT NULL DROP TABLE Student_Mast;
IF OBJECT_ID('State_Name', 'U') IS NOT NULL DROP TABLE State_Name;
IF OBJECT_ID('Users', 'U') IS NOT NULL DROP TABLE Users;

IF OBJECT_ID('InsertState', 'P') IS NOT NULL DROP PROCEDURE InsertState;
IF OBJECT_ID('GetAllStudents', 'P') IS NOT NULL DROP PROCEDURE GetAllStudents;
GO

-- ====================================================================
-- Table Creation
-- ====================================================================

-- 1. Table for System Users (Login)
CREATE TABLE Users (
    UserId INT PRIMARY KEY IDENTITY(1,1),
    Username NVARCHAR(50) UNIQUE NOT NULL,
    -- Default password for 'admin' will be 'password123' (hash not implemented in mock)
    PasswordHash NVARCHAR(100) NOT NULL 
);
GO

-- 2. Table for State Names (Dynamic Dropdown source)
CREATE TABLE State_Name (
    StateId INT PRIMARY KEY IDENTITY(1,1),
    StateName NVARCHAR(100) UNIQUE NOT NULL
);
GO

-- 3. Student Master Table (General Student Data)
CREATE TABLE Student_Mast (
    StudentId INT PRIMARY KEY IDENTITY(1,1),
    Name NVARCHAR(100) NOT NULL,
    Age INT NOT NULL, -- Required (Int)
    DateOfBirth DATE NOT NULL, -- Required (Date)
    Address NVARCHAR(255), -- Not Required
    StateId INT NOT NULL, -- Required, Foreign Key (Int Dropdown)
    PhoneNumber NVARCHAR(20) NOT NULL, -- Required (Text with regex validation)
    PhotoPath NVARCHAR(255), 
    PhotoData VARBINARY(MAX), -- Max 2KB compressed photo data storage (Optional)
    CreatedAt DATETIME DEFAULT GETDATE(),
    UpdatedAt DATETIME DEFAULT GETDATE(),
    
    FOREIGN KEY (StateId) REFERENCES State_Name(StateId)
);
GO

-- 4. Student Detail Table (Subjects - Table with dynamic rows)
CREATE TABLE Student_Detail (
    DetailId INT PRIMARY KEY IDENTITY(1,1),
    StudentId INT NOT NULL,
    SubjectName NVARCHAR(100) NOT NULL,
    
    -- Crucial: ON DELETE CASCADE ensures subject details are removed when the student master record is deleted.
    FOREIGN KEY (StudentId) REFERENCES Student_Mast(StudentId) ON DELETE CASCADE
);
GO

-- ====================================================================
-- Initial Data Population
-- ====================================================================

-- Default Login User
INSERT INTO Users (Username, PasswordHash) VALUES
('admin', 'password123'); 

-- Initial States for the Dropdown
INSERT INTO State_Name (StateName) VALUES
('Maharashtra'),
('Karnataka'),
('Delhi'),
('Tamil Nadu');
GO

-- ====================================================================
-- Stored Procedures (APIs will call these for efficient DB operations)
-- ====================================================================

-- 1. SP to Insert a New State (for the 'Save State Name' modal)
CREATE PROCEDURE InsertState (
    @StateName NVARCHAR(100)
)
AS
BEGIN
    SET NOCOUNT ON;
    IF NOT EXISTS (SELECT 1 FROM State_Name WHERE StateName = @StateName)
    BEGIN
        INSERT INTO State_Name (StateName) VALUES (@StateName);
        SELECT SCOPE_IDENTITY() AS StateId; -- Return the new ID
    END
    ELSE
    BEGIN
        SELECT -1 AS StateId; -- Signal that the state already exists
    END
END
GO

-- 2. SP to Get All Students with State Name (for Index Page listing)
CREATE PROCEDURE GetAllStudents
AS
BEGIN
    SET NOCOUNT ON;
    SELECT
        sm.StudentId,
        sm.Name,
        sm.Age,
        sm.DateOfBirth,
        sm.Address,
        sm.PhoneNumber,
        sn.StateName -- Joined State Name
    FROM
        Student_Mast sm
    JOIN
        State_Name sn ON sm.StateId = sn.StateId
    ORDER BY
        sm.StudentId DESC;
END
GO