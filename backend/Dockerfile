FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

COPY GolfClub.sln .
COPY src/GolfClub.Domain/GolfClub.Domain.csproj src/GolfClub.Domain/
COPY src/GolfClub.Application/GolfClub.Application.csproj src/GolfClub.Application/
COPY src/GolfClub.Infrastructure/GolfClub.Infrastructure.csproj src/GolfClub.Infrastructure/
COPY src/GolfClub.Api/GolfClub.Api.csproj src/GolfClub.Api/
COPY tests/GolfClub.Domain.Tests/GolfClub.Domain.Tests.csproj tests/GolfClub.Domain.Tests/
COPY tests/GolfClub.Application.Tests/GolfClub.Application.Tests.csproj tests/GolfClub.Application.Tests/
RUN dotnet restore src/GolfClub.Api/GolfClub.Api.csproj

COPY . .
RUN dotnet publish src/GolfClub.Api/GolfClub.Api.csproj -c Release -o /app --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
WORKDIR /app
COPY --from=build /app .

ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080

ENTRYPOINT ["dotnet", "GolfClub.Api.dll"]
