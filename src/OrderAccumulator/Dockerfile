# OrderAccumulator (acceptor FIX 4.4) — imagem multi-stage
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
ENV DOTNET_SYSTEM_GLOBALIZATION_INVARIANT=1
WORKDIR /src

COPY src/OrderAccumulator/OrderAccumulator.csproj src/OrderAccumulator/
RUN dotnet restore src/OrderAccumulator/OrderAccumulator.csproj

COPY src/OrderAccumulator/ src/OrderAccumulator/
RUN dotnet publish src/OrderAccumulator/OrderAccumulator.csproj -c Release -o /app

FROM mcr.microsoft.com/dotnet/runtime:10.0 AS runtime
WORKDIR /app
COPY --from=build /app ./
EXPOSE 5001
ENV DOTNET_SYSTEM_GLOBALIZATION_INVARIANT=1
ENTRYPOINT ["dotnet", "OrderAccumulator.dll"]
