# Build and test stage (run with: docker build --target test -f App.Api/Dockerfile .)
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src
COPY ["App.Api/App.Api.csproj", "App.Api/"]
COPY ["App.Adapters/App.Adapters.csproj", "App.Adapters/"]
COPY ["App.Application/App.Application.csproj", "App.Application/"]
COPY ["App.CrossCutting/App.CrossCutting.csproj", "App.CrossCutting/"]
COPY ["App.Domain/App.Domain.csproj", "App.Domain/"]
COPY ["TestProject/TestProject.csproj", "TestProject/"]
RUN dotnet restore "App.Api/App.Api.csproj" && dotnet restore "TestProject/TestProject.csproj"
COPY . .
RUN dotnet build "App.Api/App.Api.csproj" -c $BUILD_CONFIGURATION -o /app/build
RUN dotnet build "TestProject/TestProject.csproj" -c $BUILD_CONFIGURATION --no-restore

FROM build AS test
RUN dotnet test "TestProject/TestProject.csproj" -c $BUILD_CONFIGURATION --no-build --verbosity normal
WORKDIR /src
CMD ["dotnet", "test", "TestProject/TestProject.csproj", "-c", "Release", "--no-build", "--verbosity", "normal"]

# Publish and runtime (default target)
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS base
WORKDIR /app
EXPOSE 8080
EXPOSE 8081

FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "App.Api/App.Api.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "App.Api.dll"]
