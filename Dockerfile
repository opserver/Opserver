FROM mcr.microsoft.com/dotnet/core/sdk:3.1 AS build
WORKDIR /app

# Global
COPY ./*.sln ./nuget.config ./
# Apps
COPY src/*/*.csproj ./
RUN for file in $(ls *.csproj); do mkdir -p src/${file%.*}/ && mv $file src/${file%.*}/; done
# Tests
COPY tests/*/*.csproj ./
RUN for file in $(ls *.csproj); do mkdir -p tests/${file%.*}/ && mv $file tests/${file%.*}/; done

# Full Restore
RUN dotnet restore

# Copy and build
COPY . .
RUN dotnet build -c Release

# FROM build AS test-runner
# WORKDIR /app/tests/Opserver.Tests
# ENTRYPOINT dotnet test --results-directory /app/artifacts --logger:trx

FROM build AS web-publish
WORKDIR /app/src/Opserver.Web
RUN dotnet publish -c Release -o publish

# Build runtime image
FROM mcr.microsoft.com/dotnet/core/aspnet:3.1 AS web
WORKDIR /app
COPY --from=web-publish /app/src/Opserver.Web/publish ./
ENTRYPOINT ["dotnet", "Opserver.Web.dll"]