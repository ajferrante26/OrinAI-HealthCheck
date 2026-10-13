from fastapi import FastAPI
import httpx

app = FastAPI()

# Replace with the URL of the service whose health you want to verify.
UPSTREAM_URL = "https://orinai.compulabstudio.net/health"
UPSTREAM_TIMEOUT = 3.0


@app.get("/health")
async def health():
    try:
        async with httpx.AsyncClient(timeout=UPSTREAM_TIMEOUT) as client:
            response = await client.get(UPSTREAM_URL)
        ok = response.status_code == 200
    except Exception:
        ok = False

    return {"status": ok}
