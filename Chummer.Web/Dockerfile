FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY Chummer.Web/Chummer.Web.csproj Chummer.Web/
RUN dotnet restore Chummer.Web/Chummer.Web.csproj

COPY Chummer.Web/ Chummer.Web/
RUN dotnet publish Chummer.Web/Chummer.Web.csproj -c Release -o /out

FROM mcr.microsoft.com/dotnet/aspnet:10.0
WORKDIR /app
COPY --from=build /out/ .
EXPOSE 8080
ENTRYPOINT ["dotnet", "Chummer.Web.dll"]
