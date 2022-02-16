FROM mcr.microsoft.com/dotnet/runtime:6.0 AS base
WORKDIR /app

FROM mcr.microsoft.com/dotnet/sdk:6.0 AS build
WORKDIR /src
COPY ["MessageLoggerApp/MessageLoggerApp.csproj", "MessageLoggerApp/"]
RUN dotnet restore "MessageLoggerApp/MessageLoggerApp.csproj"
COPY . .
WORKDIR "/src/MessageLoggerApp"
RUN dotnet build "MessageLoggerApp.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "MessageLoggerApp.csproj" -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "MessageLoggerApp.dll"]
