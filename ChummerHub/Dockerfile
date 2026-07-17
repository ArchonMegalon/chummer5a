#See https://aka.ms/containerfastmode to understand how Visual Studio uses this Dockerfile to build your images for faster debugging.

FROM mcr.microsoft.com/dotnet/aspnet:6.0 AS base
WORKDIR /app
EXPOSE 80
EXPOSE 443
EXPOSE 54562
EXPOSE 44321
EXPOSE 1433

FROM mcr.microsoft.com/dotnet/sdk:6.0 AS build
ARG CHUMMER_ALLOW_ARCHIVED_HUB_BUILD=0
RUN test "$CHUMMER_ALLOW_ARCHIVED_HUB_BUILD" = "1" || (echo "ChummerHub is an archived, unsupported .NET 6 compatibility asset; pass --build-arg CHUMMER_ALLOW_ARCHIVED_HUB_BUILD=1 only for isolated migration work." >&2; exit 1)
WORKDIR /src
COPY ["ChummerHub/ChummerHub.csproj", "ChummerHub/"]
RUN dotnet restore "ChummerHub/ChummerHub.csproj"
COPY . .
WORKDIR "/src/ChummerHub"
RUN dotnet build "ChummerHub.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "ChummerHub.csproj" -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "ChummerHub.dll"]
