// k6 load test for the hot read GET /api/quotes/{id}.
// Sustained closed-loop load (VUS workers, TOTAL iterations) — k6 reports
// http_req_duration percentiles (p95/p99) and req/s. setup() zeroes the
// server-side counters and teardown() reads them back so the run also reports
// DB reads / hit-rate / DB-queries-per-second straight from the API.
//
//   k6 run -e BASE_URL=http://localhost:5081 quotes-loadtest.js   # cache OFF (before)
//   k6 run -e BASE_URL=http://localhost:5080 quotes-loadtest.js   # cache ON  (after)
import http from 'k6/http';
import { check } from 'k6';

const BASE  = __ENV.BASE_URL || 'http://localhost:5080';
const ID    = __ENV.QUOTE_ID || '1';
const TOTAL = parseInt(__ENV.TOTAL || '10000');
const VUS   = parseInt(__ENV.VUS   || '100');

export const options = {
  scenarios: {
    sustained: {
      executor: 'shared-iterations',
      vus: VUS,
      iterations: TOTAL,
      maxDuration: '2m',
    },
  },
  summaryTrendStats: ['avg', 'min', 'med', 'p(90)', 'p(95)', 'p(99)', 'max'],
};

export function setup() {
  http.post(`${BASE}/api/quotes/cache-stats/reset`);   // zero counters for a clean run
  return { t0: Date.now() };
}

export default function () {
  const res = http.get(`${BASE}/api/quotes/${ID}`);
  check(res, { 'status is 200': (r) => r.status === 200 });
}

export function teardown(data) {
  const elapsed = (Date.now() - data.t0) / 1000;
  const s = http.get(`${BASE}/api/quotes/cache-stats`).json();
  const dbQps = (s.dbReads / elapsed).toFixed(1);
  console.log(`server-side: requests=${s.requests} dbReads=${s.dbReads} cacheHits=${s.cacheHits} hitRate=${s.hitRatePercent}%`);
  console.log(`-> DB queries/sec (server-side): ${dbQps}`);
}
