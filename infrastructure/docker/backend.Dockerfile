FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY ["EbayClone.sln", "./"]
COPY ["backend/Buyer.API/Buyer.API.csproj", "backend/Buyer.API/"]
COPY ["backend/Buyer.Application/Buyer.Application.csproj", "backend/Buyer.Application/"]
COPY ["backend/Buyer.Domain/Buyer.Domain.csproj", "backend/Buyer.Domain/"]
COPY ["backend/Buyer.Infrastructure/Buyer.Infrastructure.csproj", "backend/Buyer.Infrastructure/"]
COPY ["tests/Buyer.UnitTests/Buyer.UnitTests.csproj", "tests/Buyer.UnitTests/"]
COPY ["tests/Buyer.IntegrationTests/Buyer.IntegrationTests.csproj", "tests/Buyer.IntegrationTests/"]
RUN dotnet restore "backend/Buyer.API/Buyer.API.csproj"
COPY . .
RUN dotnet publish "backend/Buyer.API/Buyer.API.csproj" -c Release -o /app/publish --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app
EXPOSE 8080
ENV ASPNETCORE_URLS=http://+:8080
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "Buyer.API.dll"]
