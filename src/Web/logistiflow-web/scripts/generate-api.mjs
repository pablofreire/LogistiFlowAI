// Generates typed API contracts from the OpenAPI documents the services publish (docs/api).
// Run after changing a service contract: npm run generate:api
import { execFileSync } from 'node:child_process';
import { dirname, resolve } from 'node:path';
import { fileURLToPath } from 'node:url';

const root = dirname(dirname(fileURLToPath(import.meta.url)));
const docs = resolve(root, '../../../docs/api');
const cli = resolve(root, 'node_modules/openapi-typescript/bin/cli.js');

for (const service of ['gateway', 'orders', 'inventory', 'fulfillment', 'shipping']) {
  execFileSync(process.execPath, [cli, resolve(docs, `${service}.openapi.json`), '-o', resolve(root, `src/api/generated/${service}.ts`)], {
    stdio: 'inherit',
  });
}
