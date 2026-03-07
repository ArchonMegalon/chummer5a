FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY Chummer.Contracts/Chummer.Contracts.csproj Chummer.Contracts/
COPY Chummer.Application/Chummer.Application.csproj Chummer.Application/
COPY Chummer.Presentation/Chummer.Presentation.csproj Chummer.Presentation/
COPY Chummer.Rulesets.Hosting/Chummer.Rulesets.Hosting.csproj Chummer.Rulesets.Hosting/
COPY Chummer.Infrastructure.Browser/Chummer.Infrastructure.Browser.csproj Chummer.Infrastructure.Browser/
COPY Chummer.Session.Web/Chummer.Session.Web.csproj Chummer.Session.Web/
RUN dotnet restore Chummer.Session.Web/Chummer.Session.Web.csproj

COPY Chummer.Contracts/ Chummer.Contracts/
COPY Chummer.Application/ Chummer.Application/
COPY Chummer.Presentation/ Chummer.Presentation/
COPY Chummer.Rulesets.Hosting/ Chummer.Rulesets.Hosting/
COPY Chummer.Infrastructure.Browser/ Chummer.Infrastructure.Browser/
COPY Chummer.Session.Web/ Chummer.Session.Web/
RUN dotnet publish Chummer.Session.Web/Chummer.Session.Web.csproj -c Release -o /out

FROM mcr.microsoft.com/dotnet/aspnet:10.0
WORKDIR /app
COPY --from=build /out/ .
EXPOSE 8080
ENTRYPOINT ["dotnet", "Chummer.Session.Web.dll"]
