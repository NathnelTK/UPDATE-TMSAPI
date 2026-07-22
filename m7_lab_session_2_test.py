import concurrent.futures
import json
import ssl
import urllib.error
import urllib.request
from urllib.parse import urljoin

BASE_URL = "https://localhost:7190"
CTX = ssl._create_unverified_context()

ENDPOINTS = [
    ("GET /api/v1/courses", "GET", "/api/v1/courses", None, None),
    ("GET /api/v2/courses", "GET", "/api/v2/courses", None, None),
    ("GET /api/v2/courses/1", "GET", "/api/v2/courses/1", None, None),
    ("GET /api/courses", "GET", "/api/courses", None, None),
    ("GET /api/courses/1", "GET", "/api/courses/1", None, None),
    ("GET /api/assessments/results", "GET", "/api/assessments/results", None, None),
    ("GET /api/enrollments/worker-smoke", "GET", "/api/enrollments/worker-smoke", None, None),
    ("GET /api/error", "GET", "/api/error", None, None),
    ("GET /scalar/V1", "GET", "/scalar/V1", None, None),
    ("GET /scalar/V2", "GET", "/scalar/V2", None, None),
]


def request(method, path, headers=None, body=None):
    url = urljoin(BASE_URL, path)
    data = None
    if body is not None:
        data = body.encode("utf-8")
    req = urllib.request.Request(url, data=data, headers=headers or {}, method=method)
    try:
        with urllib.request.urlopen(req, context=CTX, timeout=30) as resp:
            content = resp.read().decode("utf-8", errors="replace")
            return {
                "status": resp.getcode(),
                "headers": dict(resp.getheaders()),
                "body": content,
                "url": url,
            }
    except urllib.error.HTTPError as e:
        try:
            content = e.read().decode("utf-8", errors="replace")
        except Exception:
            content = ""
        return {
            "status": e.code,
            "headers": dict(e.headers.items()),
            "body": content,
            "url": url,
            "error": str(e),
        }
    except Exception as e:
        return {"status": None, "headers": {}, "body": "", "url": url, "error": str(e)}


def run_endpoints():
    results = []
    for name, method, path, headers, body in ENDPOINTS:
        headers = headers or {}
        if body is not None:
            headers.setdefault("Content-Type", "application/json")
        result = request(method, path, headers=headers, body=body)
        result["name"] = name
        results.append(result)
    return results


def run_rate_limit_tests():
    anon = []
    paid = []
    for idx in range(15):
        anon.append(request("GET", "/api/v2/courses"))
    for idx in range(15):
        paid.append(request("GET", "/api/v2/courses", headers={"X-Api-Key": "tms-paid-001"}))
    return anon, paid


def run_transcript_burst():
    def job(_):
        return request(
            "POST",
            "/api/v2/transcripts",
            headers={"Content-Type": "application/json", "X-Api-Key": "tms-paid-001"},
            body="{\"studentId\":1}"
        )

    results = []
    request_count = 100
    with concurrent.futures.ThreadPoolExecutor(max_workers=100) as executor:
        futures = [executor.submit(job, i) for i in range(request_count)]
        for fut in concurrent.futures.as_completed(futures):
            results.append(fut.result())
    return results


def run_cache_invalidation():
    headers = {"Content-Type": "application/json", "X-Api-Key": "tms-paid-001"}
    original = request("GET", "/api/v2/courses/1", headers=headers)
    if original.get("status") != 200:
        return {"error": "Could not read original course", "original": original}

    try:
        payload = json.loads(original["body"])
        original_title = payload.get("title")
        updated_title = original_title + " [TEST]"
    except Exception:
        return {"error": "Cannot parse original course JSON", "body": original["body"]}

    update_body = json.dumps({"title": updated_title})
    update_result = request("PUT", "/api/v2/courses/1", headers=headers, body=update_body)
    after_update = request("GET", "/api/v2/courses/1", headers=headers)

    restore_result = request("PUT", "/api/v2/courses/1", headers=headers, body=json.dumps({"title": original_title}))
    restored = request("GET", "/api/v2/courses/1", headers=headers)
    return {
        "original": original,
        "update": update_result,
        "after_update": after_update,
        "restore": restore_result,
        "restored": restored,
    }


def format_result(res):
    lines = [f"{res['name']} -> {res.get('status')} ({res.get('url')})"]
    if res.get("error"):
        lines.append(f"ERROR: {res['error']}")
    if res.get("headers"):
        lines.append("Headers:")
        for k, v in res["headers"].items():
            lines.append(f"  {k}: {v}")
    body = res.get("body", "")
    if body:
        lines.append("Body:")
        lines.append(body if len(body) < 800 else body[:800] + "...")
    return "\n".join(lines)


def main():
    report = []
    report.append("# M7 Lab Session 2 API Test Report\n")
    report.append("## Environment\n")
    report.append(f"Base URL: {BASE_URL}\n")
    report.append("## Initial endpoint smoke tests\n")
    endpoints = run_endpoints()
    for res in endpoints:
        report.append(format_result(res))
        report.append("\n----\n")

    report.append("## Rate limit test: anonymous vs paid\n")
    anon, paid = run_rate_limit_tests()
    report.append("### Anonymous (no X-Api-Key)\n")
    report.extend([f"Request {i+1}: {r.get('status')} Retry-After={r.get('headers', {}).get('Retry-After')}" for i, r in enumerate(anon, 1)])
    report.append("\n### Paid tier (X-Api-Key: tms-paid-001)\n")
    report.extend([f"Request {i+1}: {r.get('status')}" for i, r in enumerate(paid, 1)])
    report.append("\n----\n")

    report.append("## Transcript concurrency limiter burst\n")
    transcript_results = run_transcript_burst()
    status_counts = {}
    for r in transcript_results:
        status_counts[r.get('status')] = status_counts.get(r.get('status'), 0) + 1
    report.append(f"Status counts: {status_counts}\n")
    report.append("Detailed transcript results:\n")
    report.extend([f"Request {i+1}: {r.get('status')}" for i, r in enumerate(transcript_results, 1)])
    report.append("\n----\n")

    report.append("## Cache invalidation test for PUT /api/v2/courses/1\n")
    cache_results = run_cache_invalidation()
    if "error" in cache_results:
        report.append(cache_results["error"])
    else:
        report.append("Original course response:\n" + format_result({**cache_results["original"], "name": "GET /api/v2/courses/1 original"}))
        report.append("\nUpdate response:\n" + format_result({**cache_results["update"], "name": "PUT /api/v2/courses/1 update"}))
        report.append("\nAfter update response:\n" + format_result({**cache_results["after_update"], "name": "GET /api/v2/courses/1 after update"}))
        report.append("\nRestore response:\n" + format_result({**cache_results["restore"], "name": "PUT /api/v2/courses/1 restore"}))
        report.append("\nRestored response:\n" + format_result({**cache_results["restored"], "name": "GET /api/v2/courses/1 restored"}))
    report.append("\n----\n")

    out = "\n".join(report)
    with open("m7_lab_session_2_test_report.md", "w", encoding="utf-8") as f:
        f.write(out)
    print("Saved report to m7_lab_session_2_test_report.md")


if __name__ == "__main__":
    main()
