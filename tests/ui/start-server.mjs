import { cp, mkdir, rm } from 'node:fs/promises';
import { randomUUID } from 'node:crypto';
import { tmpdir } from 'node:os';
import { join } from 'node:path';
import { spawn } from 'node:child_process';

const repositoryRoot = process.cwd();
const contentRoot = join(tmpdir(), 'file-workspace-playwright', randomUUID());

await rm(contentRoot, { force: true, recursive: true });
await mkdir(contentRoot, { recursive: true });
await cp(join(repositoryRoot, 'wwwroot'), join(contentRoot, 'wwwroot'), { recursive: true });

const server = spawn(
  'dotnet',
  ['run', '--project', 'FileWorkspace.csproj', '--configuration', 'Release', '--no-launch-profile', '--', '--urls', 'http://127.0.0.1:5090', '--contentRoot', contentRoot],
  { cwd: repositoryRoot, env: process.env, stdio: 'inherit' }
);

let stopping = false;
async function stop(exitCode) {
  if (stopping) return;
  stopping = true;
  if (!server.killed) server.kill();
  await rm(contentRoot, { force: true, recursive: true });
  process.exit(exitCode);
}

server.once('exit', async code => {
  if (!stopping) await stop(code ?? 1);
});
process.on('SIGINT', () => stop(0));
process.on('SIGTERM', () => stop(0));
