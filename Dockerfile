# Build stage
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

# Copy csproj and restore dependencies
COPY InvoiceManagement.csproj ./
RUN dotnet restore InvoiceManagement.csproj

# Copy everything else and build
COPY . ./
RUN dotnet publish InvoiceManagement.csproj -c Release -o /app/publish

# Runtime stage
FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS runtime
WORKDIR /app

# Install ICU for globalization support
RUN apt-get update && apt-get install -y libicu-dev && rm -rf /var/lib/apt/lists/*

COPY --from=build /app/publish .

# Set environment variables for Google Cloud Run
ENV ASPNETCORE_URLS=http://+:8080
ENV ASPNETCORE_ENVIRONMENT=Production
ENV DOTNET_SYSTEM_GLOBALIZATION_INVARIANT=false

EXPOSE 8080

ENTRYPOINT ["dotnet", "InvoiceManagement.dll"]
