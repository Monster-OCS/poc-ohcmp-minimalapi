#See https://aka.ms/containerfastmode to understand how Visual Studio uses this Dockerfile to build your images for faster debugging.

FROM mcr.microsoft.com/dotnet/aspnet:6.0 AS base
WORKDIR /app
EXPOSE 8080
EXPOSE 9090

FROM mcr.microsoft.com/dotnet/sdk:6.0 AS build
WORKDIR /src
COPY ["poc-ohcmp-minimalapi.csproj", "."]
RUN dotnet restore "poc-ohcmp-minimalapi.csproj"
COPY . .
WORKDIR "/src/"
RUN dotnet build "poc-ohcmp-minimalapi.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "poc-ohcmp-minimalapi.csproj" -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "poc-ohcmp-minimalapi.dll"]
