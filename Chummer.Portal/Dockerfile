FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY Chummer.Contracts/Chummer.Contracts.csproj Chummer.Contracts/
COPY Chummer.Portal/Chummer.Portal.csproj Chummer.Portal/
RUN dotnet restore Chummer.Portal/Chummer.Portal.csproj

COPY Chummer.Contracts/ Chummer.Contracts/
COPY Chummer.Portal/ Chummer.Portal/
RUN dotnet publish Chummer.Portal/Chummer.Portal.csproj -c Release -o /out

FROM mcr.microsoft.com/dotnet/aspnet:10.0
WORKDIR /app
COPY --from=build /out/ .
EXPOSE 8080
ENTRYPOINT ["dotnet", "Chummer.Portal.dll"]
