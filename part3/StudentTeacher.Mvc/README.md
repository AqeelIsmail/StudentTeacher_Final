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

## Part 3 — Running the MVC application in Docker (local development)

This repository includes `part3/StudentTeacher.Mvc`, a .NET 10 ASP.NET Core MVC application. The Dockerfile in that project builds the **MVC application only**. For this classroom example, the Part 2 API and its SQL Server database continue running on your Windows computer; they are **not** started by `docker run`. If you are testing profile/course image uploads, start Azurite using the API's existing local Azure Storage configuration as well.

### Before starting

1. Install and start **Docker Desktop**, with the **Linux container** engine running.
2. Set up the API and database using the Part 2 steps above. Start `Authentication_API` in Visual Studio using a profile that serves **HTTP on port 5279**. Check `http://localhost:5279/swagger/index.html` from your computer. Swagger is enabled in the Development environment.
3. The MVC container calls the API using `http://host.docker.internal:5279/`. `host.docker.internal` is Docker Desktop's hostname for reaching services on the computer running Docker. A working Swagger page on the host does not, by itself, prove that the API is reachable from the container.
4. Use an available **host** port. This example uses **8081** because port 8080 may already be used by another MVC practical. The MVC application listens on **container** port 8080 as configured in its Dockerfile.

### Build the MVC Docker image

From the **repository root** (`StudentTeacher_Final`, the folder containing `part2` and `part3`), open a terminal in Visual Studio (View > Terminal) or another terminal and run:

```bash
docker build -t studentteachermvc:latest -f part3/StudentTeacher.Mvc/Dockerfile part3/StudentTeacher.Mvc
```

This sets the build context to `part3/StudentTeacher.Mvc`, as required by the existing Dockerfile's `COPY StudentTeacher.Mvc.csproj .` instruction. The Dockerfile uses a .NET 10 SDK build stage and a smaller ASP.NET Core runtime stage. To verify the image was created, run `docker images` or open **Docker Desktop > Images > Local**.

### Start the MVC container — required `docker run` command

Run this **single command** after the image has been built and while the host API is running:

```bash
docker run -d --name studentteacher-final-mvc -p 8081:8080 -e DockerLocal=true -e ApiSettings__BaseUrl=http://host.docker.internal:5279/ studentteachermvc:latest
```

The options mean:

| Option | Meaning |
| --- | --- |
| `-d` | Start the container in the background. |
| `--name studentteacher-final-mvc` | Give the container a recognizable name. |
| `-p 8081:8080` | Forward the computer's port **8081** to the MVC container's HTTP port **8080**. |
| `-e DockerLocal=true` | Disable HTTPS redirection in MVC **for this local HTTP demonstration only**, using the existing `Program.cs` setting. |
| `-e ApiSettings__BaseUrl=http://host.docker.internal:5279/` | Override `ApiSettings:BaseUrl` so MVC can reach the API on the host instead of looking for it at `localhost` inside the container. |
| `studentteachermvc:latest` | The Docker image built in the previous step. |

Open **http://localhost:8081** in a browser. The normal local Visual Studio MVC address is different (`https://localhost:7094` or `http://localhost:5165`), and should not be used to test the running container.

**Docker Desktop alternative:** Open **Images > Local**, select `studentteachermvc:latest`, choose **Run > Optional settings**, set container name `studentteacher-final-mvc`, set host port **8081** for `8080/tcp`, and add the two environment variables above. Run the image and open `http://localhost:8081`. Using Docker Desktop's graphical interface does not replace the requirement to include a working `docker run` command in the README.

### Check that the application works

1. Open **Docker Desktop > Containers**, and confirm `studentteacher-final-mvc` is running with published port `8081:8080`. Or use `docker ps`.
2. Open `http://localhost:8081`. Verify the MVC page loads.
3. Confirm that the **host API is still running**, then exercise registration, login, Student and Teacher pages, course features, and logout. Compare these behaviours with the MVC app running directly from Visual Studio.
4. If testing image uploads, also ensure **Azurite** is available to the host API and check the relevant profile/course image feature.
5. Use **Docker Desktop > Containers > studentteacher-final-mvc > Logs** or `docker logs studentteacher-final-mvc` to investigate errors.

Only the homepage loading is **not enough** to demonstrate that the containerised, completed MVC application behaves the same as it does locally.

### Stop, restart, remove and rebuild

```bash
docker stop studentteacher-final-mvc
docker start studentteacher-final-mvc
docker logs studentteacher-final-mvc
```

When changing MVC source code, build the image again and **recreate** the container so it runs the updated code:

```bash
docker stop studentteacher-final-mvc
docker rm studentteacher-final-mvc
docker build -t studentteachermvc:latest -f part3/StudentTeacher.Mvc/Dockerfile part3/StudentTeacher.Mvc
docker run -d --name studentteacher-final-mvc -p 8081:8080 -e DockerLocal=true -e ApiSettings__BaseUrl=http://host.docker.internal:5279/ studentteachermvc:latest
```

**Notes and troubleshooting:**

- If Docker reports that the **container name already exists**, stop and remove that named container before running the command again. Do not delete the image unless you want to rebuild or reclaim space.
- If Docker reports that **host port 8081 is already in use**, choose another free host port, for example change `-p 8081:8080` to `-p 8082:8080` and open `http://localhost:8082`. Keep the container side at 8080.
- If the MVC page loads but API features fail, check the `ApiSettings__BaseUrl` environment variable and API logs. If the host API is bound only to a loopback interface that the container cannot reach, you may need to configure a suitable host-accessible HTTP development binding and Windows firewall rule on a **trusted network**. Do not expose a development API publicly.
- After creating a new container, existing session/authentication cookies may no longer work because ASP.NET Core Data Protection keys are not persisted by this simple configuration. Sign in again; discuss persistent volumes and key storage separately when covering Docker Compose.
- `DockerLocal=true` and the HTTP API URL are **classroom-only settings**. Production deployment requires appropriate HTTPS, secret management, access control and persistent key storage.

### How this relates to the POE

POE Part 3 requires the **finished MVC application** to run in a Docker container, a well-structured Dockerfile, a working `docker run` command documented in the README, and the **same application functionality as locally**. This StudentTeacher project is a teaching example, not a RaceDay submission. Students should adapt the image name, project paths, ports, environment variables and prerequisite services to their own **RaceDay MVC** application, and demonstrate the complete application in their submission video. **Docker Compose is a separate Learning Unit 6 topic**; it is not a substitute for the required individual `docker run` instructions.

Official references: [Docker Desktop host networking](https://docs.docker.com/desktop/features/networking/networking-how-tos/), [Docker run reference](https://docs.docker.com/reference/cli/docker/container/run), and [Microsoft ASP.NET Core Docker guidance](https://learn.microsoft.com/en-us/aspnet/core/host-and-deploy/docker/building-net-docker-images?view=aspnetcore-10.0).
