# Lyo Portfolio (Next.js standalone) — build from repo root:
#   docker build -f deploy/portfolio/web/Dockerfile -t lyo-portfolio-web .

FROM node:22-bookworm-slim AS deps
WORKDIR /repo

COPY packages/typescript/lyo-api-client/package.json packages/typescript/lyo-api-client/
COPY packages/typescript/lyo-query/package.json packages/typescript/lyo-query/
COPY packages/typescript/lyo-query-react/package.json packages/typescript/lyo-query-react/
COPY packages/typescript/lyo-person-api-client/package.json packages/typescript/lyo-person-api-client/
COPY apps/portfolio/package.json apps/portfolio/package-lock.json* apps/portfolio/

WORKDIR /repo/packages/typescript/lyo-api-client
RUN npm install --ignore-scripts

WORKDIR /repo/packages/typescript/lyo-query
RUN npm install --ignore-scripts

WORKDIR /repo/packages/typescript/lyo-query-react
RUN npm install --ignore-scripts

WORKDIR /repo/packages/typescript/lyo-person-api-client
RUN npm install --ignore-scripts

WORKDIR /repo/apps/portfolio
RUN npm install

FROM node:22-bookworm-slim AS builder
WORKDIR /repo

COPY --from=deps /repo/packages/typescript /repo/packages/typescript
COPY --from=deps /repo/apps/portfolio/node_modules /repo/apps/portfolio/node_modules

COPY packages/typescript /repo/packages/typescript
COPY apps/portfolio /repo/apps/portfolio
COPY docs/benchmarks/data /repo/docs/benchmarks/data
COPY scripts/portfolio /repo/scripts/portfolio
# docs/benchmarks/history is gitignored — sync-benchmarks no-ops when absent.

WORKDIR /repo/packages/typescript/lyo-api-client
RUN npm run build

WORKDIR /repo/packages/typescript/lyo-query
RUN npm run build

WORKDIR /repo/packages/typescript/lyo-query-react
RUN npm run build

WORKDIR /repo/packages/typescript/lyo-person-api-client
RUN npm run build

WORKDIR /repo/apps/portfolio
ENV NEXT_TELEMETRY_DISABLED=1
ENV LYO_API_BASE_URL=http://127.0.0.1:5251
RUN npm run sync-benchmarks \
 && npm run build

FROM node:22-bookworm-slim AS runner
WORKDIR /app

ENV NODE_ENV=production \
    NEXT_TELEMETRY_DISABLED=1 \
    PORT=3100 \
    HOSTNAME=0.0.0.0

RUN addgroup --system --gid 1001 nodejs \
 && adduser --system --uid 1001 nextjs

COPY --from=builder /repo/apps/portfolio/public ./public
COPY --from=builder --chown=nextjs:nodejs /repo/apps/portfolio/.next/standalone ./
COPY --from=builder --chown=nextjs:nodejs /repo/apps/portfolio/.next/static ./apps/portfolio/.next/static

USER nextjs
EXPOSE 3100
CMD ["node", "apps/portfolio/server.js"]
