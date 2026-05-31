FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY src/Bebrakumpis.Domain/Bebrakumpis.Domain.csproj src/Bebrakumpis.Domain/
COPY src/Bebrakumpis.Application/Bebrakumpis.Application.csproj src/Bebrakumpis.Application/
COPY src/Bebrakumpis.Infrastructure/Bebrakumpis.Infrastructure.csproj src/Bebrakumpis.Infrastructure/
COPY src/Bebrakumpis.API/Bebrakumpis.API.csproj src/Bebrakumpis.API/

RUN dotnet restore src/Bebrakumpis.API/Bebrakumpis.API.csproj

COPY src/ src/

RUN dotnet publish src/Bebrakumpis.API/Bebrakumpis.API.csproj \
    -c Release \
    -o /app/publish \
    --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app

COPY --from=build /app/publish .

ENV ASPNETCORE_HTTP_PORTS=8080
EXPOSE 8080

ENTRYPOINT ["dotnet", "Bebrakumpis.API.dll"]
