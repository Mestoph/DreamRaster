# Architecture — English

## Principle

DreamRaster isolates its components inside the portable root. `PortablePaths` rejects paths outside the pack for application-managed executables and data.

## Local services

- OpenCode: `127.0.0.1:54095`
- Ollama: `127.0.0.1:11434`
- ComfyUI: `127.0.0.1:8188`
- image proxy: `127.0.0.1:54100`
- generation API: `127.0.0.1:54101`

Ports are configurable.

## Generation

`Flux2Generator` loads a ComfyUI API workflow, replaces prompt/model/dimension/step/seed fields, submits it to `/prompt`, polls `/history/{id}`, then copies the resulting image to `images\`.

## Wan I2V video generation

`VideoGenerator` submits ComfyUI workflows through `/prompt`, polls `/history/{id}`, and supports cancellation. A valid reference image is required for I2V.

For the large **Wan I2V 14B** model, DreamRaster starts ComfyUI with `--lowvram` and uses two phases:

1. **Latent generation** using Wan, UMT5 and CLIP-Vision, with temporary latent storage.
2. **Unload models and stop/restart ComfyUI**; load the VAE in a fresh process, decode, then encode H.264 MP4. Intermediate latent files are cleaned up.

Workload-aware timeouts are **20 or 60 minutes** for latent sampling and **8 or 20 minutes** for VAE decode/encode. The I2V output header is checked before export, but this is not a substitute for decoding the entire video.

The **832 × 480, 33 frames, 16 FPS, 50 steps** pipeline was validated locally on an RTX 4060 Ti 16 GB. See [Wan video validation](VIDEO_VALIDATION.en.md).

## Updating

`GitHubUpdater` checks `releases/latest`, downloads the release archive into `runtime\updates`, extracts the new EXE, then starts a local script that waits for the application to exit before replacing the executable.


Image-to-image mode uses `flux2_img_to_img_api.json`, temporarily copies the source image into ComfyUI's `input` folder, applies the `denoise` value, then removes the temporary file after generation.
