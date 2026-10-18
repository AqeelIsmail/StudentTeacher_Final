IF OBJECT_ID(N'[__EFMigrationsHistory]') IS NULL
BEGIN
    CREATE TABLE [__EFMigrationsHistory] (
        [MigrationId] nvarchar(150) NOT NULL,
        [ProductVersion] nvarchar(32) NOT NULL,
        CONSTRAINT [PK___EFMigrationsHistory] PRIMARY KEY ([MigrationId])
    );
END;
GO

BEGIN TRANSACTION;
CREATE TABLE [Course_Categories] (
    [CourseCategoryId] int NOT NULL IDENTITY,
    [Name] nvarchar(100) NOT NULL,
    [Description] nvarchar(500) NULL,
    CONSTRAINT [PK_Course_Categories] PRIMARY KEY ([CourseCategoryId])
);

CREATE TABLE [Users] (
    [UserId] int NOT NULL IDENTITY,
    [Email] nvarchar(256) NOT NULL,
    [PasswordHash] nvarchar(500) NOT NULL,
    [Role] nvarchar(20) NOT NULL,
    CONSTRAINT [PK_Users] PRIMARY KEY ([UserId]),
    CONSTRAINT [CK_Users_Role] CHECK ([Role] IN ('Student', 'Teacher'))
);

CREATE TABLE [Student_Profiles] (
    [StudentProfileId] int NOT NULL IDENTITY,
    [UserId] int NOT NULL,
    [StudentNumber] nvarchar(30) NOT NULL,
    [FirstName] nvarchar(100) NOT NULL,
    [LastName] nvarchar(100) NOT NULL,
    [ProfileImageUrl] nvarchar(2048) NULL,
    CONSTRAINT [PK_Student_Profiles] PRIMARY KEY ([StudentProfileId]),
    CONSTRAINT [FK_Student_Profiles_Users_UserId] FOREIGN KEY ([UserId]) REFERENCES [Users] ([UserId]) ON DELETE NO ACTION
);

CREATE TABLE [Teacher_Profiles] (
    [TeacherProfileId] int NOT NULL IDENTITY,
    [UserId] int NOT NULL,
    [EmployeeNumber] nvarchar(30) NOT NULL,
    [FirstName] nvarchar(100) NOT NULL,
    [LastName] nvarchar(100) NOT NULL,
    [ProfileImageUrl] nvarchar(2048) NULL,
    CONSTRAINT [PK_Teacher_Profiles] PRIMARY KEY ([TeacherProfileId]),
    CONSTRAINT [FK_Teacher_Profiles_Users_UserId] FOREIGN KEY ([UserId]) REFERENCES [Users] ([UserId]) ON DELETE NO ACTION
);

CREATE TABLE [Courses] (
    [CourseId] int NOT NULL IDENTITY,
    [TeacherProfileId] int NOT NULL,
    [CourseCategoryId] int NOT NULL,
    [Code] nvarchar(30) NOT NULL,
    [Title] nvarchar(150) NOT NULL,
    [Description] nvarchar(1000) NULL,
    [CourseImageUrl] nvarchar(2048) NULL,
    [CreatedAt] datetime2 NOT NULL DEFAULT (GETDATE()),
    CONSTRAINT [PK_Courses] PRIMARY KEY ([CourseId]),
    CONSTRAINT [FK_Courses_Course_Categories_CourseCategoryId] FOREIGN KEY ([CourseCategoryId]) REFERENCES [Course_Categories] ([CourseCategoryId]) ON DELETE NO ACTION,
    CONSTRAINT [FK_Courses_Teacher_Profiles_TeacherProfileId] FOREIGN KEY ([TeacherProfileId]) REFERENCES [Teacher_Profiles] ([TeacherProfileId]) ON DELETE NO ACTION
);

CREATE TABLE [Enrolments] (
    [EnrolmentId] int NOT NULL IDENTITY,
    [StudentProfileId] int NOT NULL,
    [CourseId] int NOT NULL,
    [EnrolledAt] datetime2 NOT NULL DEFAULT (GETDATE()),
    [Status] nvarchar(20) NOT NULL DEFAULT N'Pending',
    CONSTRAINT [PK_Enrolments] PRIMARY KEY ([EnrolmentId]),
    CONSTRAINT [CK_Enrolments_Status] CHECK ([Status] IN ('Pending', 'Confirmed', 'Cancelled')),
    CONSTRAINT [FK_Enrolments_Courses_CourseId] FOREIGN KEY ([CourseId]) REFERENCES [Courses] ([CourseId]) ON DELETE NO ACTION,
    CONSTRAINT [FK_Enrolments_Student_Profiles_StudentProfileId] FOREIGN KEY ([StudentProfileId]) REFERENCES [Student_Profiles] ([StudentProfileId]) ON DELETE NO ACTION
);

CREATE TABLE [Course_Results] (
    [CourseResultId] int NOT NULL IDENTITY,
    [EnrolmentId] int NOT NULL,
    [FinalMark] decimal(5,2) NOT NULL,
    [Outcome] nvarchar(10) NOT NULL,
    [RecordedAt] datetime2 NOT NULL DEFAULT (GETDATE()),
    CONSTRAINT [PK_Course_Results] PRIMARY KEY ([CourseResultId]),
    CONSTRAINT [CK_CourseResults_FinalMark] CHECK ([FinalMark] BETWEEN 0 AND 100),
    CONSTRAINT [CK_CourseResults_Outcome] CHECK (([FinalMark] >= 50 AND [Outcome] = 'Pass') OR ([FinalMark] < 50 AND [Outcome] = 'Fail')),
    CONSTRAINT [FK_Course_Results_Enrolments_EnrolmentId] FOREIGN KEY ([EnrolmentId]) REFERENCES [Enrolments] ([EnrolmentId]) ON DELETE NO ACTION
);

CREATE UNIQUE INDEX [IX_Course_Categories_Name] ON [Course_Categories] ([Name]);

CREATE UNIQUE INDEX [IX_Course_Results_EnrolmentId] ON [Course_Results] ([EnrolmentId]);

CREATE UNIQUE INDEX [IX_Courses_Code] ON [Courses] ([Code]);

CREATE INDEX [IX_Courses_CourseCategoryId] ON [Courses] ([CourseCategoryId]);

CREATE INDEX [IX_Courses_TeacherProfileId] ON [Courses] ([TeacherProfileId]);

CREATE INDEX [IX_Enrolments_CourseId] ON [Enrolments] ([CourseId]);

CREATE UNIQUE INDEX [IX_Enrolments_StudentProfileId_CourseId] ON [Enrolments] ([StudentProfileId], [CourseId]);

CREATE UNIQUE INDEX [IX_Student_Profiles_StudentNumber] ON [Student_Profiles] ([StudentNumber]);

CREATE UNIQUE INDEX [IX_Student_Profiles_UserId] ON [Student_Profiles] ([UserId]);

CREATE UNIQUE INDEX [IX_Teacher_Profiles_EmployeeNumber] ON [Teacher_Profiles] ([EmployeeNumber]);

CREATE UNIQUE INDEX [IX_Teacher_Profiles_UserId] ON [Teacher_Profiles] ([UserId]);

CREATE UNIQUE INDEX [IX_Users_Email] ON [Users] ([Email]);

INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
VALUES (N'20261012215628_InitialStudentTeacherSchema', N'10.0.11');

COMMIT;
GO

