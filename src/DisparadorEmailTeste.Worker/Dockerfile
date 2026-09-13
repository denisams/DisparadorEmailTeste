FROM mcr.microsoft.com/dotnet/runtime:9.0 AS base
WORKDIR /app

FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src
COPY ["src/DisparadorEmailTeste.Worker/DisparadorEmailTeste.Worker.csproj", "src/DisparadorEmailTeste.Worker/"]
COPY ["src/DisparadorEmailTeste.Dominio/DisparadorEmailTeste.Dominio.csproj", "src/DisparadorEmailTeste.Dominio/"]
COPY ["src/DisparadorEmailTeste.Infraestrutura/DisparadorEmailTeste.Infraestrutura.csproj", "src/DisparadorEmailTeste.Infraestrutura/"]
RUN dotnet restore "src/DisparadorEmailTeste.Worker/DisparadorEmailTeste.Worker.csproj"
COPY . .
RUN dotnet publish "src/DisparadorEmailTeste.Worker/DisparadorEmailTeste.Worker.csproj" -c Release -o /app/publish --no-restore

FROM base AS final
WORKDIR /app
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "DisparadorEmailTeste.Worker.dll"]
