# Consulte https://aka.ms/customizecontainer para aprender a personalizar su contenedor de depuración y cómo Visual Studio usa este Dockerfile para compilar sus imágenes para una depuración más rápida.

# Esta fase se usa cuando se ejecuta desde VS en modo rápido (valor predeterminado para la configuración de depuración)
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS base
WORKDIR /app
EXPOSE 8080
EXPOSE 8081

# Instal·la nmap com a root
USER root
RUN apt-get update && apt-get install -y nmap && rm -rf /var/lib/apt/lists/*

RUN apt-get update && apt-get install -y wget unzip
RUN wget https://github.com/projectdiscovery/nuclei/releases/download/v3.1.0/nuclei_3.1.0_linux_amd64.zip
RUN unzip nuclei_3.1.0_linux_amd64.zip
RUN mv nuclei /usr/local/bin/
RUN rm nuclei_3.1.0_linux_amd64.zip
RUN chmod +x /usr/local/bin/nuclei

# Descarreguem les plantilles de vulnerabilitats (això es fa al build time o al run time)
RUN nuclei -update-templates

# Esta fase se usa para compilar el proyecto de servicio
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src
COPY ["Worker/Worker.csproj", "Worker/"]
RUN dotnet restore "./Worker/Worker.csproj"
COPY . .
WORKDIR "/src/Worker"
RUN dotnet build "./Worker.csproj" -c $BUILD_CONFIGURATION -o /app/build

# Esta fase se usa para publicar el proyecto de servicio que se copiará en la fase final.
FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "./Worker.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

# Esta fase se usa en producción o cuando se ejecuta desde VS en modo normal (valor predeterminado cuando no se usa la configuración de depuración)
FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "Worker.dll"]