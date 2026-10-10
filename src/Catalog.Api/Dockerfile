# Stage 1: Runtime environment (.NET 10 LTS ASP.NET Core runtime)
# Uses the slim runtime image containing only what's needed to execute the compiled app.
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS base
USER $APP_UID
WORKDIR /app
EXPOSE 5001
ENV ASPNETCORE_HTTP_PORTS=5001
ENV ASPNETCORE_URLS=http://+:5001

# Stage 2: SDK build environment (.NET 10 LTS SDK)
# Contains the full .NET 10 SDK required for compiling and restoring NuGet packages.
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src

# Copy project definition first to leverage Docker layer caching for restored packages
COPY ["src/Catalog.Api/Catalog.Api.csproj", "src/Catalog.Api/"]
RUN dotnet restore "src/Catalog.Api/Catalog.Api.csproj"

# Copy remaining source code and build
COPY ["src/Catalog.Api/", "src/Catalog.Api/"]
WORKDIR "/src/src/Catalog.Api"
RUN dotnet build "Catalog.Api.csproj" -c $BUILD_CONFIGURATION -o /app/build

# Stage 3: Publish optimized production binaries
FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "Catalog.Api.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

# Stage 4: Production lean runtime image
# Copies only the compiled output from the publish stage, leaving behind the heavy SDK.
FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "Catalog.Api.dll"]
