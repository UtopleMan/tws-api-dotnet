# Self-built IBKR Client Portal (Web API) Gateway image.
#
# This is the REST/HTTPS gateway (v1/api/...) — a SEPARATE thing from the ib-gateway
# socket API in docker-compose.yml. It replaces the previous third-party ibeam image
# with a plain, self-contained gateway we build ourselves.
#
# There is NO login automation here: the Client Portal Gateway authenticates via a
# browser SSO flow. After `docker compose up -d cpgateway`, open the URL and log in
# (username/password + 2FA) yourself; the session then lasts ~24h.
#
#   open https://localhost:5000            # accept the self-signed cert, then log in
#   curl -ks https://localhost:5000/v1/api/iserver/auth/status | jq   # {"authenticated": true, ...}
FROM debian:bookworm-slim

# Update base packages, then install just what the gateway needs: a JRE to run it,
# unzip to unpack the distribution, and curl (+ CA certs) to download it over HTTPS.
RUN apt-get update && apt-get upgrade -y \
    && apt-get install -y --no-install-recommends \
       openjdk-17-jre-headless \
       unzip \
       curl \
       ca-certificates \
    && rm -rf /var/lib/apt/lists/*

WORKDIR /app

# Download and unzip the Client Portal Gateway distribution.
# IBKR only publishes a rolling "latest" at this URL — rebuild (--no-cache) to update.
RUN mkdir gateway && cd gateway \
    && curl -fsSL -O https://download2.interactivebrokers.com/portal/clientportal.gw.zip \
    && unzip -q clientportal.gw.zip \
    && rm clientportal.gw.zip

# Our config (listens on 5000) overrides the distribution default.
COPY conf.yaml gateway/root/conf.yaml
COPY start.sh /app/start.sh
RUN chmod +x /app/start.sh

# HTTPS REST API (see listenPort in conf.yaml).
EXPOSE 5000

CMD ["/app/start.sh"]
