# Wan 2.1 video validation — Windows

This guide records Wan I2V 14B settings and **actual local tests on October 9, 2026** using an NVIDIA GeForce RTX 4060 Ti 16 GB and 32 GB of system RAM. Other systems may behave differently.

## Image-to-video workflow

1. In the **Video** tab, select `wan2.1_i2v_480p_14B_fp8_e4m3fn.safetensors`.
2. Provide an **existing reference image** (editable path field or Browse) and a prompt.
3. Set dimensions, frame count, FPS, steps, and sampling parameters.
4. Start generation; selecting the Wan I2V 14B model automatically launches ComfyUI with `--lowvram`.
5. DreamRaster generates a latent video, unloads models, restarts ComfyUI, then decodes the latent through VAE in a fresh process and encodes H.264 MP4.
6. Check the preview and output under the portable `videos\` folder. **Cancel** interrupts the ComfyUI job.

I2V generation requires **at least 12 sampling steps**. Twelve-step runs validate the pipeline, not final perceptual quality.

## Suggested presets for this machine

| Purpose | Dimensions | Frames | FPS | Steps | Status |
| --- | --- | ---: | ---: | ---: | --- |
| Quick trial | 832 × 480 | 17 | 16 | 20 | **Validated October 10, 2026** |
| Balanced | 832 × 480 | 33 | 16 | 30 | **Validated October 10, 2026** |
| Quality | 832 × 480 | 33 | 16 | 40 | Suggested, not directly tested |
| Final | 832 × 480 | 33 | 16 | 50 | **End-to-end tested** |

The UI already offers **Fast 20**, **Standard 30**, **Quality 40**, and **Best 50** steps. Set the frame count separately as needed. Reference sampling settings: CFG 6, sampling shift 8, sampler `uni_pc`, scheduler `simple`. A reference image is mandatory for I2V.

## Verified runs — October 9, 2026

All runs used **Wan I2V 14B**, a reference image and 16 FPS. All but the first used `832 × 480`.

| Resolution | Frames | Steps | Result | Output |
| --- | ---: | ---: | --- | --- |
| 256 × 256 | 5 | 12 | I2V generation and full FFmpeg decode succeeded | 5 frames |
| 832 × 480 | 5 | 12 | I2V generation and full FFmpeg decode succeeded | 5 frames |
| 832 × 480 | 9 | 12 | I2V generation and full FFmpeg decode succeeded | 9 frames |
| 832 × 480 | 17 | 12 | I2V generation and full FFmpeg decode succeeded | 17 frames |
| 832 × 480 | 33 | 12 | I2V generation and full FFmpeg decode succeeded | 33 frames |
| **832 × 480** | **33** | **50** | **50/50 steps, MP4 export and full FFmpeg decode succeeded** | **33 frames** |

The full-profile latent job took approximately **24 min 31 sec**. Its H.264 MP4 contains **33 frame hashes with distinct values**, at 16 FPS for **2.0625 sec**. FFmpeg decoded all frames without error. Verified output example: `videos\DreamRaster_WAN_2026-10-09T14-54-06-435Z_00001_.mp4`.

Distinct decoded frame hashes do **not** establish perceptual quality, convincing motion or temporal consistency. Compare prompts and reference images at different sampling steps before deciding which preset gives the best results.

## Post-release checks — DreamRaster v38.1.0 (October 10, 2026)

The official GitHub release was installed in `DreamRaster_FreshTest` by replacing **only** the executable. The ZIP and published SHA-256 were checked. The personal configuration was restored byte-for-byte and the inventory of **5,186 model files** was preserved.

- **I2V smoke test:** 832 × 480, 5 frames, 12 steps, 16 FPS; H.264 MP4 exported, fully decoded with FFmpeg, all five decoded frame hashes distinct.
- **Real cancellation:** 832 × 480, 33-frame, 12-step Wan sampling interrupted mid-generation. ComfyUI confirmed `interrupt=OK` and queued-prompt removal; the queue was empty, no incomplete MP4 appeared, and the Generate button was re-enabled.
- **Fast preset tested:** 832 × 480, 17 frames, 20 steps, 16 FPS, fixed seed 1. Latent run: **311.28 seconds**; H.264 MP4 duration **1.0625 seconds**, **17/17 distinct decoded frames**, full FFmpeg decode succeeded. Output: `DreamRaster_WAN_2026-10-09T22-16-24-259Z_00001_.mp4`.
- **Balanced preset tested:** 832 × 480, 33 frames, 30 steps, 16 FPS, **same prompt, reference image and fixed seed 1**. Latent job: **15 min 12 sec**; H.264 MP4 duration **2.0625 seconds**, **33/33 distinct decoded frame hashes**, full FFmpeg decode succeeded. Output: `DreamRaster_WAN_2026-10-09T22-23-24-864Z_00001_.mp4`. Sampled frames show a recognizable red mug and temporally stable composition.

- **FLUX.2 image generation also tested:** official v38.1.0 executable, Klein 4B FP8, **512 × 512, 4 steps, seed 1001, no LoRA**; PNG of **290,292 bytes**, signature and dimensions verified, clear recognizable red mug. Output: `DreamRaster_FLUX2_2026-10-09T22-41-47-975Z_00001_.png`.

Visually sampled frames from Fast and Balanced retain a recognizable red mug and temporally stable framing. Despite matching prompt, reference image and seed, these tests have **different durations** (17 versus 33 frames), so visual differences cannot be attributed solely to sampling steps. The earlier 50-step video uses a **different subject** and cannot be ranked directly against the mug. For this machine, **Standard / 30 steps and 33 frames** is a suggested daily trade-off, while **Best / 50 steps** remains a proven, more expensive final-render profile. The 40-step profile has not been tested.

## Diagnostics

See the portable installation's `logs\` directory. If video generation fails, inspect the `LOW_VRAM` setting, CUDA errors, sampling-step progress, the restart between latent generation and VAE decoding, and the final MP4 save event.

Optional **manual** validation with FFmpeg and FFprobe, if installed:

```powershell
ffprobe -v error -show_entries stream=codec_name,width,height,avg_frame_rate,nb_frames -show_entries format=duration,size -of json "path\to\video.mp4"
ffmpeg -v error -i "path\to\video.mp4" -f null NUL
```

The application checks the output **container header** before export; decoding the entire file with FFmpeg is a **separate manual check**, not an automatic DreamRaster operation.

## Data safety

Publish into a separate staging directory and deploy only the intended application EXE. Never overwrite `config\settings.json`, model files, `images\`, `videos\`, or `workspace\`. See the [cleanup audit](AUDIT_NETTOYAGE.md).
