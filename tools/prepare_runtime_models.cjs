// Preserve source GLBs and derive only models requiring unsupported quantization.
const fs = require('node:fs');
const path = require('node:path');
const crypto = require('node:crypto');
const {NodeIO} = require('@gltf-transform/core');
const {ALL_EXTENSIONS} = require('@gltf-transform/extensions');
const {dequantize} = require('@gltf-transform/functions');
const root = path.resolve(__dirname, '..');
function files(directory) {
  return fs.readdirSync(directory, {withFileTypes:true}).flatMap(entry => {
    const name = path.join(directory, entry.name);
    return entry.isDirectory() ? files(name) : [name];
  });
}
function jsonGLB(buffer) {
  const length = buffer.readUInt32LE(12);
  return JSON.parse(buffer.subarray(20, 20 + length).toString());
}
async function main() {
  const scenes = files(path.join(root, 'Scenes')).filter(file => file.endsWith('.tscn'));
  const sources = new Set();
  for (const scene of scenes) {
    for (const match of fs.readFileSync(scene, 'utf8').matchAll(/res:\/\/[^"\n]+\.glb/g)) {
      if (!match[0].startsWith('res://Assets/RuntimeModels/')) sources.add(match[0]);
    }
  }
  const manifestPath = path.join(root, 'Assets/RuntimeModels/manifest.json');
  const previous = fs.existsSync(manifestPath) ? JSON.parse(fs.readFileSync(manifestPath, 'utf8')) : [];
  for (const entry of previous) sources.add(entry.source);
  const io = new NodeIO().registerExtensions(ALL_EXTENSIONS);
  const manifest = [];
  for (const source of [...sources].sort()) {
    const absolute = path.join(root, source.slice(6));
    const buffer = fs.readFileSync(absolute);
    if (!(jsonGLB(buffer).extensionsRequired || []).includes('KHR_mesh_quantization')) continue;
    const target = source.replace('res://Assets/', 'res://Assets/RuntimeModels/');
    const output = path.join(root, target.slice(6));
    const document = await io.read(absolute);
    await document.transform(dequantize());
    // Preserve the import scale that the existing scene wrappers were authored for.
    const importPath = absolute + '.import';
    const match = fs.existsSync(importPath) && fs.readFileSync(importPath, 'utf8').match(/nodes\/root_scale=([\d.]+)/);
    const scale = previous.find(entry => entry.source === source)?.importScale ?? (match ? Number(match[1]) : 1);
    if (scale !== 1) {
      const nodes = new Set(document.getRoot().listScenes().flatMap(scene => scene.listChildren()));
      for (const node of nodes) {
        node.setScale(node.getScale().map(value => value * scale));
        node.setTranslation(node.getTranslation().map(value => value * scale));
      }
    }
    fs.mkdirSync(path.dirname(output), {recursive:true});
    const result = Buffer.from(await io.writeBinary(document));
    if ((jsonGLB(result).extensionsRequired || []).includes('KHR_mesh_quantization')) throw Error(target);
    fs.writeFileSync(output, result);
    manifest.push({source, runtime:target, sourceSHA256:crypto.createHash('sha256').update(buffer).digest('hex'), importScale:scale});
    for (const scene of scenes) {
      const text = fs.readFileSync(scene, 'utf8');
      const rewritten = text.split(source).join(target).split('\n').map(line =>
        line.includes(`path="${target}"`) ? line.replace(/ uid="uid:\/\/[^"]+"/, '') : line).join('\n');
      if (rewritten !== text) fs.writeFileSync(scene, rewritten);
    }
    console.log('Compatible runtime model:', target);
  }
  fs.mkdirSync(path.dirname(manifestPath), {recursive:true});
  fs.writeFileSync(manifestPath, JSON.stringify(manifest, null, 2) + '\n');
  console.log(`Prepared ${manifest.length} compatible models; original GLBs preserved.`);
}
main().catch(error => {console.error(error); process.exitCode = 1;});
