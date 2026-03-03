FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY Chummer.Core/Chummer.Core.csproj Chummer.Core/
COPY Chummer.Web/Chummer.Web.csproj Chummer.Web/
RUN dotnet restore Chummer.Web/Chummer.Web.csproj

COPY Chummer.Core/ Chummer.Core/
COPY Chummer.Web/ Chummer.Web/
RUN dotnet publish Chummer.Web/Chummer.Web.csproj -c Release -o /out

FROM mcr.microsoft.com/dotnet/aspnet:10.0
WORKDIR /app
COPY --from=build /out/ .
COPY Chummer/data/ /app/data/
COPY Chummer/lang/ /app/lang/
EXPOSE 8080
ENTRYPOINT ["dotnet", "Chummer.Web.dll"]
