FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY Chummer.Contracts/Chummer.Contracts.csproj Chummer.Contracts/
COPY Chummer.Presentation/Chummer.Presentation.csproj Chummer.Presentation/
COPY Chummer.Rulesets.Hosting/Chummer.Rulesets.Hosting.csproj Chummer.Rulesets.Hosting/
COPY Chummer.Rulesets.Sr5/Chummer.Rulesets.Sr5.csproj Chummer.Rulesets.Sr5/
COPY Chummer.Rulesets.Sr6/Chummer.Rulesets.Sr6.csproj Chummer.Rulesets.Sr6/
COPY Chummer.Blazor/Chummer.Blazor.csproj Chummer.Blazor/
RUN dotnet restore Chummer.Blazor/Chummer.Blazor.csproj

COPY Chummer.Contracts/ Chummer.Contracts/
COPY Chummer.Presentation/ Chummer.Presentation/
COPY Chummer.Rulesets.Hosting/ Chummer.Rulesets.Hosting/
COPY Chummer.Rulesets.Sr5/ Chummer.Rulesets.Sr5/
COPY Chummer.Rulesets.Sr6/ Chummer.Rulesets.Sr6/
COPY Chummer.Blazor/ Chummer.Blazor/
RUN dotnet publish Chummer.Blazor/Chummer.Blazor.csproj -c Release -o /out

FROM mcr.microsoft.com/dotnet/aspnet:10.0
WORKDIR /app
COPY --from=build /out/ .
EXPOSE 8080
ENTRYPOINT ["dotnet", "Chummer.Blazor.dll"]
