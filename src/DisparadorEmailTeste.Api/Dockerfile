FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS base
WORKDIR /app
EXPOSE 8080

FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src
COPY ["src/DisparadorEmailTeste.Api/DisparadorEmailTeste.Api.csproj", "src/DisparadorEmailTeste.Api/"]
COPY ["src/DisparadorEmailTeste.Dominio/DisparadorEmailTeste.Dominio.csproj", "src/DisparadorEmailTeste.Dominio/"]
COPY ["src/DisparadorEmailTeste.Infraestrutura/DisparadorEmailTeste.Infraestrutura.csproj", "src/DisparadorEmailTeste.Infraestrutura/"]
RUN dotnet restore "src/DisparadorEmailTeste.Api/DisparadorEmailTeste.Api.csproj"
COPY . .
RUN dotnet publish "src/DisparadorEmailTeste.Api/DisparadorEmailTeste.Api.csproj" -c Release -o /app/publish --no-restore

FROM base AS final
WORKDIR /app
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "DisparadorEmailTeste.Api.dll"]
