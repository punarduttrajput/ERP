#!/bin/bash
# Smoke test — called immediately after staging deployment
# STAGING_URL must be set as environment variable
set -e

BASE_URL="${STAGING_URL:-https://api-staging.erpplatform.com}"
TENANT_SLUG="${STAGING_TENANT_SLUG:-system}"
MAX_RETRIES=10
RETRY_INTERVAL=15

echo "=== ERP Platform Smoke Test ==="
echo "Target: $BASE_URL"
echo "Tenant: $TENANT_SLUG"

# Wait for deployment to be ready
echo "Waiting for API to be ready..."
for i in $(seq 1 $MAX_RETRIES); do
    STATUS=$(curl -s -o /dev/null -w "%{http_code}" "$BASE_URL/health" --max-time 10 || echo "000")
    if [ "$STATUS" = "200" ]; then
        echo "✓ Health check passed (attempt $i)"
        break
    fi
    if [ $i -eq $MAX_RETRIES ]; then
        echo "✗ Health check failed after $MAX_RETRIES attempts (last status: $STATUS)"
        exit 1
    fi
    echo "  Attempt $i/$MAX_RETRIES — status $STATUS, retrying in ${RETRY_INTERVAL}s..."
    sleep $RETRY_INTERVAL
done

# Test 1: Swagger docs load
echo "Testing Swagger endpoint..."
SWAGGER=$(curl -s -o /dev/null -w "%{http_code}" "$BASE_URL/swagger/v1/swagger.json" --max-time 15)
if [ "$SWAGGER" = "200" ]; then
    echo "✓ Swagger OK"
else
    echo "✗ Swagger returned $SWAGGER"
    exit 1
fi

# Test 2: Login returns 401 with wrong creds (not 500)
echo "Testing auth endpoint..."
AUTH=$(curl -s -o /dev/null -w "%{http_code}" \
    -X POST "$BASE_URL/api/auth/login" \
    -H "Content-Type: application/json" \
    -H "X-Tenant-Slug: $TENANT_SLUG" \
    -d '{"email":"smoke@test.com","password":"wrong"}' \
    --max-time 15)
if [ "$AUTH" = "401" ]; then
    echo "✓ Auth endpoint OK (401 for bad creds)"
elif [ "$AUTH" = "200" ]; then
    echo "✗ Auth accepted wrong credentials — security failure"
    exit 1
else
    echo "✗ Auth returned unexpected status $AUTH (expected 401)"
    exit 1
fi

# Test 3: Anonymous endpoints accessible
echo "Testing public endpoint..."
HEALTH_BODY=$(curl -s "$BASE_URL/health" --max-time 10)
if echo "$HEALTH_BODY" | grep -q '"status":"healthy"'; then
    echo "✓ Health body correct"
else
    echo "✗ Health body unexpected: $HEALTH_BODY"
    exit 1
fi

echo ""
echo "=== Smoke tests PASSED ==="
