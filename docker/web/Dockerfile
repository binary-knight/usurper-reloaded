# Usurper Reborn — Web Proxy Container
# Stats API, admin/balance dashboards, WebSocket-to-TCP bridge

FROM node:22-alpine
WORKDIR /opt/usurper/web

COPY web/package.json ./
RUN npm install --omit=dev

# The proxy serves everything in this directory (pages, steam.html, lang/*.json),
# so copy the whole web tree; node_modules is excluded by .dockerignore.
COPY web/ ./
# Docker ships a smaller landing page than usurper-reborn.net; mount your own
# over /opt/usurper/web/index.html to replace it (see DOCS/DOCKER.md).
COPY docker/web/index.html ./

# /api/stats reports the version from /opt/usurper/version.txt (written by the
# production deploy). Derive it from GameConfig at build time so Docker matches.
COPY Scripts/Core/GameConfig.cs /tmp/GameConfig.cs
RUN sed -n 's/.*public const string Version = "\([^"]*\)".*/\1/p' /tmp/GameConfig.cs | head -1 > /opt/usurper/version.txt \
    && rm /tmp/GameConfig.cs && echo "web image version: $(cat /opt/usurper/version.txt)"

EXPOSE 3000

CMD ["node", "ssh-proxy.js"]
