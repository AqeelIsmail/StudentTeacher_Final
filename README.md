# StudentTeacher API

This is a PROG6212 teaching example showing how a Part 1 database design can be implemented as a Part 2 ASP.NET Core Web API. This is not the RaceDay POE solution; you must apply the same techniques to your own RaceDay requirements.



#### What the system does

The system supports two roles: Teacher and Student. Users can register, log in, log out and view/update their profiles. Teachers manage courses and related information, while students can browse and enrol in courses. The API also manages course categories, enrolment statuses and course results. Authorization restricts certain actions by role.

### 

#### Technologies

* C# / ASP.NET Core Web API (.NET 10)
* Entity Framework Core (Code First) with SQL Server Express
* Cookie authentication and ASP.NET Core Identity password hashing
* Swagger/OpenAPI for manual endpoint testing
* xUnit unit tests
* GitHub Actions (repository currently includes a documentation check workflow; add a separate build-and-test workflow if required)

## 

#### Requirements before running

1. Visual Studio with ASP.NET and web development workload, or a compatible editor and .NET 10 SDK.
2. SQL Server Express (or another accessible SQL Server instance).
3. SQL Server Management Studio (SSMS), for inspecting the database.

## 

### Set up the project

1. Clone or download this repository.
2. Open part2/Authentication\_API.slnx in Visual Studio.
3. Ensure Authentication\_API is the startup project.
4. Open part2/Authentication\_API/appsettings.json and update the DefaultConnection connection string for your SQL Server instance. The example uses localhost\\SQLEXPRESS and StudentTeacherMigrationTest; your server or database name may differ.
5. Build the solution so NuGet dependencies are restored.
6. In Visual Studio, open Tools > NuGet Package Manager > Package Manager Console. Select the Authentication\_API project as the default project and run:



&#x09;Update-Database



This applies the existing EF Core migration to the database named in the connection string. Do not create another initial migration when the repository already includes one. Do not execute the Part 1 CREATE TABLE script against the same database before applying migrations.

7. Open SSMS and verify the database contains Users, Student\_Profiles, Teacher\_Profiles, Course\_Categories, Courses, Enrolments, Course\_Results and \_\_EFMigrationsHistory.
8. Run the API using Visual Studio. In the development environment, open the Swagger page (usually /swagger). Use the actual localhost URL displayed by Visual Studio.
9. Register a new Teacher or Student account through POST /api/auth/register, then log in using POST /api/auth/login. Keep the login cookie for subsequent authorized requests in the same Swagger/browser session.
10. Try course/category/enrolment/results endpoints according to the user's role.

Important: Applying migrations creates the schema, not the sample records from a different local database. Register users and create any necessary categories/courses through the API. Password hashes copied from another database are not required for initial setup.



### API areas

Auth: Register, login, logout, view and update profile  

Categories: View and manage course categories 

Courses: List, view, create, update and delete courses; teacher's courses 

Enrolments: Enrol, view enrolments, change status and cancel/remove enrolments 

Results: Record, view and update course results 

Reacher/ Student: role-based authorization demonstrations 

DashBoard: Authentication-only 

Some controllers are demonstrations and not essential RaceDay features. See controller attributes and methods for the exact routes and permitted roles.

### 

### Running tests

In Visual Studio choose Test > Run All Tests. Or you can choose one and run



The repository includes xUnit tests. Passing tests do not replace manual checks that unauthenticated users receive 401 and users with the wrong role receive 403 where applicable.



### Part 1 to Part 2 learning path

1. Start with the ERD, SQL script and API endpoint plan in docs/.
2. Map each ERD entity to a C# model and its relationships.
3. Configure the schema in AppDbContext with DbSet<> and OnModelCreating.
4. Inspect the initial migration and apply it with Update-Database.
5. Configure SQL Server, cookie authentication and authorization in Program.cs.
6. Implement and test controller endpoints with Swagger.
7. Write and run unit tests; configure CI to build and test.



## Note

This example teaches the development process, not a ready-made submission. RaceDay uses Organiser/Participant, events, categories, enrolments and results. Refer to the official POE, memo and rubric for the exact RaceDay functionality, security requirements, tests and submission rules. Document your own design decisions.

