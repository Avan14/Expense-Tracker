FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

COPY ExpenseTracker.sln ./
COPY src/ExpenseTracker.Domain/ExpenseTracker.Domain.csproj src/ExpenseTracker.Domain/
COPY src/ExpenseTracker.Application/ExpenseTracker.Application.csproj src/ExpenseTracker.Application/
COPY src/ExpenseTracker.Infrastructure/ExpenseTracker.Infrastructure.csproj src/ExpenseTracker.Infrastructure/
COPY src/ExpenseTracker.Api/ExpenseTracker.Api.csproj src/ExpenseTracker.Api/
COPY tests/ExpenseTracker.UnitTests/ExpenseTracker.UnitTests.csproj tests/ExpenseTracker.UnitTests/
COPY tests/ExpenseTracker.IntegrationTests/ExpenseTracker.IntegrationTests.csproj tests/ExpenseTracker.IntegrationTests/

RUN dotnet restore ExpenseTracker.sln

COPY . .
WORKDIR /src/src/ExpenseTracker.Api
RUN dotnet publish -c Release -o /app/publish --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS runtime
WORKDIR /app
EXPOSE 8080

ENV ASPNETCORE_URLS=http://+:8080
COPY --from=build /app/publish .

ENTRYPOINT ["dotnet", "ExpenseTracker.Api.dll"]
