# StudentTeacher Final

This is the completed StudentTeacher example. You can compare it with **StudentTeacher Practical** to see how we added MVC pages, API calls, login, course management and image uploads.

It is a **reference for learning**, not a RaceDay POE solution. Use it to understand the techniques, then apply them to your own project.

## Run the project in Visual Studio

You need Visual Studio with .NET 10, SQL Server and Azurite for image uploads.

1. Open `StudentTeacher_Final.slnx`.
2. Check `part2/Authentication_API/appsettings.json` and change the SQL Server connection if needed.
3. Apply the existing EF Core migrations from the main folder:

```powershell
dotnet ef database update --project .\part2\Authentication_API\Authentication_API.csproj --startup-project .\part2\Authentication_API\Authentication_API.csproj
```

4. `appsettings.Development.json`. It contains the local **Azurite** settings used for image uploads. Start Azurite before testing uploads. Storage Explorer can be used to view the uploaded images. Image uploads: The API is configured to use Azurite for local image storage. Make sure Azurite is running before uploading profile or course pictures. You can use Microsoft Azure Storage Explorer to view the uploaded images.
5. Start `Authentication_API` using its HTTPS launch profile. Its API should run at `https://localhost:7261` and also listen on `http://localhost:5279`.
6. Start `StudentTeacher.Mvc` using its HTTPS profile. Open `https://localhost:7094`.
7. Register a Teacher or Student account and try the relevant features. A fresh database won't contain the accounts from someone else's computer.

## Run MVC using Docker

Start Docker Desktop and keep the API, SQL Server and Azurite running on Windows. From the **main project folder**, build the MVC image:

```powershell
docker build -t studentteachermvc:latest -f part3/StudentTeacher.Mvc/Dockerfile part3/StudentTeacher.Mvc
```

Then run the MVC container with this command:

```powershell
docker run -d --name studentteacher-final-mvc -p 8081:8080 -e DockerLocal=true -e ApiSettings__BaseUrl=http://host.docker.internal:5279/ -v studentteacher_final_keys:/app/keys studentteachermvc:latest
```

Open `http://localhost:8081`. You should be able to register, log in and use the website as you would in Visual Studio. The API still runs on Windows; **only MVC is inside Docker** in this example. The Docker volume saves MVC's security keys when the container is recreated.

If port 8081 is being used, change the **first** port in `-p 8081:8080` to an unused port. To stop or remove the container:

```powershell
docker stop studentteacher-final-mvc
docker rm studentteacher-final-mvc
```

Build the image again and create a new container after making changes to the MVC code. If the website opens but login fails, check that the API is running on port 5279 and check the Docker container logs.

## Remember for the POE

Your RaceDay Part 3 submission needs its **own** working MVC application, Dockerfile and `docker run` command in the README. Test the full website in Docker, not just the home page. Your README should also explain your own setup and include the documentation, references and AI-use links required by the POE instructions.

Docker Compose is covered separately in Learning Unit 6. It is not required by the Part 3 Docker marking section.
