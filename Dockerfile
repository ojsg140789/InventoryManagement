FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY Inventory.Domain/Inventory.Domain.csproj Inventory.Domain/
COPY Inventory.Application/Inventory.Application.csproj Inventory.Application/
COPY Inventory.Infrastructure/Inventory.Infrastructure.csproj Inventory.Infrastructure/
COPY Inventory.Api/Inventory.Api.csproj Inventory.Api/
RUN dotnet restore Inventory.Api/Inventory.Api.csproj
COPY Inventory.Domain/ Inventory.Domain/
COPY Inventory.Application/ Inventory.Application/
COPY Inventory.Infrastructure/ Inventory.Infrastructure/
COPY Inventory.Api/ Inventory.Api/
RUN dotnet publish Inventory.Api/Inventory.Api.csproj -c Release --no-restore -o /app/publish /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app
COPY --from=build /app/publish .
ENV ASPNETCORE_HTTP_PORTS=8080
USER app
EXPOSE 8080
ENTRYPOINT ["dotnet", "Inventory.Api.dll"]
