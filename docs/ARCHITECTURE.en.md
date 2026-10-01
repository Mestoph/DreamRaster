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

## Updating

`GitHubUpdater` checks `releases/latest`, downloads the release archive into `runtime\updates`, extracts the new EXE, then starts a local script that waits for the application to exit before replacing the executable.


Image-to-image mode uses `flux2_img_to_img_api.json`, temporarily copies the source image into ComfyUI's `input` folder, applies the `denoise` value, then removes the temporary file after generation.
