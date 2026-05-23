# curl /health

```
curl -sv http://localhost:8080/health
```

```
* Host localhost:8080 was resolved.
* IPv6: ::1
* IPv4: 127.0.0.1
*   Trying [::1]:8080...
* Established connection to localhost (::1 port 55172)
* using HTTP/1.x
> GET /health HTTP/1.1
> Host: localhost:8080
> User-Agent: curl/8.18.0
> Accept: */*
>
* Request completely sent off
< HTTP/1.1 200 OK
< Content-Type: text/plain
< Date: Sat, 23 May 2026 06:02:56 GMT
< Server: Kestrel
< Cache-Control: no-store, no-cache
< Expires: Thu, 01 Jan 1970 00:00:00 GMT
< Pragma: no-cache
< Transfer-Encoding: chunked
<
Healthy
```

Container log line for the request:

```
[06:02:56 INF] f46426d11464a17aa665ff2d9e7d5698 Serilog.AspNetCore.RequestLoggingMiddleware: HTTP GET /health responded 200 in 25.0130 ms
```
