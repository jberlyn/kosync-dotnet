# Build environment
FROM mcr.microsoft.com/dotnet/sdk:10.0-alpine AS build-env
WORKDIR /app
COPY . ./
RUN dotnet restore
RUN dotnet publish -c Release -o output

# Runtime environment
FROM mcr.microsoft.com/dotnet/aspnet:10.0-alpine
WORKDIR /app
COPY --from=build-env /app/output .
ENTRYPOINT ["dotnet", "Kosync.dll"]