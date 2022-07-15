FROM mcr.microsoft.com/dotnet/sdk:6.0 AS build-env
WORKDIR /app
ARG VERSION=1.0.0.0

# Copy everything
COPY . ./
RUN dotnet clean
# Restore as distinct layers
RUN dotnet restore
# Build and publish a release
RUN dotnet publish -c Release -o out -p:Version=$VERSION

# Build runtime image
FROM mcr.microsoft.com/dotnet/aspnet:6.0
WORKDIR /app
COPY --from=build-env /app/out .
ENTRYPOINT ["dotnet", "poc-ohcmp-minimalapi.dll"]
EXPOSE 8080
EXPOSE 9090
