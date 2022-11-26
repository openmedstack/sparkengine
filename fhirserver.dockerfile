# Build runtime image
FROM mcr.microsoft.com/dotnet/runtime-deps:7.0-alpine
ENV ACCEPT_EULA=Y
ENV DOTNET_CLI_TELEMETRY_OPTOUT=1
ENV DOTNET_NOLOGO=true
RUN addgroup -S fhirgroup && adduser -S fhiruser -G fhirgroup
USER fhiruser
COPY artifacts/publish/linux-musl-x64/ app/
WORKDIR /app
ENTRYPOINT ["./openmedstack.fhirserver"]