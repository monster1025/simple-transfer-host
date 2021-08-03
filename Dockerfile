FROM mcr.microsoft.com/dotnet/sdk:5.0 AS build

ADD ./src /src
WORKDIR /src

# Restore
RUN dotnet restore "Simple Transfer Host.sln"
RUN dotnet build "Simple Transfer Host.sln" -c Release -o /app
RUN dotnet test "Simple Transfer Host.sln" -c Release
RUN dotnet publish "Simple Transfer Host.sln" -c Release -o /app

# Final image
FROM mcr.microsoft.com/dotnet/aspnet:5.0
WORKDIR /app
COPY --from=build /app .
ENTRYPOINT ["dotnet", "Instance.dll"]
