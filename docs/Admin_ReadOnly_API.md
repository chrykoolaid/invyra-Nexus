# Admin Read-Only API (v1.7)

Purpose: expose trust + DQS + audit data for admin UI without direct DB access.

Security note (v1):
- This is **read-only** by design.
- Add auth (JWT / API key) before exposing beyond localhost.

Suggested deployment:
- localhost only (127.0.0.1) behind your main app
- or internal network with mTLS later
