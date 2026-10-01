# Is Qwen3-VL required?

No, not for image generation.

FLUX.2 runs inside ComfyUI with its diffusion model, text encoder and VAE. Qwen3-VL is not part of that generation path.

Qwen3-VL is useful when you want to:
- analyze or describe an image;
- give an Ollama/OpenCode assistant vision capabilities;
- build a future workflow where a VLM interprets an image before generation.

Starting with v25, `InstallVisionModel=false` by default, so the portable installer no longer waits for a multi-gigabyte Qwen3-VL download unless you enable it.
