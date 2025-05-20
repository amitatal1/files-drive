# Use a .NET SDK image to build the application
# Choose the appropriate SDK version for your project
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build

WORKDIR /app

# Copy the application's .csproj file and restore dependencies
# This step is separated to leverage Docker's build cache
COPY ./Drive/Server/*.csproj ./
RUN dotnet restore

# Copy the rest of the application files
COPY ./Drive/Server/. .

# Publish the application for release
RUN dotnet publish -c Release -o out

# Use a smaller ASP.NET Core runtime image for the final stage
# This image only contains the necessary components to run the application
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final

WORKDIR /app

# Copy the published output from the build stage
COPY --from=build /app/out .

# Expose the port your application listens on
# Make sure this matches the port configured in your C# app (e.g., in appsettings.json or Program.cs)
# The default ASP.NET Core Kestrel port is 80 (HTTP) or 443 (HTTPS) in Docker
EXPOSE 123

# Set the entrypoint to run the application
# Replace 'YourServerProjectName' with the actual name of your server's executable/DLL
ENTRYPOINT ["dotnet", "FileDriveServer.dll"]
# Ensure the DLL name matches the output of your dotnet publish command
