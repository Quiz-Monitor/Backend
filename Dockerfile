# Use the official ASP.NET Core runtime as a base image
FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS base
USER app
WORKDIR /app
EXPOSE 8080

# Use the SDK image to build the app
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src

# Copy project files and restore dependencies
COPY ["QuizMonitor.API/QuizMonitor.API.csproj", "QuizMonitor.API/"]
COPY ["QuizMonitor.BLL/QuizMonitor.BLL.csproj", "QuizMonitor.BLL/"]
COPY ["QuizMonitor.DAL/QuizMonitor.DAL.csproj", "QuizMonitor.DAL/"]
RUN dotnet restore "./QuizMonitor.API/QuizMonitor.API.csproj"

# Copy the rest of the source code
COPY . .
WORKDIR "/src/QuizMonitor.API"

# Build the application
RUN dotnet build "./QuizMonitor.API.csproj" -c $BUILD_CONFIGURATION -o /app/build

# Publish the application
FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "./QuizMonitor.API.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

# Final stage/image
FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .

# Important: The .env file can be passed at runtime or copied if needed. 
# It is generally best practice NOT to bake .env files with secrets into the image.
ENTRYPOINT ["dotnet", "QuizMonitor.API.dll"]
