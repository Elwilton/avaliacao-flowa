# OrderGenerator.Api (initiator FIX 4.4 + API) — publicação self-contained
# (empacota o runtime ASP.NET no próprio app, sem depender do framework compartilhado)
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
ARG TARGETARCH
ENV DOTNET_SYSTEM_GLOBALIZATION_INVARIANT=1
WORKDIR /src

COPY src/OrderGenerator.Api/OrderGenerator.Api.csproj src/OrderGenerator.Api/
RUN ARCH="$(echo ${TARGETARCH:-arm64} | sed 's/amd64/x64/')" && \
    dotnet restore -r "linux-$ARCH" src/OrderGenerator.Api/OrderGenerator.Api.csproj

COPY src/OrderGenerator.Api/ src/OrderGenerator.Api/
RUN ARCH="$(echo ${TARGETARCH:-arm64} | sed 's/amd64/x64/')" && \
    dotnet publish src/OrderGenerator.Api/OrderGenerator.Api.csproj \
      -c Release -r "linux-$ARCH" --self-contained true --no-restore -o /app

FROM mcr.microsoft.com/dotnet/runtime-deps:10.0 AS runtime
WORKDIR /app
COPY --from=build /app ./
EXPOSE 8080
ENV ASPNETCORE_URLS=http://+:8080
ENV DOTNET_SYSTEM_GLOBALIZATION_INVARIANT=1
ENTRYPOINT ["./OrderGenerator.Api"]
