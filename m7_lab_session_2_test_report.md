# M7 Lab Session 2 API Test Report

## Environment

Base URL: https://localhost:7190

## Initial endpoint smoke tests

GET /api/v1/courses -> 200 (https://localhost:7190/api/v1/courses)
Headers:
  Connection: close
  Content-Type: application/json; charset=utf-8
  Date: Wed, 22 Jul 2026 08:43:16 GMT
  Server: Kestrel
  Transfer-Encoding: chunked
  X-Correlation-Id: 8be69a42
  api-supported-versions: 1.0, 2.0
  Deprecation: true
  Sunset: Thu, 31 Dec 2026 00:00:00 GMT
  Link: <https://localhost:7190/api/v2/courses>; rel="successor-version"
Body:
{"items":[{"id":3,"code":"MAT-101","title":"Calculus I","maxCapacity":40,"enrollmentCount":0},{"id":2,"code":"CS-201","title":"Data Structures and Algorithms","maxCapacity":25,"enrollmentCount":2},{"id":1,"code":"CS-101","title":"Introduction to Computer Science [TEST]","maxCapacity":30,"enrollmentCount":2}],"totalCount":3,"page":1,"pageSize":20,"totalPages":1,"hasNext":false,"hasPrevious":false}

----

GET /api/v2/courses -> 200 (https://localhost:7190/api/v2/courses)
Headers:
  Connection: close
  Content-Type: application/json; charset=utf-8
  Date: Wed, 22 Jul 2026 08:43:16 GMT
  Server: Kestrel
  Transfer-Encoding: chunked
  X-Correlation-Id: 9ec8a00c
  api-supported-versions: 1.0, 2.0
Body:
{"data":[{"id":3,"code":"MAT-101","title":"Calculus I","maxCapacity":40,"enrollmentCount":0},{"id":2,"code":"CS-201","title":"Data Structures and Algorithms","maxCapacity":25,"enrollmentCount":2},{"id":1,"code":"CS-101","title":"Introduction to Computer Science [TEST]","maxCapacity":30,"enrollmentCount":2}],"meta":{"totalCount":3,"page":1,"pageSize":20,"totalPages":1,"hasNext":false,"hasPrevious":false},"links":{"self":"/api/v2/courses?page=1&pageSize=20","next":null,"prev":null,"enroll":"/api/v2/enrollments"}}

----

GET /api/v2/courses/1 -> 200 (https://localhost:7190/api/v2/courses/1)
Headers:
  Connection: close
  Content-Type: application/json; charset=utf-8
  Date: Wed, 22 Jul 2026 08:43:16 GMT
  Server: Kestrel
  Transfer-Encoding: chunked
  X-Correlation-Id: f4b9511a
  api-supported-versions: 1.0, 2.0
Body:
{"id":1,"code":"CS-101","title":"Introduction to Computer Science [TEST]","maxCapacity":30,"enrollmentCount":2}

----

GET /api/courses -> 200 (https://localhost:7190/api/courses)
Headers:
  Connection: close
  Content-Type: application/json; charset=utf-8
  Date: Wed, 22 Jul 2026 08:43:17 GMT
  Server: Kestrel
  Transfer-Encoding: chunked
  X-Correlation-Id: ba53f056
  api-supported-versions: 1.0, 2.0
Body:
{"items":[{"id":3,"code":"MAT-101","title":"Calculus I","maxCapacity":40,"enrollmentCount":0},{"id":2,"code":"CS-201","title":"Data Structures and Algorithms","maxCapacity":25,"enrollmentCount":2},{"id":1,"code":"CS-101","title":"Introduction to Computer Science [TEST]","maxCapacity":30,"enrollmentCount":2}],"totalCount":3,"page":1,"pageSize":20,"totalPages":1,"hasPrevious":false,"hasNext":false}

----

GET /api/courses/1 -> 200 (https://localhost:7190/api/courses/1)
Headers:
  Connection: close
  Content-Type: application/json; charset=utf-8
  Date: Wed, 22 Jul 2026 08:43:17 GMT
  Server: Kestrel
  Transfer-Encoding: chunked
  X-Correlation-Id: b17a1e1a
  api-supported-versions: 1.0, 2.0
Body:
{"id":1,"code":"CS-101","title":"Introduction to Computer Science [TEST]","maxCapacity":30,"enrollmentCount":2}

----

GET /api/assessments/results -> 401 (https://localhost:7190/api/assessments/results)
ERROR: HTTP Error 401: Unauthorized
Headers:
  Connection: close
  Content-Type: application/problem+json
  Date: Wed, 22 Jul 2026 08:43:18 GMT
  Server: Kestrel
  Transfer-Encoding: chunked
  X-Correlation-Id: d20098d1
Body:
{"type":"https://tools.ietf.org/html/rfc9110#section-15.5.2","title":"Unauthorized","status":401,"traceId":"00-d8781a983d38b0a20c84c9131b3c27e7-5199c688b71cb413-00"}

----

GET /api/enrollments/worker-smoke -> 200 (https://localhost:7190/api/enrollments/worker-smoke)
Headers:
  Connection: close
  Content-Type: application/json; charset=utf-8
  Date: Wed, 22 Jul 2026 08:43:18 GMT
  Server: Kestrel
  Transfer-Encoding: chunked
  X-Correlation-Id: f632042e
Body:
"processed"

----

GET /api/error -> 500 (https://localhost:7190/api/error)
ERROR: HTTP Error 500: Internal Server Error
Headers:
  Connection: close
  Content-Type: application/json; charset=utf-8
  Date: Wed, 22 Jul 2026 08:43:18 GMT
  Server: Kestrel
  Cache-Control: no-cache,no-store
  Expires: -1
  Pragma: no-cache
  Transfer-Encoding: chunked
Body:
{"title":"Server error","status":500,"detail":"An unexpected error occurred. Trace ID: 0HNN7PCDIUQ47:00000001","instance":"/api/error"}

----

GET /scalar/V1 -> 200 (https://localhost:7190/scalar/V1)
Headers:
  Content-Length: 624
  Connection: close
  Content-Type: text/html
  Date: Wed, 22 Jul 2026 08:43:18 GMT
  Server: Kestrel
  X-Correlation-Id: 9223bda2
Body:
<!doctype html>
<html>
<head>
    <title>Scalar API Reference</title>
    <meta charset="utf-8" />
    <meta name="viewport" content="width=device-width, initial-scale=1" />
    
</head>
<body>
    
    <div id="app"></div>
    <script src="scalar.js"></script>
    <script type="module" src="scalar.aspnetcore.js"></script>
    <script type="module">
        import { initialize } from './scalar.aspnetcore.js'
        initialize(
        '%2Fscalar%2FV1',
        false,
        {"favicon":"favicon.svg","_integration":"dotnet","sources":[{"title":"V1","url":"openapi/V1.json"}]},
        '')
    </script>
</body>
</html>

----

GET /scalar/V2 -> 200 (https://localhost:7190/scalar/V2)
Headers:
  Content-Length: 624
  Connection: close
  Content-Type: text/html
  Date: Wed, 22 Jul 2026 08:43:18 GMT
  Server: Kestrel
  X-Correlation-Id: 68050716
Body:
<!doctype html>
<html>
<head>
    <title>Scalar API Reference</title>
    <meta charset="utf-8" />
    <meta name="viewport" content="width=device-width, initial-scale=1" />
    
</head>
<body>
    
    <div id="app"></div>
    <script src="scalar.js"></script>
    <script type="module" src="scalar.aspnetcore.js"></script>
    <script type="module">
        import { initialize } from './scalar.aspnetcore.js'
        initialize(
        '%2Fscalar%2FV2',
        false,
        {"favicon":"favicon.svg","_integration":"dotnet","sources":[{"title":"V2","url":"openapi/V2.json"}]},
        '')
    </script>
</body>
</html>

----

## Rate limit test: anonymous vs paid

### Anonymous (no X-Api-Key)

Request 2: 200 Retry-After=None
Request 3: 200 Retry-After=None
Request 4: 429 Retry-After=10
Request 5: 429 Retry-After=10
Request 6: 429 Retry-After=10
Request 7: 429 Retry-After=10
Request 8: 429 Retry-After=10
Request 9: 429 Retry-After=10
Request 10: 429 Retry-After=10
Request 11: 429 Retry-After=10
Request 12: 429 Retry-After=10
Request 13: 200 Retry-After=None
Request 14: 429 Retry-After=10
Request 15: 429 Retry-After=10
Request 16: 429 Retry-After=10

### Paid tier (X-Api-Key: tms-paid-001)

Request 2: 200
Request 3: 200
Request 4: 200
Request 5: 200
Request 6: 200
Request 7: 200
Request 8: 200
Request 9: 200
Request 10: 200
Request 11: 200
Request 12: 200
Request 13: 200
Request 14: 200
Request 15: 200
Request 16: 200

----

## Transcript concurrency limiter burst

Status counts: {200: 100}

Detailed transcript results:

Request 2: 200
Request 3: 200
Request 4: 200
Request 5: 200
Request 6: 200
Request 7: 200
Request 8: 200
Request 9: 200
Request 10: 200
Request 11: 200
Request 12: 200
Request 13: 200
Request 14: 200
Request 15: 200
Request 16: 200
Request 17: 200
Request 18: 200
Request 19: 200
Request 20: 200
Request 21: 200
Request 22: 200
Request 23: 200
Request 24: 200
Request 25: 200
Request 26: 200
Request 27: 200
Request 28: 200
Request 29: 200
Request 30: 200
Request 31: 200
Request 32: 200
Request 33: 200
Request 34: 200
Request 35: 200
Request 36: 200
Request 37: 200
Request 38: 200
Request 39: 200
Request 40: 200
Request 41: 200
Request 42: 200
Request 43: 200
Request 44: 200
Request 45: 200
Request 46: 200
Request 47: 200
Request 48: 200
Request 49: 200
Request 50: 200
Request 51: 200
Request 52: 200
Request 53: 200
Request 54: 200
Request 55: 200
Request 56: 200
Request 57: 200
Request 58: 200
Request 59: 200
Request 60: 200
Request 61: 200
Request 62: 200
Request 63: 200
Request 64: 200
Request 65: 200
Request 66: 200
Request 67: 200
Request 68: 200
Request 69: 200
Request 70: 200
Request 71: 200
Request 72: 200
Request 73: 200
Request 74: 200
Request 75: 200
Request 76: 200
Request 77: 200
Request 78: 200
Request 79: 200
Request 80: 200
Request 81: 200
Request 82: 200
Request 83: 200
Request 84: 200
Request 85: 200
Request 86: 200
Request 87: 200
Request 88: 200
Request 89: 200
Request 90: 200
Request 91: 200
Request 92: 200
Request 93: 200
Request 94: 200
Request 95: 200
Request 96: 200
Request 97: 200
Request 98: 200
Request 99: 200
Request 100: 200
Request 101: 200

----

## Cache invalidation test for PUT /api/v2/courses/1

Original course response:
GET /api/v2/courses/1 original -> 200 (https://localhost:7190/api/v2/courses/1)
Headers:
  Connection: close
  Content-Type: application/json; charset=utf-8
  Date: Wed, 22 Jul 2026 08:43:20 GMT
  Server: Kestrel
  Transfer-Encoding: chunked
  X-Correlation-Id: 1c9c855b
  api-supported-versions: 1.0, 2.0
Body:
{"id":1,"code":"CS-101","title":"Introduction to Computer Science [TEST]","maxCapacity":30,"enrollmentCount":2}

Update response:
PUT /api/v2/courses/1 update -> 204 (https://localhost:7190/api/v2/courses/1)
Headers:
  Connection: close
  Date: Wed, 22 Jul 2026 08:43:20 GMT
  Server: Kestrel
  X-Correlation-Id: f8929795
  api-supported-versions: 1.0, 2.0

After update response:
GET /api/v2/courses/1 after update -> 200 (https://localhost:7190/api/v2/courses/1)
Headers:
  Connection: close
  Content-Type: application/json; charset=utf-8
  Date: Wed, 22 Jul 2026 08:43:20 GMT
  Server: Kestrel
  Transfer-Encoding: chunked
  X-Correlation-Id: 9e7005ae
  api-supported-versions: 1.0, 2.0
Body:
{"id":1,"code":"CS-101","title":"Introduction to Computer Science [TEST] [TEST]","maxCapacity":30,"enrollmentCount":2}

Restore response:
PUT /api/v2/courses/1 restore -> 204 (https://localhost:7190/api/v2/courses/1)
Headers:
  Connection: close
  Date: Wed, 22 Jul 2026 08:43:20 GMT
  Server: Kestrel
  X-Correlation-Id: 89c20884
  api-supported-versions: 1.0, 2.0

Restored response:
GET /api/v2/courses/1 restored -> 200 (https://localhost:7190/api/v2/courses/1)
Headers:
  Connection: close
  Content-Type: application/json; charset=utf-8
  Date: Wed, 22 Jul 2026 08:43:21 GMT
  Server: Kestrel
  Transfer-Encoding: chunked
  X-Correlation-Id: b458580e
  api-supported-versions: 1.0, 2.0
Body:
{"id":1,"code":"CS-101","title":"Introduction to Computer Science [TEST]","maxCapacity":30,"enrollmentCount":2}

----
