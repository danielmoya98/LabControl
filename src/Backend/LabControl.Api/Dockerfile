# 1. Stage de Construcción
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY ["src/Backend/LabControl.Api/LabControl.Api.csproj", "src/Backend/LabControl.Api/"]
COPY ["src/Backend/LabControl.Infrastructure/LabControl.Infrastructure.csproj", "src/Backend/LabControl.Infrastructure/"]
COPY ["src/Backend/LabControl.Application/LabControl.Application.csproj", "src/Backend/LabControl.Application/"]
COPY ["src/Backend/LabControl.Domain/LabControl.Domain.csproj", "src/Backend/LabControl.Domain/"]

RUN dotnet restore "src/Backend/LabControl.Api/LabControl.Api.csproj"

COPY . .
WORKDIR "/src/src/Backend/LabControl.Api"
RUN dotnet build "LabControl.Api.csproj" -c Release -o /app/build

# 2. Stage de Publicación
FROM build AS publish
RUN dotnet publish "LabControl.Api.csproj" -c Release -o /app/publish /p:UseAppHost=false

# 3. Stage Final de Ejecución (Runtime)
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app
EXPOSE 8080
EXPOSE 8081
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "LabControl.Api.dll"]
