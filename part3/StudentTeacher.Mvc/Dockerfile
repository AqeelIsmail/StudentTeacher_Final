# Stage 1: build and publish our .NET 10 MVC application.
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build

# Set the working folder inside the build stage.
WORKDIR /src

# Copy the project file first and download its NuGet packages.
# Docker can reuse this step when the project file has not changed.
COPY StudentTeacher.Mvc.csproj .
RUN dotnet restore StudentTeacher.Mvc.csproj

# Copy the remaining source files.
COPY . .

# Compile and prepare the application for running.
RUN dotnet publish StudentTeacher.Mvc.csproj \
    -c Release \
    -o /app/publish \
    --no-restore

# Stage 2: create the final image with the ASP.NET Core runtime.
# The SDK and build tools stay out of this final image.
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final

WORKDIR /app

# Tell ASP.NET Core to listen on HTTP port 8080.
ENV ASPNETCORE_HTTP_PORTS=8080

# Document the port used by the application.
EXPOSE 8080

# Copy the published application from the build stage.
COPY --from=build /app/publish .

# Run as the non-root application user provided by the .NET image.
USER $APP_UID

# Start the MVC application when the container starts.
ENTRYPOINT ["dotnet", "StudentTeacher.Mvc.dll"]