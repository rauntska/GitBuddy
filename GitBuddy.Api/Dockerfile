FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

COPY ["GitBuddy.Api/GitBuddy.Api.csproj", "GitBuddy.Api/"]
COPY ["GitBuddy.Domain/GitBuddy.Domain.csproj", "GitBuddy.Domain/"]

RUN dotnet restore "GitBuddy.Api/GitBuddy.Api.csproj"

COPY GitBuddy.Api/. GitBuddy.Api/
COPY GitBuddy.Domain/. GitBuddy.Domain/

WORKDIR "/src/GitBuddy.Api"
RUN dotnet publish "GitBuddy.Api.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS final
WORKDIR /app

EXPOSE 80
EXPOSE 443

COPY --from=build /app/publish .

ENTRYPOINT ["dotnet", "GitBuddy.Api.dll"]
