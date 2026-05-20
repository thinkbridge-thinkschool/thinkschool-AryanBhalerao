# Auth & Authorization — curl tests

Server: `http://localhost:5051`  
Seed credentials: `test@example.com` / `Password123!`  
Development `ExpiresInMinutes: 1` → tokens expire after 60 s.

---

## 1. POST without a token → 401

```bash
curl -v -X POST http://localhost:5051/api/quotes \
  -H "Content-Type: application/json" \
  -d '{"author":"Test","text":"Test quote"}'
```

**Output:**
```
Note: Unnecessary use of -X or --request, POST is already inferred.
* Host localhost:5051 was resolved.
* IPv6: ::1
* IPv4: 127.0.0.1
  % Total    % Received % Xferd  Average Speed  Time    Time    Time   Current
                                 Dload  Upload  Total   Spent   Left   Speed
  0      0   0      0   0      0      0      0                              0*   Trying [::1]:5051...
* Established connection to localhost (::1 port 64331) from ::1 port 64331
* using HTTP/1.x
> POST /api/quotes HTTP/1.1
> Host: localhost:5051
> User-Agent: curl/8.18.0
> Accept: */*
> Content-Type: application/json
> Content-Length: 37
>
} [37 bytes data]
* upload completely sent off: 37 bytes
< HTTP/1.1 401 Unauthorized
< Content-Length: 0
< Date: Wed, 20 May 2026 11:25:41 GMT
< Server: Kestrel
< WWW-Authenticate: Bearer
<
100     37   0      0 100     37      0   3466                              0100     37   0      0 100     37      0   3388                              0100     37   0      0 100     37      0   3313                              0
* Connection #0 to host localhost:5051 left intact
```

---

## Prerequisite: login to obtain a token

```bash
curl -s -X POST http://localhost:5051/api/auth/login \
  -H "Content-Type: application/json" \
  -d '{"email":"test@example.com","password":"Password123!"}'
```

**Output:**
```json
{"access_token":"eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJzdWIiOiIxIiwiZW1haWwiOiJ0ZXN0QGV4YW1wbGUuY29tIiwibmJmIjoxNzc5Mjc2MzU1LCJleHAiOjE3NzkyNzY0MTUsImlhdCI6MTc3OTI3NjM1NSwiaXNzIjoiUXVvdGVzQXBpIiwiYXVkIjoiUXVvdGVzQXBpVXNlcnMifQ.HqLSU6LO3AZs75ROj2tn4xiXAAzz1WAs6xwRQZsNYWQ","refresh_token":"TEM22q4EucYh4hnJj9CrHP4CTCz4Snn59BSam4pIZjM=","expires_in":60}
```

---

## 2. With a valid token → 201

```bash
TOKEN=eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJzdWIiOiIxIiwiZW1haWwiOiJ0ZXN0QGV4YW1wbGUuY29tIiwibmJmIjoxNzc5Mjc2MzU1LCJleHAiOjE3NzkyNzY0MTUsImlhdCI6MTc3OTI3NjM1NSwiaXNzIjoiUXVvdGVzQXBpIiwiYXVkIjoiUXVvdGVzQXBpVXNlcnMifQ.HqLSU6LO3AZs75ROj2tn4xiXAAzz1WAs6xwRQZsNYWQ

curl -v -X POST http://localhost:5051/api/quotes \
  -H "Content-Type: application/json" \
  -H "Authorization: Bearer $TOKEN" \
  -d '{"author":"Feynman","text":"The first principle is that you must not fool yourself."}'
```

