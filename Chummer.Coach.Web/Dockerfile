FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY Chummer.Contracts/Chummer.Contracts.csproj Chummer.Contracts/
COPY Chummer.Coach.Web/Chummer.Coach.Web.csproj Chummer.Coach.Web/
RUN dotnet restore Chummer.Coach.Web/Chummer.Coach.Web.csproj

COPY Chummer.Contracts/ Chummer.Contracts/
COPY Chummer.Coach.Web/ Chummer.Coach.Web/
RUN dotnet publish Chummer.Coach.Web/Chummer.Coach.Web.csproj -c Release -o /out

FROM mcr.microsoft.com/dotnet/aspnet:10.0
WORKDIR /app
COPY --from=build /out/ .
EXPOSE 8080
ENTRYPOINT ["dotnet", "Chummer.Coach.Web.dll"]
