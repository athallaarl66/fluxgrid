FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY ["FluxGrid.Api.csproj", "."]
RUN dotnet restore "FluxGrid.Api.csproj"
COPY . .
RUN dotnet publish "FluxGrid.Api.csproj" -c Release -o /app/publish

FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
WORKDIR /app
EXPOSE 5000
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "FluxGrid.Api.dll"]
