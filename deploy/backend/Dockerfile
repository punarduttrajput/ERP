# Stage 1: Build
FROM mcr.microsoft.com/dotnet/sdk:8.0-alpine AS build
WORKDIR /src

# Copy solution + all csproj files first (better layer caching)
COPY ERP.sln .
COPY Directory.Build.props .

# Copy all csproj files maintaining directory structure
COPY src/ERP.Host/ERP.Host.csproj src/ERP.Host/
COPY src/ERP.Shared/ERP.Shared.csproj src/ERP.Shared/
COPY src/Modules/ERP.Auth/ERP.Auth.csproj src/Modules/ERP.Auth/
COPY src/Modules/ERP.RBAC/ERP.RBAC.csproj src/Modules/ERP.RBAC/
COPY src/Modules/ERP.Tenants/ERP.Tenants.csproj src/Modules/ERP.Tenants/
COPY src/Modules/ERP.Users/ERP.Users.csproj src/Modules/ERP.Users/
COPY src/Modules/ERP.Admissions/ERP.Admissions.csproj src/Modules/ERP.Admissions/
COPY src/Modules/ERP.SIS/ERP.SIS.csproj src/Modules/ERP.SIS/
COPY src/Modules/ERP.Academics/ERP.Academics.csproj src/Modules/ERP.Academics/
COPY src/Modules/ERP.Timetable/ERP.Timetable.csproj src/Modules/ERP.Timetable/
COPY src/Modules/ERP.Attendance/ERP.Attendance.csproj src/Modules/ERP.Attendance/
COPY src/Modules/ERP.Exams/ERP.Exams.csproj src/Modules/ERP.Exams/
COPY src/Modules/ERP.LMS/ERP.LMS.csproj src/Modules/ERP.LMS/
COPY src/Modules/ERP.Fees/ERP.Fees.csproj src/Modules/ERP.Fees/
COPY src/Modules/ERP.Finance/ERP.Finance.csproj src/Modules/ERP.Finance/
COPY src/Modules/ERP.HRMS/ERP.HRMS.csproj src/Modules/ERP.HRMS/
COPY src/Modules/ERP.Library/ERP.Library.csproj src/Modules/ERP.Library/
COPY src/Modules/ERP.Hostel/ERP.Hostel.csproj src/Modules/ERP.Hostel/
COPY src/Modules/ERP.Transport/ERP.Transport.csproj src/Modules/ERP.Transport/
COPY src/Modules/ERP.Placement/ERP.Placement.csproj src/Modules/ERP.Placement/
COPY src/Modules/ERP.Accreditation/ERP.Accreditation.csproj src/Modules/ERP.Accreditation/
COPY src/Modules/ERP.NAAC/ERP.NAAC.csproj src/Modules/ERP.NAAC/
COPY src/Modules/ERP.OBE/ERP.OBE.csproj src/Modules/ERP.OBE/
COPY src/Modules/ERP.NIRF/ERP.NIRF.csproj src/Modules/ERP.NIRF/
COPY src/Modules/ERP.Compliance/ERP.Compliance.csproj src/Modules/ERP.Compliance/
COPY src/Modules/ERP.ABC/ERP.ABC.csproj src/Modules/ERP.ABC/
COPY src/Modules/ERP.Research/ERP.Research.csproj src/Modules/ERP.Research/
COPY src/Modules/ERP.Alumni/ERP.Alumni.csproj src/Modules/ERP.Alumni/
COPY src/Modules/ERP.Chatbot/ERP.Chatbot.csproj src/Modules/ERP.Chatbot/
COPY src/Modules/ERP.Analytics/ERP.Analytics.csproj src/Modules/ERP.Analytics/
COPY src/Modules/ERP.MobileApi/ERP.MobileApi.csproj src/Modules/ERP.MobileApi/
COPY src/Modules/ERP.Reporting/ERP.Reporting.csproj src/Modules/ERP.Reporting/

RUN dotnet restore src/ERP.Host/ERP.Host.csproj

# Copy all source
COPY src/ src/

RUN dotnet publish src/ERP.Host/ERP.Host.csproj \
    --configuration Release \
    --no-restore \
    --output /app/publish \
    -p:UseAppHost=false

# Stage 2: Runtime — minimal image, non-root user
FROM mcr.microsoft.com/dotnet/aspnet:8.0-alpine AS runtime
WORKDIR /app

# Install timezone data + ICU for globalization
RUN apk add --no-cache icu-libs tzdata

ENV DOTNET_SYSTEM_GLOBALIZATION_INVARIANT=false

# Create non-root user (UID 1001 — non-root by convention)
RUN addgroup -g 1001 -S appgroup && \
    adduser -u 1001 -S appuser -G appgroup

# Copy published output
COPY --from=build --chown=appuser:appgroup /app/publish .

# DinkToPdf requires libwkhtmltox — install native lib
RUN apk add --no-cache libstdc++ libx11 libxrender libxext ca-certificates fontconfig

USER appuser

EXPOSE 8080

ENV ASPNETCORE_URLS=http://+:8080
ENV ASPNETCORE_ENVIRONMENT=Production

HEALTHCHECK --interval=30s --timeout=10s --start-period=60s --retries=3 \
    CMD wget -qO- http://localhost:8080/health || exit 1

ENTRYPOINT ["dotnet", "ERP.Host.dll"]
