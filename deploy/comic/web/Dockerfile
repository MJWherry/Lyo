# Lyo Comic Next.js standalone — build from repo root:
#   docker build -f deploy/comic/web/Dockerfile -t lyo-comic-web .

FROM node:22-bookworm-slim AS deps
WORKDIR /repo

COPY packages/typescript/lyo-api-client/package.json packages/typescript/lyo-api-client/
COPY packages/typescript/lyo-query/package.json packages/typescript/lyo-query/
COPY packages/typescript/lyo-query-components/package.json packages/typescript/lyo-query-components/
COPY packages/typescript/lyo-comic-api-client/package.json packages/typescript/lyo-comic-api-client/
COPY apps/comic/package.json apps/comic/package-lock.json* apps/comic/

WORKDIR /repo/packages/typescript/lyo-api-client
RUN npm install --ignore-scripts

WORKDIR /repo/packages/typescript/lyo-query
RUN npm install --ignore-scripts

WORKDIR /repo/packages/typescript/lyo-query-components
RUN npm install --ignore-scripts

WORKDIR /repo/packages/typescript/lyo-comic-api-client
RUN npm install --ignore-scripts

WORKDIR /repo/apps/comic
RUN npm install

FROM node:22-bookworm-slim AS builder
WORKDIR /repo

COPY --from=deps /repo/packages/typescript /repo/packages/typescript
COPY --from=deps /repo/apps/comic/node_modules /repo/apps/comic/node_modules

COPY packages/typescript /repo/packages/typescript
COPY apps/comic /repo/apps/comic

WORKDIR /repo/packages/typescript/lyo-api-client
RUN npm run build

WORKDIR /repo/packages/typescript/lyo-query
RUN npm run build

WORKDIR /repo/packages/typescript/lyo-query-components
RUN npm run build

WORKDIR /repo/packages/typescript/lyo-comic-api-client
RUN npm run build

WORKDIR /repo/apps/comic
ENV NEXT_TELEMETRY_DISABLED=1
ENV LYO_COMIC_API_BASE_URL=http://127.0.0.1:5000
ENV AUTH_COOKIE_SECRET=build-placeholder-not-used-at-runtime
RUN npm run build

FROM node:22-bookworm-slim AS runner
WORKDIR /app

ENV NODE_ENV=production \
    NEXT_TELEMETRY_DISABLED=1 \
    PORT=3101 \
    HOSTNAME=0.0.0.0

RUN addgroup --system --gid 1001 nodejs \
 && adduser --system --uid 1001 nextjs

COPY --from=builder /repo/apps/comic/public ./public
COPY --from=builder --chown=nextjs:nodejs /repo/apps/comic/.next/standalone ./
COPY --from=builder --chown=nextjs:nodejs /repo/apps/comic/.next/static ./apps/comic/.next/static

USER nextjs
EXPOSE 3101
CMD ["node", "apps/comic/server.js"]
