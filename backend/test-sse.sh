#!/bin/bash

# Test script for Page SSE notifications
# Tests create, rename, and delete events

set -e

API_URL="http://localhost:5036/api"
TOKEN="eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJzdWIiOiJlNjgyNjc1Yi1lYmIxLTQ3NjYtOWM5YS0yNTY2NTI2NTQxMjEiLCJlbWFpbCI6Im93bmVyQGV4YW1wbGUuY29tIiwibmFtZSI6Ik93bmVyIFVzZXIiLCJqdGkiOiJiNzg2YTVmYi0xOTY0LTQ5ODMtOWMyNi0yMzk4YmRkYjdjN2QiLCJleHAiOjE3NTkyNzAwMDgsImlzcyI6Ik5vdGlvbkNsb25lIiwiYXVkIjoiTm90aW9uQ2xvbmVVc2VycyJ9.IN8j8rYL8VHuYtRIyRkJgpFH_eD8NT00zsbAdOeotQs"

echo "=== Testing Page SSE Notifications ==="
echo ""

# Step 1: Get organization ID
echo "Step 1: Getting organization ID..."
ORG_RESPONSE=$(curl -s -X GET "$API_URL/Organizations" \
  -H "Authorization: Bearer $TOKEN")

ORG_ID=$(echo "$ORG_RESPONSE" | python3 -c "import sys, json; orgs = json.load(sys.stdin).get('organizations', []); print(orgs[0]['id'] if orgs else '')")

if [ -z "$ORG_ID" ]; then
  echo "ERROR: No organization found. Response: $ORG_RESPONSE"
  exit 1
fi

echo "✓ Organization ID: $ORG_ID"
echo ""

# Step 2: Start SSE listener in background
echo "Step 2: Starting SSE listener..."
SSE_LOG="sse-events.log"
rm -f "$SSE_LOG"

curl -N -X GET "$API_URL/Pages/stream?orgId=$ORG_ID" \
  -H "Authorization: Bearer $TOKEN" \
  -H "Accept: text/event-stream" > "$SSE_LOG" 2>&1 &

SSE_PID=$!
echo "✓ SSE listener started (PID: $SSE_PID)"
sleep 2
echo ""

# Step 3: Create a page
echo "Step 3: Creating test page..."
CREATE_RESPONSE=$(curl -s -X POST "$API_URL/Pages" \
  -H "Authorization: Bearer $TOKEN" \
  -H "Content-Type: application/json" \
  -d "{\"orgId\": \"$ORG_ID\", \"title\": \"SSE Test Page\"}")

PAGE_ID=$(echo "$CREATE_RESPONSE" | python3 -c "import sys, json; print(json.load(sys.stdin).get('id', ''))")

if [ -z "$PAGE_ID" ]; then
  echo "ERROR: Failed to create page. Response: $CREATE_RESPONSE"
  kill $SSE_PID 2>/dev/null || true
  exit 1
fi

echo "✓ Page created with ID: $PAGE_ID"
sleep 1
echo ""

# Step 4: Rename the page
echo "Step 4: Renaming page..."
curl -s -X PATCH "$API_URL/Pages/$PAGE_ID/title" \
  -H "Authorization: Bearer $TOKEN" \
  -H "Content-Type: application/json" \
  -d "{\"title\": \"SSE Test Page (Renamed)\"}" > /dev/null

echo "✓ Page renamed"
sleep 1
echo ""

# Step 5: Delete the page
echo "Step 5: Deleting page..."
curl -s -X DELETE "$API_URL/Pages/$PAGE_ID" \
  -H "Authorization: Bearer $TOKEN" > /dev/null

echo "✓ Page deleted"
sleep 1
echo ""

# Step 6: Stop SSE listener and check events
echo "Step 6: Stopping SSE listener and checking events..."
kill $SSE_PID 2>/dev/null || true
sleep 1

echo ""
echo "=== SSE Events Received ==="
cat "$SSE_LOG"
echo ""
echo "=== Event Analysis ==="

# Check for each event type
PAGE_CREATED=$(grep -c "PageCreated" "$SSE_LOG" || echo "0")
PAGE_RENAMED=$(grep -c "PageRenamed" "$SSE_LOG" || echo "0")
PAGE_DELETED=$(grep -c "PageDeleted" "$SSE_LOG" || echo "0")

echo "PageCreated events: $PAGE_CREATED (expected: 1)"
echo "PageRenamed events: $PAGE_RENAMED (expected: 1)"
echo "PageDeleted events: $PAGE_DELETED (expected: 1)"
echo ""

# Verify results
if [ "$PAGE_CREATED" -eq "1" ] && [ "$PAGE_RENAMED" -eq "1" ] && [ "$PAGE_DELETED" -eq "1" ]; then
  echo "✅ SUCCESS: All SSE events received correctly!"
  exit 0
else
  echo "❌ FAILURE: Not all expected events were received"
  exit 1
fi
