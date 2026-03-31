#!/usr/bin/env bash
set -euo pipefail

BASE_URL="${BASE_URL:-http://localhost:5009}"
RESOURCE="$BASE_URL/invoices"

if ! command -v curl >/dev/null 2>&1; then
  echo "curl is required but not installed."
  exit 1
fi

echo
echo "== DotEmilu FullApp smoke test (bash) =="
echo "Base URL: $BASE_URL"
echo
echo "Make sure API is running first:"
echo "dotnet run --project samples/DotEmilu.Samples.FullApp"
echo

TIMESTAMP="$(date +%s)"
NUMBER="INV-SMOKE-$TIMESTAMP"
UPDATED_NUMBER="${NUMBER:0:18}-U"

echo "1) CREATE invoice"
CREATE_STATUS="$(curl -sS -o /tmp/dotemilu_create_body.json -w "%{http_code}" \
  -X POST "$RESOURCE" \
  -H "Content-Type: application/json" \
  -d "{
    \"number\":\"$NUMBER\",
    \"description\":\"Smoke test invoice\",
    \"amount\":123.45,
    \"date\":\"2026-03-23\"
  }")"
echo "   Status: $CREATE_STATUS"

echo "2) LIST invoices"
LIST_JSON="$(curl -sS "$RESOURCE?pageNumber=1&pageSize=50")"
echo "$LIST_JSON"

# Extract first Id/id value from JSON without extra tools.
INVOICE_ID="$(echo "$LIST_JSON" | sed -nE 's/.*"[Ii]d"[[:space:]]*:[[:space:]]*([0-9]+).*/\1/p' | head -n1)"

if [[ -z "${INVOICE_ID:-}" ]]; then
  echo "No invoice id found in list response. Stopping."
  exit 1
fi
echo "   Picked invoice id: $INVOICE_ID"

echo "3) GET by id"
GET_ONE="$(curl -sS "$RESOURCE/$INVOICE_ID")"
echo "$GET_ONE"

echo "4) CONFIRM invoice"
CONFIRM_STATUS="$(curl -sS -o /tmp/dotemilu_confirm_body.json -w "%{http_code}" \
  -X POST "$RESOURCE/$INVOICE_ID/confirm" \
  -H "Content-Type: application/json" \
  -d "{
    \"confirmationNotes\":\"Looks good to me\"
  }")"
cat /tmp/dotemilu_confirm_body.json && echo
echo "   Status: $CONFIRM_STATUS"

echo "4.1) GET confirmed invoice"
GET_CONFIRMED="$(curl -sS "$RESOURCE/$INVOICE_ID")"
echo "$GET_CONFIRMED"

echo "5) UPDATE invoice"
UPDATE_STATUS="$(curl -sS -o /tmp/dotemilu_update_body.json -w "%{http_code}" \
  -X PUT "$RESOURCE/$INVOICE_ID" \
  -H "Content-Type: application/json" \
  -d "{
    \"number\":\"$UPDATED_NUMBER\",
    \"description\":\"Smoke test invoice updated\",
    \"amount\":150.00,
    \"date\":\"2026-03-23\",
    \"isPaid\":true
  }")"
echo "   Status: $UPDATE_STATUS"

echo "6) GET updated invoice"
GET_UPDATED="$(curl -sS "$RESOURCE/$INVOICE_ID")"
echo "$GET_UPDATED"

echo "7) SYNC invoices"
SYNC_STATUS="$(curl -sS -o /tmp/dotemilu_sync_body.json -w "%{http_code}" \
  -X POST "$RESOURCE/sync" \
  -H "Content-Type: application/json" \
  -d "{
    \"invoiceIds\":[$INVOICE_ID]
  }")"
cat /tmp/dotemilu_sync_body.json && echo
echo "   Status: $SYNC_STATUS"

echo "7.1) GET synced invoice"
GET_SYNCED="$(curl -sS "$RESOURCE/$INVOICE_ID")"
echo "$GET_SYNCED"

echo "8) DELETE invoice (soft-delete)"
DELETE_STATUS="$(curl -sS -o /tmp/dotemilu_delete_body.json -w "%{http_code}" \
  -X DELETE "$RESOURCE/$INVOICE_ID")"
echo "   Status: $DELETE_STATUS"

echo "9) GET deleted invoice (should be 404)"
GET_DELETED_STATUS="$(curl -sS -o /tmp/dotemilu_get_deleted_body.json -w "%{http_code}" \
  "$RESOURCE/$INVOICE_ID")"
cat /tmp/dotemilu_get_deleted_body.json && echo
echo "   Status: $GET_DELETED_STATUS"

echo "10) LIST invoices after delete"
AFTER_JSON="$(curl -sS "$RESOURCE?pageNumber=1&pageSize=50")"
echo "$AFTER_JSON"

echo
echo "Smoke test finished."
