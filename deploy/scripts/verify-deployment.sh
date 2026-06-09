#!/bin/bash
# Post-production deployment verification
# Called after Helm upgrade completes in production
set -e

PROD_URL="${PROD_URL:-https://api.erpplatform.com}"
MAX_RETRIES=20
RETRY_INTERVAL=30

echo "=== Production Deployment Verification ==="

for i in $(seq 1 $MAX_RETRIES); do
    STATUS=$(curl -s -o /dev/null -w "%{http_code}" "$PROD_URL/health" --max-time 15 || echo "000")
    if [ "$STATUS" = "200" ]; then
        echo "✓ Production health check passed (attempt $i)"
        BODY=$(curl -s "$PROD_URL/health" --max-time 10)
        echo "  Response: $BODY"
        exit 0
    fi
    if [ $i -eq $MAX_RETRIES ]; then
        echo "✗ Production health check FAILED after $MAX_RETRIES attempts"
        echo "  Last status: $STATUS"
        echo "  Triggering rollback..."
        exit 1
    fi
    echo "  Attempt $i/$MAX_RETRIES — status $STATUS, retrying in ${RETRY_INTERVAL}s..."
    sleep $RETRY_INTERVAL
done
