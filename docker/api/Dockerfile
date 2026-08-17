# Build:  docker build -f docker/api/Dockerfile -t harness-api . (chạy từ backend/)
# Multi-stage: build → runtime chỉ chứa kết quả publish (~100MB)

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copy csproj trước để cache restore
COPY ["src/Harness.Api/Harness.Api.csproj", "src/Harness.Api/"]
COPY ["src/Harness.BuildingBlocks.Domain/Harness.BuildingBlocks.Domain.csproj", "src/Harness.BuildingBlocks.Domain/"]
COPY ["src/Harness.BuildingBlocks.Application/Harness.BuildingBlocks.Application.csproj", "src/Harness.BuildingBlocks.Application/"]
COPY ["src/Harness.BuildingBlocks.Infrastructure/Harness.BuildingBlocks.Infrastructure.csproj", "src/Harness.BuildingBlocks.Infrastructure/"]
COPY ["src/Harness.BuildingBlocks.Presentation/Harness.BuildingBlocks.Presentation.csproj", "src/Harness.BuildingBlocks.Presentation/"]
COPY ["src/Harness.Modules.Catalog/Harness.Modules.Catalog.csproj", "src/Harness.Modules.Catalog/"]
COPY ["src/Harness.Modules.Order/Harness.Modules.Order.csproj", "src/Harness.Modules.Order/"]
COPY ["src/Harness.Modules.Inventory/Harness.Modules.Inventory.csproj", "src/Harness.Modules.Inventory/"]
COPY ["src/Harness.Modules.Customer/Harness.Modules.Customer.csproj", "src/Harness.Modules.Customer/"]
COPY ["src/Harness.Modules.Promotion/Harness.Modules.Promotion.csproj", "src/Harness.Modules.Promotion/"]
COPY ["src/Harness.Modules.Payment/Harness.Modules.Payment.csproj", "src/Harness.Modules.Payment/"]
COPY ["src/Harness.Modules.Shipping/Harness.Modules.Shipping.csproj", "src/Harness.Modules.Shipping/"]
COPY ["src/Harness.Modules.Loyalty/Harness.Modules.Loyalty.csproj", "src/Harness.Modules.Loyalty/"]
COPY ["src/Harness.Modules.Review/Harness.Modules.Review.csproj", "src/Harness.Modules.Review/"]
COPY ["src/Harness.Modules.Cms/Harness.Modules.Cms.csproj", "src/Harness.Modules.Cms/"]
COPY ["src/Harness.Modules.Integration/Harness.Modules.Integration.csproj", "src/Harness.Modules.Integration/"]
RUN dotnet restore "src/Harness.Api/Harness.Api.csproj"

# Copy toàn bộ source và build
COPY . .
RUN dotnet publish "src/Harness.Api/Harness.Api.csproj" -c Release -o /app/publish --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app
COPY --from=build /app/publish .
EXPOSE 8080
ENV ASPNETCORE_URLS=http://+:8080
ENTRYPOINT ["dotnet", "Harness.Api.dll"]
