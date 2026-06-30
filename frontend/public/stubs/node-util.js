export function promisify(fn) {
  return (...args) =>
    new Promise((resolve, reject) => {
      fn(...args, (err, result) => {
        if (err) reject(err);
        else resolve(result);
      });
    });
}

export const TextEncoder = globalThis.TextEncoder;
export const TextDecoder = globalThis.TextDecoder;
export const types = {
  isFloat32Array: (v) => v instanceof Float32Array,
  isInt32Array: (v) => v instanceof Int32Array,
  isUint8Array: (v) => v instanceof Uint8Array,
  isUint8ClampedArray: (v) => v instanceof Uint8ClampedArray,
};
export default { promisify, TextEncoder, TextDecoder, types };
