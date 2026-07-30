FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY ["FluxGrid.Api.csproj", "."]
RUN dotnet restore "FluxGrid.Api.csproj"
COPY . .
RUN dotnet publish "FluxGrid.Api.csproj" -c Release -o /app/publish

FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
WORKDIR /app
EXPOSE 5020
COPY --from=build /app/publish .
RUN mkdir -p /app/uploads && chown -R 1000:1000 /app/uploads
USER dotnet
HEALTHCHECK CMD curl -f http://localhost:5020/api/health || exit 1
ENTRYPOINT ["dotnet", "FluxGrid.Api.dll"]
