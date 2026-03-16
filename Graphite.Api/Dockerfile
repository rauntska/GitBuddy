FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

COPY ["Graphite.Api/Graphite.Api.csproj", "Graphite.Api/"]
COPY ["Graphite.Domain/Graphite.Domain.csproj", "Graphite.Domain/"]

RUN dotnet restore "Graphite.Api/Graphite.Api.csproj"

COPY Graphite.Api/. Graphite.Api/
COPY Graphite.Domain/. Graphite.Domain/

WORKDIR "/src/Graphite.Api"
RUN dotnet publish "Graphite.Api.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS final
WORKDIR /app

EXPOSE 80
EXPOSE 443

COPY --from=build /app/publish .

ENTRYPOINT ["dotnet", "Graphite.Api.dll"]
