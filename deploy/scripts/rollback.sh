#!/bin/bash
# Helm rollback helper — called manually or from a pipeline failure hook
set -e

NAMESPACE="${NAMESPACE:-erp-platform}"
RELEASE="${RELEASE:-erp-backend}"
REVISION="${REVISION:-}"  # empty = rollback to previous

echo "=== ERP Platform Rollback ==="
echo "Release: $RELEASE"
echo "Namespace: $NAMESPACE"

if [ -z "$REVISION" ]; then
    echo "Rolling back to previous revision..."
    helm rollback "$RELEASE" -n "$NAMESPACE" --wait --timeout 5m
else
    echo "Rolling back to revision $REVISION..."
    helm rollback "$RELEASE" "$REVISION" -n "$NAMESPACE" --wait --timeout 5m
fi

echo "✓ Rollback complete"
helm history "$RELEASE" -n "$NAMESPACE" --max 5
