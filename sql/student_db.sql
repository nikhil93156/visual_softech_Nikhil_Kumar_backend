-- =============================================
-- Database Creation Script for Student Management System
-- Author: [Your Name]
-- Description: Creates Database, Tables, Foreign Keys, and Seed Data
-- =============================================

-- 1. Create Database (Check if it exists first to avoid errors)
IF NOT EXISTS (SELECT * FROM sys.databases WHERE name = 'StudentDB')
BEGIN
    CREATE DATABASE [StudentDB];
END
GO

USE [StudentDB];
GO

-- =============================================
-- 2. Create Tables
-- =============================================

-- Table: State_Mast
-- Stores the dynamic list of States for the dropdown
IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[State_Mast]') AND type in (N'U'))
BEGIN
    CREATE TABLE [dbo].[State_Mast](
        [StateId] [int] IDENTITY(1,1) NOT NULL,
        [StateName] [nvarchar](100) NOT NULL UNIQUE,
        PRIMARY KEY CLUSTERED ([StateId] ASC)
    );
END
GO

-- Table: Student_Mast
-- Stores the main student details and the photo
IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[Student_Mast]') AND type in (N'U'))
BEGIN
    CREATE TABLE [dbo].[Student_Mast](
        [StudentId] [int] IDENTITY(1,1) NOT NULL,
        [Name] [nvarchar](100) NOT NULL,
        [Age] [int] NOT NULL,
        [DateOfBirth] [date] NOT NULL,
        [Address] [nvarchar](max) NULL,
        [PhoneNumber] [nvarchar](20) NOT NULL,
        [StateId] [int] NOT NULL,
        [PhotoData] [varbinary](max) NULL, -- Stores the image as binary data
        PRIMARY KEY CLUSTERED ([StudentId] ASC)
    );
END
GO

-- Table: Student_Detail
-- Stores the subjects. One student can have multiple subjects.
IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[Student_Detail]') AND type in (N'U'))
BEGIN
    CREATE TABLE [dbo].[Student_Detail](
        [DetailId] [int] IDENTITY(1,1) NOT NULL,
        [StudentId] [int] NOT NULL,
        [SubjectName] [nvarchar](100) NOT NULL,
        PRIMARY KEY CLUSTERED ([DetailId] ASC)
    );
END
GO

-- =============================================
-- 3. Create Foreign Keys (Relationships)
-- =============================================

-- Link Student to State
-- If a State is deleted, we strictly prevent it if students are using it (No Action/Default)
IF NOT EXISTS (SELECT * FROM sys.foreign_keys WHERE object_id = OBJECT_ID(N'[dbo].[FK_Student_State]') AND parent_object_id = OBJECT_ID(N'[dbo].[Student_Mast]'))
BEGIN
    ALTER TABLE [dbo].[Student_Mast] WITH CHECK ADD CONSTRAINT [FK_Student_State] FOREIGN KEY([StateId])
    REFERENCES [dbo].[State_Mast] ([StateId]);
    
    ALTER TABLE [dbo].[Student_Mast] CHECK CONSTRAINT [FK_Student_State];
END
GO

-- Link Subjects to Student
-- ON DELETE CASCADE: If a Student is deleted, their subjects are automatically deleted.
IF NOT EXISTS (SELECT * FROM sys.foreign_keys WHERE object_id = OBJECT_ID(N'[dbo].[FK_Subject_Student]') AND parent_object_id = OBJECT_ID(N'[dbo].[Student_Detail]'))
BEGIN
    ALTER TABLE [dbo].[Student_Detail] WITH CHECK ADD CONSTRAINT [FK_Subject_Student] FOREIGN KEY([StudentId])
    REFERENCES [dbo].[Student_Mast] ([StudentId])
    ON DELETE CASCADE;
    
    ALTER TABLE [dbo].[Student_Detail] CHECK CONSTRAINT [FK_Subject_Student];
END
GO

-- =============================================
-- 4. Seed Data (Default Values)
-- =============================================

-- Insert default States so the dropdown is not empty on first run
INSERT INTO [dbo].[State_Mast] ([StateName])
SELECT 'Delhi' WHERE NOT EXISTS (SELECT 1 FROM State_Mast WHERE StateName = 'Delhi')
UNION ALL
SELECT 'Maharashtra' WHERE NOT EXISTS (SELECT 1 FROM State_Mast WHERE StateName = 'Maharashtra')
UNION ALL
SELECT 'Karnataka' WHERE NOT EXISTS (SELECT 1 FROM State_Mast WHERE StateName = 'Karnataka')
UNION ALL
SELECT 'Tamil Nadu' WHERE NOT EXISTS (SELECT 1 FROM State_Mast WHERE StateName = 'Tamil Nadu')
UNION ALL
SELECT 'Uttar Pradesh' WHERE NOT EXISTS (SELECT 1 FROM State_Mast WHERE StateName = 'Uttar Pradesh');
GO