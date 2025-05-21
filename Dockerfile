# Use a .NET SDK image to build the application
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build

# Set a source directory for all your project files
WORKDIR /src

# Copy all project files from the root of your context (where Dockerfile is)
# This includes the 'Drive' folder.
COPY . .

# Change the working directory to your specific project folder
# This is crucial so dotnet restore/publish runs in the correct context
WORKDIR /src/FilesDriveServer/

# Restore dependencies for your specific project
RUN dotnet restore

# Publish the application for release.
# Output to a known location: /app/out (outside the current WORKDIR, /src/Drive/Server)
RUN dotnet publish -c Release -o /app/out

# Use a smaller ASP.NET Core runtime image for the final stage
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final

# Set the working directory in the final image
WORKDIR /app

# Copy the published output from the build stage into the final image's /app directory
COPY --from=build /app/out .

# Expose the port your application listens on
EXPOSE 123

# Set the entrypoint to run the application
# Ensure 'FileDriveServer.dll' is the exact name of the DLL produced by dotnet publish
ENTRYPOINT ["dotnet", "FileDriveServer.dll"]