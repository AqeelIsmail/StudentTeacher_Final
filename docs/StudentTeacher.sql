USE master;
GO
CREATE DATABASE StudentTeacherDB;
GO
USE StudentTeacherDB;
GO

--User Table
CREATE TABLE Users (
    UserId INT IDENTITY(1,1) PRIMARY KEY,
    Email NVARCHAR(256) NOT NULL UNIQUE,
    PasswordHash NVARCHAR(500) NOT NULL,
    Role NVARCHAR(20) NOT NULL CHECK (Role IN ('Student', 'Teacher'))--check constraints
);
GO

--Student_Profile
CREATE TABLE Student_Profiles (
    StudentProfileId INT IDENTITY(1,1) PRIMARY KEY,
    UserId INT NOT NULL UNIQUE,
    StudentNumber NVARCHAR(30) NOT NULL UNIQUE,
    FirstName NVARCHAR(100) NOT NULL,
    LastName NVARCHAR(100) NOT NULL,
    ProfileImageUrl NVARCHAR(2048) NULL,
    FOREIGN KEY (UserId) REFERENCES Users(UserId)
);
GO

--Teacher_Profiles
CREATE TABLE Teacher_Profiles (
    TeacherProfileId INT IDENTITY(1,1) PRIMARY KEY,
    UserId INT NOT NULL UNIQUE,
    EmployeeNumber NVARCHAR(30) NOT NULL UNIQUE,
    FirstName NVARCHAR(100) NOT NULL,
    LastName NVARCHAR(100) NOT NULL,
    ProfileImageUrl NVARCHAR(2048) NULL,
    FOREIGN KEY (UserId) REFERENCES Users(UserId)
);
GO

--Course_Categories
CREATE TABLE Course_Categories (
    CourseCategoryId INT IDENTITY(1,1) PRIMARY KEY,
    Name NVARCHAR(100) NOT NULL UNIQUE,
    Description NVARCHAR(500) NULL
);
GO

--Courses
CREATE TABLE Courses (
    CourseId INT IDENTITY(1,1) PRIMARY KEY,
    TeacherProfileId INT NOT NULL,
    CourseCategoryId INT NOT NULL,
    Code NVARCHAR(30) NOT NULL UNIQUE,
    Title NVARCHAR(150) NOT NULL,
    Description NVARCHAR(1000) NULL,
    CourseImageUrl NVARCHAR(2048) NULL,
    CreatedAt DATETIME2 NOT NULL DEFAULT GETDATE(),
    FOREIGN KEY (TeacherProfileId) REFERENCES Teacher_Profiles(TeacherProfileId),
    FOREIGN KEY (CourseCategoryId) REFERENCES Course_Categories(CourseCategoryId)
);
GO

--Enrolments
CREATE TABLE Enrolments (
    EnrolmentId INT IDENTITY(1,1) PRIMARY KEY,
    StudentProfileId INT NOT NULL,
    CourseId INT NOT NULL,
    EnrolledAt DATETIME2 NOT NULL DEFAULT GETDATE(),
    Status NVARCHAR(20) NOT NULL DEFAULT 'Pending'
        CHECK (Status IN ('Pending', 'Confirmed', 'Cancelled')),
    FOREIGN KEY (StudentProfileId) REFERENCES Student_Profiles(StudentProfileId),
    FOREIGN KEY (CourseId) REFERENCES Courses(CourseId),
    UNIQUE (StudentProfileId, CourseId)
);
GO

--Course_Results 
CREATE TABLE Course_Results (
    CourseResultId INT IDENTITY(1,1) PRIMARY KEY,
    EnrolmentId INT NOT NULL UNIQUE,
    FinalMark DECIMAL(5,2) NOT NULL CHECK (FinalMark BETWEEN 0 AND 100),
    Outcome NVARCHAR(10) NOT NULL CHECK (Outcome IN ('Pass', 'Fail')),
    RecordedAt DATETIME2 NOT NULL DEFAULT GETDATE(),
    FOREIGN KEY (EnrolmentId) REFERENCES Enrolments(EnrolmentId),
    CHECK ((FinalMark >= 50 AND Outcome = 'Pass') OR
           (FinalMark < 50 AND Outcome = 'Fail'))
);
GO

--data
INSERT INTO Users (Email, PasswordHash, Role) VALUES
('teacher1@Tchmail.com', 'AQAAAAIAAYagAAAAEDdyDPv4IMoINo7j4zp2WJp5A3dc2AE7UCwwO5O2gJbHnXTRORPDgiR05pKE1dSMXQ==', 'Teacher'),
('teacher2@Tchmail.com', 'AQAAAAIAAYagAAAAEJf/p4AkMlWyyDZOuPZq1/uDFQwCUHQzo/tTwXkq9681HjYzFHhNeetotmLa1bR/Ew==', 'Teacher'),
('student1@Stmail.com', 'AQAAAAIAAYagAAAAENAIH2CzGb4kSnv99wj8r2gH5LUC6nyvtNo5W8tF8n9dB82TnQ735C4SB1mM0sBavA==', 'Student'),
('student2@Stmail.com', 'AQAAAAIAAYagAAAAEGXZRb7Y2mfoD/IMmYd9lOEjRs/nM09V4I08dqxuol4d/RUlf3VJwI06w0MHoHibDA==', 'Student');


INSERT INTO Teacher_Profiles (UserId, EmployeeNumber, FirstName, LastName) VALUES
(1, 'TCH001', 'Aqeel', 'Ismail'),
(2, 'TCH002', 'James', 'Pillay');

INSERT INTO Student_Profiles (UserId, StudentNumber, FirstName, LastName) VALUES
(3, 'STU001', 'Bart', 'Simpson'),
(4, 'STU002', 'Peter', 'Parker');

INSERT INTO Course_Categories (Name, Description) VALUES
('Programming', 'Programming and software development'),
('Databases', 'Database design and SQL'),
('Networking', 'Computer networking');

INSERT INTO Courses (TeacherProfileId, CourseCategoryId, Code, Title, Description, CreatedAt) VALUES
(1, 1, 'PROG6212', 'Programming 2B', 'Advanced C# and ASP.NET Core', '2026-02-02'),
(2, 2, 'DBAS6211', 'Database Systems', 'Relational databases and SQL Server', '2026-02-02'),
(2, 3, 'NETW6211', 'Computer Networks', 'Networking fundamentals', '2026-02-02');

INSERT INTO Enrolments (StudentProfileId, CourseId, EnrolledAt, Status) VALUES
(1, 1, '2026-02-09', 'Confirmed'),
(1, 2, '2026-02-10','Confirmed'),
(2, 1, '2026-10-02','Pending');

INSERT INTO Course_Results (EnrolmentId, FinalMark, Outcome, RecordedAt) VALUES
(1, 76.50, 'Pass', '2026-06-25');

--check tables
SELECT * FROM Users;
SELECT * FROM Student_Profiles;
SELECT * FROM Teacher_Profiles;
SELECT * FROM Course_Categories;
SELECT * FROM Courses;
SELECT * FROM Enrolments;
SELECT * FROM Course_Results;
GO
