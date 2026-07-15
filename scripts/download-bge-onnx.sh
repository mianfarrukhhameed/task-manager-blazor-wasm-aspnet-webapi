#!/usr/bin/env bash
# Downloads BGE-small-en-v1.5 ONNX weights + vocab for local embeddings.
# Source: Hugging Face Xenova/bge-small-en-v1.5 (ONNX export compatible with ONNX Runtime).
set -euo pipefail

ROOT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
TARGET_DIR="${ROOT_DIR}/models/bge-small-en-v1.5"
BASE_URL="https://huggingface.co/Xenova/bge-small-en-v1.5/resolve/main"

mkdir -p "${TARGET_DIR}"

download() {
  local remote_path="$1"
  local dest="$2"
  if [[ -f "${dest}" ]]; then
    echo "Skipping existing ${dest}"
    return 0
  fi
  echo "Downloading ${remote_path} -> ${dest}"
  curl -fL --retry 3 --retry-delay 2 \
    -o "${dest}" \
    "${BASE_URL}/${remote_path}"
}

# Prefer FP32 model.onnx; fall back to onnx/model.onnx layout used by some exports.
if [[ ! -f "${TARGET_DIR}/model.onnx" ]]; then
  if curl -fI -s -o /dev/null "${BASE_URL}/onnx/model.onnx"; then
    download "onnx/model.onnx" "${TARGET_DIR}/model.onnx"
  else
    download "model.onnx" "${TARGET_DIR}/model.onnx"
  fi
else
  echo "Skipping existing ${TARGET_DIR}/model.onnx"
fi

download "vocab.txt" "${TARGET_DIR}/vocab.txt"

# Optional: tokenizer.json if present (useful for debugging / alternate loaders)
if curl -fI -s -o /dev/null "${BASE_URL}/tokenizer.json"; then
  download "tokenizer.json" "${TARGET_DIR}/tokenizer.json" || true
fi

echo ""
echo "BGE ONNX assets ready under ${TARGET_DIR}"
echo "Enable with Ai:Features:EnableEmbeddings=true and Ai:Embedding:Provider=Onnx"
