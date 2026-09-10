//k6 run perf/k6test.js
//k6 run -e MODE=uncached -e VUS=2 -e DURATION=10s perf/k6test.js
//k6 run -e PAGES=20 -e PAGE_SIZE=100 perf/k6test.js
//k6 run -e ACCEPT_ENCODING=none perf/k6test.js

import http from 'k6/http';
import { check } from 'k6';

let BASE = __ENV.BASE || 'http://localhost:7071';
let ENDPOINT = __ENV.ENDPOINT || 'api/properties/active';

let TOKEN = __ENV.TOKEN || 'leeBHB+PURPYQ4mFc6pl8bKlYbAt+OK5otWZbDEeAuQ=';

let MODE = __ENV.MODE || 'cached';
let VUS = Number(__ENV.VUS || 20);
let DURATION = __ENV.DURATION || '30s';

let PAGE = Number(__ENV.PAGE || 1);
let PAGE_SIZE = Number(__ENV.PAGE_SIZE || 100);
let PAGES = Number(__ENV.PAGES || 1);

let P95 = Number(__ENV.P95 || (MODE === 'cached' ? 50 : 3000));
let P99 = Number(__ENV.P99 || P95 * 3);
let ACCEPT_ENCODING = __ENV.ACCEPT_ENCODING || 'gzip, br';

let params = { headers: { Authorization: `Bearer ${TOKEN}` } };

if (ACCEPT_ENCODING !== 'none') {
  params.headers['Accept-Encoding'] = ACCEPT_ENCODING;
}

export const options = {
  scenarios: {
    load: { executor: 'constant-vus', vus: VUS, duration: DURATION },
  },
  thresholds: {
    http_req_duration: [`p(95)<${P95}`, `p(99)<${P99}`],
    http_req_failed: ['rate<0.01'],
    checks: ['rate>0.99'],
  },
  summaryTrendStats: ['min', 'med', 'avg', 'p(90)', 'p(95)', 'p(99)', 'max'],
};

export function setup() {
  let res = http.get(urlFor(PAGE), params);

  if (res.status !== 200) {
    throw new Error(`Warm-up call to ${urlFor(PAGE)} returned ${res.status}. Check the host is running and TOKEN is correct.`);
  }
}

export default function () {
  let page = PAGE + ((__VU + __ITER) % PAGES);
  let res = http.get(urlFor(page), params);
  let body = parse(res);
  let rows = body && Array.isArray(body.items) ? body.items : null;

  check(res, {
    'status is 200': (r) => r.status === 200,
    'body is valid json': () => body !== null,
    'rows are returned': () => rows !== null && rows.length > 0,
    'page is not over size': () => rows !== null && rows.length <= PAGE_SIZE,
    'paging is echoed back': () => body !== null && body.page === page && body.pageSize === PAGE_SIZE,
    'hasMore is reported': () => body !== null && typeof body.hasMore === 'boolean',
  });
}

function urlFor(page) {
  return `${BASE}/${ENDPOINT}?page=${page}&pageSize=${PAGE_SIZE}`;
}

function parse(res) {
  try {
    return res.json();
  } catch (e) {
    return null;
  }
}
