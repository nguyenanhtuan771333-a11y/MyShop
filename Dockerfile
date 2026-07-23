FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY . .
RUN dotnet restore MyShop.csproj
RUN dotnet build MyShop.csproj -c Release --no-restore
RUN dotnet publish MyShop.csproj -c Release -o /app --no-build

FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
WORKDIR /app
COPY --from=build /app .
EXPOSE 80
ENTRYPOINT ["dotnet", "MyShop.dll"]