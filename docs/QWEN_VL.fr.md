# Qwen3-VL est-il requis ?

Non, pas pour la génération d’images.

FLUX.2 tourne dans ComfyUI avec son modèle de diffusion, son text encoder et son VAE. Qwen3-VL n’intervient pas dans ce chemin.

Qwen3-VL devient utile si vous voulez :
- analyser ou décrire une image ;
- fournir des capacités de vision à un assistant Ollama/OpenCode ;
- exploiter un futur workflow où l’image est comprise par un VLM avant génération.

Depuis la v25, `InstallVisionModel=false` par défaut : l’installation portable n’attend plus plusieurs gigaoctets de Qwen3-VL si vous ne l’utilisez pas.
