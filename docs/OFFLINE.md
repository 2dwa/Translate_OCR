# 软件包与离线模型包 / Separate app and offline model packages

**Translate 0.4.1 · GPT-6 ASTRA**

## 简体中文

1. 分别下载 **`Translate-0.4.1-win-x64-portable.zip`（软件）** 和 **`MiOCRDesk-0.3.3-win-x64-models.zip`（模型及运行时）**，可使用软件包的 `SHA256SUMS.txt` 与原模型包的 `SHA256SUMS-OFFLINE.txt` 分别核验。
2. 先解压软件包，再将模型包解压到**同一个目录**。最终 `Translate.exe` 与 `OcrRuntime` 文件夹应并排，不能再多套一层目录。模型和软件分别压缩、分别发布，无合并 ZIP。
3. 双击 **Translate.exe**。程序自动发现旁边的模型，首次截图时自动启动本地引擎并加载模型。无需运行 `Setup-OCR.cmd`，无需额外下载或安装 Python、.NET、Ollama。
4. `Ctrl+Alt+O` 截图识别；`Ctrl+Alt+T` 截图并翻译。翻译需要在「API 设置」填入自己的服务地址及 API Key 并选择模型，且需要联网。

支持 Windows 10 2004+ / Windows 11 x64，建议至少 8 GB 内存。软件 ZIP 约 72 MB，模型 ZIP 约 2.05 GB，两者解压后共约 2.9 GB；保存两个 ZIP 并解压建议预留 6 GB 空间。首次加载模型会慢一些；纯 CPU 识别通常比 GPU 慢。

软件包含桌面程序和 .NET 10.0.14，不含 OCR 模型。模型包含完整 BF16 Xiaomi-OCR-0 模型、投影器、Ollama 0.40.14、全部 CPU 指令集变体和 CUDA 13 加速库，不含桌面程序。模型包采用 CPU + CUDA 13 配置，未包含其他可选 CUDA 12/Vulkan 后端；模型权重未裁剪、量化或修改。支持 CUDA 13 的 NVIDIA 显卡及驱动可使用 GPU，否则使用 CPU。显卡驱动由 Windows/设备厂商提供，不随包分发。

```text
Translate/
├─ Translate.exe              ← 软件 ZIP
├─ docs/OFFLINE.md
└─ OcrRuntime/                ← 模型 ZIP
   ├─ runtime/ollama.exe
   ├─ runtime/lib/ollama/
   ├─ models/blobs/
   └─ models/manifests/
```

以后只更新软件时，可以保留原有 `OcrRuntime`，无需重复下载模型包。不要在 ZIP 内直接运行程序。

全新电脑自动发现 EXE 旁的 `OcrRuntime`。已有安装若保存了旧模型路径，可在「本地模型」选择本包的 `OcrRuntime`，然后「检测并启动」。更新前从托盘菜单彻底退出旧实例。用户设置与 API Key 不在 ZIP 内；换电脑需重新配置翻译服务。离线 OCR 不需要 API Key。

请保留整个目录及所有许可文件。软件署名 GPT-6 ASTRA；原始 OCR 模型由 Xiaomi Corporation 发布，基于 Alibaba Cloud / Qwen 的 Qwen3.5-0.8B；本包采用 longwayxu 发布的 Ollama BF16 格式。完整来源与许可随两个 ZIP 分别提供，见各自的 `NOTICE`、`THIRD_PARTY_NOTICES.md`、`licenses/` 及 `OcrRuntime/runtime-sources.json`。CUDA、Microsoft 运行库和其他依赖分别适用各自条款，不能统一视为 Apache-2.0。

## English

1. Download **`Translate-0.4.1-win-x64-portable.zip`** (software) and **`MiOCRDesk-0.3.3-win-x64-models.zip`** (model/runtime) separately. Verify the software with its `SHA256SUMS.txt` and the existing model with its original `SHA256SUMS-OFFLINE.txt`.
2. Extract the software ZIP, then extract the model ZIP into **the same directory**. `Translate.exe` and the `OcrRuntime` folder must be side by side, without an extra enclosing folder. The packages are published separately; no combined ZIP is provided.
3. Run **Translate.exe**. The first capture automatically starts the adjacent engine and loads the model. No CMD setup, additional download, Python, .NET or system Ollama installation is needed.
4. **Ctrl+Alt+O** captures and recognizes; **Ctrl+Alt+T** also translates. Translation requires your own configured provider API key, model and internet connection.

Windows 10 2004+ / Windows 11 x64; at least 8 GB RAM is recommended. The software ZIP is approximately 72 MB, the model ZIP approximately 2.05 GB, and the extracted files total approximately 2.9 GB. Allow 6 GB free space to retain the downloads and extracted files. First model loading takes longer, and CPU inference is generally slower than GPU inference.

The software ZIP contains the desktop app and .NET 10.0.14, with no model. The model ZIP contains the complete BF16 model/projector, Ollama 0.40.14, all upstream CPU variants and CUDA 13, with no desktop app. Other optional CUDA 12/Vulkan backends are omitted. Model weights are unchanged. Compatible NVIDIA GPUs/drivers can use CUDA 13; other systems use CPU. GPU drivers are not bundled. Keep `OcrRuntime` when updating only the app to avoid downloading the model again.

A new installation discovers `OcrRuntime` next to the executable automatically. If upgrading with an old saved runtime path, select this package's `OcrRuntime` under Local model and click Detect and start. Exit old instances through the tray before upgrading. No user settings or API keys are bundled; configure translation separately on each computer. Offline OCR needs no API key.

Keep all copyright and license files. GPT-6 ASTRA is the desktop project attribution; Xiaomi Corporation is the original OCR model author, Alibaba Cloud / Qwen supplies the Qwen3.5-0.8B base, and longwayxu publishes the Ollama BF16 package. See `NOTICE`, `THIRD_PARTY_NOTICES.md`, `licenses/` and `OcrRuntime/runtime-sources.json`. CUDA and Microsoft runtime components retain their own terms, separate from this project's Apache-2.0 license.

The older model ZIP and its checksum remain valid. Translate 0.4.1 can reuse that model directory. Software and models are distributed separately.
