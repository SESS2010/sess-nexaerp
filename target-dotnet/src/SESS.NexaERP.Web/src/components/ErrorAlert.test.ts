import { test } from 'node:test'
import assert from 'node:assert/strict'
import { createServer } from 'node:http'
import { readFileSync } from 'node:fs'
import { createRequire } from 'node:module'
import ts from 'typescript'

// Compile the actual TSX module without a DOM. Only the API/auth dependency is
// isolated; the network failure is produced by a real loopback lost response.
const require = createRequire(import.meta.url)
const source = readFileSync(new URL('./ErrorAlert.tsx', import.meta.url), 'utf8')
const compiled = ts.transpileModule(source, {
  compilerOptions: { module: ts.ModuleKind.CommonJS, jsx: ts.JsxEmit.ReactJSX },
}).outputText
const module = { exports: {} as Record<string, any> }
class ApiError extends Error {}
new Function('require', 'module', 'exports', compiled)(
  (name: string) => name === '../api/client' ? { ApiError } : require(name),
  module, module.exports,
)
const { describeFailure, humanizeField } = module.exports

test('a committed write with a lost response does not claim nothing was saved', async () => {
  let committedWrites = 0
  const server = createServer((request, response) => {
    assert.equal(request.method, 'POST')
    committedWrites++
    response.destroy()
  })
  await new Promise<void>((resolve) => server.listen(0, '127.0.0.1', resolve))
  try {
    const address = server.address()
    assert.ok(address && typeof address !== 'string')
    let failure: unknown
    try { await fetch(`http://127.0.0.1:${address.port}/write`, { method: 'POST' }) }
    catch (error) { failure = error }
    assert.equal(committedWrites, 1)
    assert.ok(failure instanceof TypeError)
    const banner = describeFailure(failure)
    assert.ok(banner)
    assert.match(banner.guidance, /could not be confirmed/)
    assert.match(banner.guidance, /Check the record before retrying/)
    assert.doesNotMatch(banner.guidance, /Nothing was saved/i)
  } finally {
    server.closeAllConnections()
    await new Promise<void>((resolve, reject) => server.close(error => error ? reject(error) : resolve()))
  }
})

test('friendly field labels retain line numbers and ordinary text fallback', () => {
  assert.equal(humanizeField('Lines[1].RequiredDate'), 'Line 2 \u00b7 Required date')
  assert.equal(describeFailure('ordinary message'), null)
})