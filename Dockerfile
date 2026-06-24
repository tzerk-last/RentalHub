FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS base
WORKDIR /app
EXPOSE 8080

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY ["RentalHub.csproj", "."]
RUN dotnet restore "./RentalHub.csproj"
COPY . .
RUN dotnet build "RentalHub.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "RentalHub.csproj" -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
RUN mkdir -p /app/wwwroot/uploads/properties
RUN mkdir -p /app/wwwroot/kyc
ENTRYPOINT ["dotnet", "RentalHub.dll"]
