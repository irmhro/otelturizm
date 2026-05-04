import fs from 'node:fs/promises';
import path from 'node:path';

const root = path.resolve(process.env.OTELTURIZM_ROOT || 'D:/otelturizm');
const maxBytes = Number(process.env.OTELTURIZM_MCP_MAX_BYTES || 120_000);
const excludedParts = new Set([
  '.git', '.vs', 'bin', 'obj', 'node_modules', 'vendor', 'paneltematabler',
  'Backups', 'publish', 'tmp', 'uploads', 'logs'
]);
const excludedFiles = [/\.log$/i, /\.binlog$/i, /stdout/i, /stderr/i];

const tools = [
  {
    name: 'find_files',
    description: 'Find project files by name/glob-like substring. Excludes build, vendor, backup, upload and log folders.',
    inputSchema: {
      type: 'object',
      properties: {
        query: { type: 'string' },
        under: { type: 'string', default: '.' },
        limit: { type: 'number', default: 50 }
      },
      required: ['query']
    }
  },
  {
    name: 'search_code',
    description: 'Search text in scoped project files. Use under to keep context narrow.',
    inputSchema: {
      type: 'object',
      properties: {
        query: { type: 'string' },
        under: { type: 'string', default: '.' },
        ext: { type: 'string' },
        limit: { type: 'number', default: 80 }
      },
      required: ['query']
    }
  },
  {
    name: 'read_range',
    description: 'Read a small line range from a project file.',
    inputSchema: {
      type: 'object',
      properties: {
        file: { type: 'string' },
        start: { type: 'number', default: 1 },
        count: { type: 'number', default: 80 }
      },
      required: ['file']
    }
  },
  {
    name: 'read_file',
    description: 'Read a project file with a byte cap. Prefer read_range for large files.',
    inputSchema: {
      type: 'object',
      properties: { file: { type: 'string' } },
      required: ['file']
    }
  },
  {
    name: 'read_method',
    description: 'Read the C#/Razor/JS block around a method or identifier.',
    inputSchema: {
      type: 'object',
      properties: {
        file: { type: 'string' },
        name: { type: 'string' },
        before: { type: 'number', default: 8 },
        after: { type: 'number', default: 80 }
      },
      required: ['file', 'name']
    }
  },
  {
    name: 'project_map',
    description: 'Return a shallow, filtered project map. Does not inspect excluded folders.',
    inputSchema: {
      type: 'object',
      properties: {
        under: { type: 'string', default: '.' },
        depth: { type: 'number', default: 2 },
        limit: { type: 'number', default: 160 }
      }
    }
  }
];

let buffer = Buffer.alloc(0);
process.stdin.on('data', chunk => {
  buffer = Buffer.concat([buffer, chunk]);
  for (;;) {
    const headerEnd = buffer.indexOf('\r\n\r\n');
    if (headerEnd < 0) return;
    const header = buffer.subarray(0, headerEnd).toString('utf8');
    const match = /Content-Length:\s*(\d+)/i.exec(header);
    if (!match) {
      buffer = buffer.subarray(headerEnd + 4);
      continue;
    }
    const len = Number(match[1]);
    const bodyStart = headerEnd + 4;
    if (buffer.length < bodyStart + len) return;
    const body = buffer.subarray(bodyStart, bodyStart + len).toString('utf8');
    buffer = buffer.subarray(bodyStart + len);
    handle(JSON.parse(body)).catch(err => send({
      jsonrpc: '2.0',
      id: safeId(body),
      error: { code: -32000, message: err.message }
    }));
  }
});

function safeId(body) {
  try { return JSON.parse(body).id ?? null; } catch { return null; }
}

async function handle(msg) {
  if (msg.method === 'initialize') {
    return send({
      jsonrpc: '2.0',
      id: msg.id,
      result: {
        protocolVersion: '2024-11-05',
        capabilities: { tools: {} },
        serverInfo: { name: 'otelturizm-context', version: '1.0.0' }
      }
    });
  }
  if (msg.method === 'notifications/initialized') return;
  if (msg.method === 'tools/list') {
    return send({ jsonrpc: '2.0', id: msg.id, result: { tools } });
  }
  if (msg.method === 'tools/call') {
    const result = await callTool(msg.params?.name, msg.params?.arguments || {});
    return send({ jsonrpc: '2.0', id: msg.id, result: textResult(result) });
  }
  if (msg.id !== undefined) {
    send({ jsonrpc: '2.0', id: msg.id, error: { code: -32601, message: `Unknown method: ${msg.method}` } });
  }
}

