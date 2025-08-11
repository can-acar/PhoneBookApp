FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS base
RUN apt-get update \
	&& apt-get install -y --no-install-recommends curl \
	&& rm -rf /var/lib/apt/lists/*
WORKDIR /app
EXPOSE 8080

FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src
COPY ["ContactService.Api/ContactService.Api.csproj", "ContactService.Api/"]
COPY ["ContactService.Infrastructure/ContactService.Infrastructure.csproj", "ContactService.Infrastructure/"]
COPY ["ContactService.Domain/ContactService.Domain.csproj", "ContactService.Domain/"]
COPY ["Shared.CrossCutting/Shared.CrossCutting.csproj", "Shared.CrossCutting/"]
COPY ["ContactService.ApplicationService/ContactService.ApplicationService.csproj", "ContactService.ApplicationService/"]
COPY ["ContactService.ApiContract/ContactService.ApiContract.csproj", "ContactService.ApiContract/"]
COPY ["ContactService.Container/ContactService.Container.csproj", "ContactService.Container/"]
RUN dotnet restore "ContactService.Api/ContactService.Api.csproj"
COPY . .
WORKDIR "/src/ContactService.Api"
RUN dotnet build "./ContactService.Api.csproj" -c $BUILD_CONFIGURATION -o /app/build

FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "./ContactService.Api.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
RUN ls -la /app
ENTRYPOINT ["dotnet", "ContactService.Api.dll"]