**Output:**
```
Note: Unnecessary use of -X or --request, POST is already inferred.
* Host localhost:5051 was resolved.
* IPv6: ::1
* IPv4: 127.0.0.1
  % Total    % Received % Xferd  Average Speed  Time    Time    Time   Current
                                 Dload  Upload  Total   Spent   Left   Speed
  0      0   0      0   0      0      0      0                              0*   Trying [::1]:5051...
* Established connection to localhost (::1 port 64337) from ::1 port 64337
* using HTTP/1.x
> POST /api/quotes HTTP/1.1
> Host: localhost:5051
> User-Agent: curl/8.18.0
> Accept: */*
> Content-Type: application/json
> Authorization: Bearer eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJzdWIiOiIxIiwiZW1haWwiOiJ0ZXN0QGV4YW1wbGUuY29tIiwibmJmIjoxNzc5Mjc2MzU1LCJleHAiOjE3NzkyNzY0MTUsImlhdCI6MTc3OTI3NjM1NSwiaXNzIjoiUXVvdGVzQXBpIiwiYXVkIjoiUXVvdGVzQXBpVXNlcnMifQ.HqLSU6LO3AZs75ROj2tn4xiXAAzz1WAs6xwRQZsNYWQ
> Content-Length: 85
>
} [85 bytes data]
* upload completely sent off: 85 bytes
< HTTP/1.1 201 Created
< Content-Type: application/json; charset=utf-8
< Date: Wed, 20 May 2026 11:25:55 GMT
< Server: Kestrel
< Location: /api/quotes/3
< Transfer-Encoding: chunked
<
{ [164 bytes data]
100    238   0    153 100     85   9155   5086                              0100    238   0    153 100     85   9016   5009                              0100    238   0    153 100     85   8887   4937                              0
* Connection #0 to host localhost:5051 left intact
{"id":3,"author":"Feynman","text":"The first principle is that you must not fool yourself.","isDeleted":false,"createdAt":"2026-05-20T11:25:55.7198247Z"}
```

---

## 3. With an expired token → 401 + WWW-Authenticate

The token from the login above expires after 60 s (`ExpiresInMinutes: 1`). Wait 65 s then reuse it:

```bash
sleep 65

curl -v -X POST http://localhost:5051/api/quotes \
  -H "Content-Type: application/json" \
  -H "Authorization: Bearer $TOKEN" \
  -d '{"author":"Test","text":"Should fail"}'
```

**Output:**
```
Note: Unnecessary use of -X or --request, POST is already inferred.
* Host localhost:5051 was resolved.
* IPv6: ::1
* IPv4: 127.0.0.1
  % Total    % Received % Xferd  Average Speed  Time    Time    Time   Current
                                 Dload  Upload  Total   Spent   Left   Speed
  0      0   0      0   0      0      0      0                              0*   Trying [::1]:5051...
* Established connection to localhost (::1 port 49434) from ::1 port 49434
* using HTTP/1.x
> POST /api/quotes HTTP/1.1
> Host: localhost:5051
> User-Agent: curl/8.18.0
> Accept: */*
> Content-Type: application/json
> Authorization: Bearer eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJzdWIiOiIxIiwiZW1haWwiOiJ0ZXN0QGV4YW1wbGUuY29tIiwibmJmIjoxNzc5Mjc2MzU1LCJleHAiOjE3NzkyNzY0MTUsImlhdCI6MTc3OTI3NjM1NSwiaXNzIjoiUXVvdGVzQXBpIiwiYXVkIjoiUXVvdGVzQXBpVXNlcnMifQ.HqLSU6LO3AZs75ROj2tn4xiXAAzz1WAs6xwRQZsNYWQ
> Content-Length: 38
>
} [38 bytes data]
* upload completely sent off: 38 bytes
< HTTP/1.1 401 Unauthorized
< Content-Length: 0
< Date: Wed, 20 May 2026 11:27:08 GMT
< Server: Kestrel
< WWW-Authenticate: Bearer error="invalid_token", error_description="The token expired at '05/20/2026 11:26:55'"
<
100     38   0      0 100     38      0   2922                              0100     38   0      0 100     38      0   2867                              0100     38   0      0 100     38      0   2815                              0
* Connection #0 to host localhost:5051 left intact
```
