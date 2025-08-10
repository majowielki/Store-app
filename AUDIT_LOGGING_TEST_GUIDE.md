# Testowanie Audit Logging - Quick Guide

## Szybki Test

### 1. Uruchom aplikacjÍ w Docker
```bash
# Build i uruchom wszystkie serwisy
docker-compose up --build

# Sprawdü logi uruchamiania
docker logs store-auditlogservice
docker logs store-identityservice
```

### 2. Test z·kladovÈho audit logging

#### A. Test Registration (utworzy audit log):
```bash
curl -X POST http://localhost:5000/api/auth/register \
  -H "Content-Type: application/json" \
  -d '{
    "email": "testuser@test.com",
    "password": "Test123!",
    "firstName": "Test",
    "lastName": "User"
  }'
```

#### B. Test Login (utworzy audit log):
```bash
curl -X POST http://localhost:5000/api/auth/login \
  -H "Content-Type: application/json" \
  -d '{
    "email": "testuser@test.com",
    "password": "Test123!"
  }'
```

#### C. Test Demo Admin Login:
```bash
curl -X POST http://localhost:5000/api/auth/demo-admin-login \
  -H "Content-Type: application/json" \
  -d '{}'
```

### 3. Sprawdü audit logs

#### A. Zaloguj siÍ jako admin i pobierz token:
```bash
ADMIN_TOKEN=$(curl -s -X POST http://localhost:5000/api/auth/demo-admin-login \
  -H "Content-Type: application/json" \
  -d '{}' | jq -r '.data.accessToken')
```

#### B. Sprawdü audit logs:
```bash
curl -X GET "http://localhost:5000/api/auditlog?page=1&pageSize=10" \
  -H "Authorization: Bearer $ADMIN_TOKEN" \
  | jq '.'
```

### 4. Test Error Handling (utworzy audit log b≥Ídu):
```bash
curl -X POST http://localhost:5000/api/auth/login \
  -H "Content-Type: application/json" \
  -d '{
    "email": "nonexistent@test.com",
    "password": "wrongpassword"
  }'
```

### 5. Sprawdü logi w bazach danych

#### A. Po≥πcz siÍ z PostgreSQL przez PGAdmin:
- URL: http://localhost:8080
- Email: admin@store-app.com
- Password: admin123

#### B. Lub bezpoúrednio przez Docker:
```bash
docker exec -it store-postgres psql -U store_user -d store_audit_db
```

```sql
-- Sprawdü audit logs
SELECT 
    id, 
    action, 
    entity_name, 
    user_email, 
    timestamp, 
    ip_address,
    additional_info
FROM audit_logs 
ORDER BY timestamp DESC 
LIMIT 10;
```

## Oczekiwane Rezultaty

### Audit logs powinny zawieraÊ:
1. **HTTP_REQUEST** - dla kaødego request
2. **HTTP_RESPONSE** - dla kaødego response  
3. **USER_REGISTRATION_SUCCESS** - dla udanej rejestracji
4. **USER_LOGIN_SUCCESS** - dla udanego logowania
5. **USER_LOGIN_FAILED** - dla nieudanego logowania
6. **JWT_TOKEN_GENERATED** - dla wygenerowanych tokenÛw
7. **GLOBAL_EXCEPTION** - dla b≥ÍdÛw

### Struktura audit log:
```json
{
  "id": 1,
  "action": "USER_REGISTRATION_SUCCESS",
  "entityName": "ApplicationUser",
  "entityId": "user-id-guid",
  "userId": "user-id-guid",
  "userEmail": "testuser@test.com",
  "timestamp": "2025-01-19T10:30:00Z",
  "ipAddress": "172.18.0.1",
  "userAgent": "curl/7.68.0",
  "additionalInfo": "{...json with details...}",
  "oldValues": null,
  "newValues": "{...json with user data...}",
  "changes": null
}
```

## Debug Issues

### Problem: Brak audit logs
```bash
# Sprawdü logi serwisÛw
docker logs store-auditlogservice | grep -i error
docker logs store-identityservice | grep -i error

# Sprawdü network connectivity
docker exec store-identityservice ping auditlogservice
```

### Problem: Database connection errors
```bash
# Sprawdü PostgreSQL
docker logs store-postgres | tail -20

# Sprawdü connection string
docker exec store-auditlogservice env | grep ConnectionStrings
```

### Problem: HttpClient timeouts
```bash
# Sprawdü network
docker network ls
docker network inspect store-network

# Test internal connectivity
docker exec store-identityservice curl -v http://auditlogservice:5004/health
```

## Production Considerations

1. **Monitor Performance** - audit logging adds overhead
2. **Log Retention** - setup log rotation/archiving  
3. **Privacy** - ensure no sensitive data in logs
4. **Backup** - audit logs are critical for compliance
5. **Alerting** - monitor for audit log failures

---

**Status Check:** ? Jeúli wszystkie powyøsze testy przechodzπ, audit logging dzia≥a poprawnie!