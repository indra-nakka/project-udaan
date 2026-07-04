# 📋 API Contract Sandbox

**Role:** Source of truth for client↔server JSON payloads. Keep the C# request/response models and the Node/Express routes matched to the schemas here. Update this file *before* changing either side. Status: **draft — backend not yet built** (Phase 2 / [[UDAAN_MASTER_PROJECT_PLAN]]).

## Conventions
- JSON, `camelCase` keys. Timestamps ISO-8601 UTC. IDs are strings.
- Every endpoint versioned under `/api/v1/...`. Auth via bearer token (guest token first; see [[invariants]] — never hand-roll credential storage).

## Matchmaking

### `POST /api/v1/matchmake`
Request:
```json
{ "playerId": "string", "classIndex": 0, "mode": "tdm_3v3", "region": "auto" }
```
Response:
```json
{ "status": "success", "roomId": "arena_01", "serverIp": "0.0.0.0", "port": 7777, "ticket": "string" }
```

### `GET /api/v1/profile/{playerId}`
Response:
```json
{ "playerId": "string", "displayName": "string", "mmr": 1000, "scrapTotal": 0, "unlockedClasses": [0, 1] }
```

> Replace placeholder IPs with config values — never commit real hosts (see [[gotchas]]: no secrets/hosts in payloads or repo).

## TODO
- [ ] Lobby endpoints (create / join-by-code / ready-up)
- [ ] Loadout persistence schema
- [ ] Match-result reporting (for MMR + server-authoritative scrap reconciliation)