function send(payload) {
  const body = JSON.stringify(payload);
  process.stdout.write(`Content-Length: ${Buffer.byteLength(body)}\r\n\r\n${body}`);
}

function textResult(text) {
  return { content: [{ type: 'text', text: String(text) }] };
}

async function callTool(name, args) {
  if (name === 'find_files') return findFiles(args);
  if (name === 'search_code') return searchCode(args);
  if (name === 'read_range') return readRange(args);
  if (name === 'read_file') return readFile(args.file);
  if (name === 'read_method') return readMethod(args);
  if (name === 'project_map') return projectMap(args);
  throw new Error(`Unknown tool: ${name}`);
}

function safePath(input = '.') {
  const full = path.resolve(root, input);
  if (full !== root && !full.startsWith(root + path.sep)) throw new Error('Path is outside project root');
  if (isExcluded(full)) throw new Error('Path is excluded by token-saving rules');
  return full;
}

function rel(full) {
  return path.relative(root, full).replaceAll(path.sep, '/');
}

function isExcluded(full) {
  const relative = path.relative(root, full);
  if (relative.startsWith('..')) return true;
  const parts = relative.split(path.sep).filter(Boolean);
  if (parts.some(p => excludedParts.has(p))) return true;
  return excludedFiles.some(re => re.test(path.basename(full)));
}

async function* walk(dir, depth = 20) {
  if (depth < 0 || isExcluded(dir)) return;
  let entries;
  try {
    entries = await fs.readdir(dir, { withFileTypes: true });
  } catch {
    return;
  }
  entries.sort((a, b) => a.name.localeCompare(b.name));
  for (const entry of entries) {
    const full = path.join(dir, entry.name);
    if (isExcluded(full)) continue;
    yield { full, entry };
    if (entry.isDirectory()) yield* walk(full, depth - 1);
  }
}

async function findFiles({ query, under = '.', limit = 50 }) {
  const base = safePath(under);
  const q = query.toLowerCase();
  const hits = [];
  for await (const { full, entry } of walk(base)) {
    if (entry.isFile() && rel(full).toLowerCase().includes(q)) hits.push(rel(full));
    if (hits.length >= limit) break;
  }
  return hits.join('\n') || 'No files found.';
}

async function searchCode({ query, under = '.', ext, limit = 80 }) {
  const base = safePath(under);
  const q = query.toLowerCase();
  const hits = [];
  for await (const { full, entry } of walk(base)) {
    if (!entry.isFile()) continue;
    if (ext && !full.toLowerCase().endsWith(ext.toLowerCase())) continue;
    let stat;
    try { stat = await fs.stat(full); } catch { continue; }
    if (stat.size > maxBytes) continue;
    const lines = (await fs.readFile(full, 'utf8')).split(/\r?\n/);
    lines.forEach((line, i) => {
      if (hits.length < limit && line.toLowerCase().includes(q)) hits.push(`${rel(full)}:${i + 1}: ${line.trim()}`);
    });
    if (hits.length >= limit) break;
  }
  return hits.join('\n') || 'No matches found.';
}

async function readFile(file) {
  const full = safePath(file);
  const stat = await fs.stat(full);
  if (stat.size > maxBytes) {
    return `File is ${stat.size} bytes. Use read_range instead.`;
  }
  return await fs.readFile(full, 'utf8');
}

async function readRange({ file, start = 1, count = 80 }) {
  const full = safePath(file);
  const lines = (await fs.readFile(full, 'utf8')).split(/\r?\n/);
  const from = Math.max(1, start);
  const to = Math.min(lines.length, from + Math.min(count, 200) - 1);
  return lines.slice(from - 1, to).map((line, i) => `${from + i}: ${line}`).join('\n');
}

async function readMethod({ file, name, before = 8, after = 80 }) {
  const full = safePath(file);
  const lines = (await fs.readFile(full, 'utf8')).split(/\r?\n/);
  const needle = name.toLowerCase();
  const idx = lines.findIndex(line => line.toLowerCase().includes(needle));
  if (idx < 0) return 'Identifier not found.';
  const start = Math.max(1, idx + 1 - before);
  return readRange({ file, start, count: before + after });
}

async function projectMap({ under = '.', depth = 2, limit = 160 }) {
  const base = safePath(under);
  const rows = [];
  for await (const { full, entry } of walk(base, depth)) {
    rows.push(`${entry.isDirectory() ? '[D]' : '[F]'} ${rel(full)}`);
    if (rows.length >= limit) break;
  }
  return rows.join('\n') || 'No entries found.';
}
