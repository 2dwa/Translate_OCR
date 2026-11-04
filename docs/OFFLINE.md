# 完整离线包 / Complete offline package

**Mi OCR Desk 0.3.3 · GPT-6 ASTRA**

## 简体中文

1. 下载 `MiOCRDesk-0.3.3-win-x64-offline.zip`，可使用 `SHA256SUMS-OFFLINE.txt` 核验下载是否完整。
2. 使用 Windows「全部解压缩」解压到可写目录。请完整解压，勿直接在 ZIP 内运行 EXE，勿单独移动 EXE。
3. 双击 **MiOcrDesk.exe**。模型随包提供，首次截图时自动启动本地引擎并加载模型。无需运行 `Setup-OCR.cmd`，无需下载模型或安装 Python、.NET、Ollama。
4. `Ctrl+Alt+O` 截图识别；`Ctrl+Alt+T` 截图并翻译。翻译需要在「偏好设置」填入自己的服务地址及 API Key 并选择模型，且需要联网。

支持 Windows 10 2004+ / Windows 11 x64，建议至少 8 GB 内存。ZIP 约 2.14 GB，解压后约 2.9 GB；保存 ZIP 并解压建议预留 6 GB 空间。首次加载模型会慢一些；纯 CPU 识别通常比 GPU 慢。

包内含完整 BF16 Xiaomi-OCR-0 模型、投影器、Ollama 0.40.14、.NET 10.0.14、全部 CPU 指令集变体和 CUDA 13 加速库。为满足 GitHub 单个 Release 附件小于 2 GiB 的限制，未包含 CUDA 12 与 Vulkan 后端；模型权重未裁剪、量化或修改。支持 CUDA 13 的 NVIDIA 显卡及驱动可使用 GPU，否则使用 CPU。显卡驱动由 Windows/设备厂商提供，不随包分发。

全新电脑自动发现 EXE 旁的 `OcrRuntime`。已有安装若保存了旧模型路径，可在「本地模型」选择本包的 `OcrRuntime`，然后「检测并启动」。更新前从托盘菜单彻底退出旧实例。用户设置与 API Key 不在 ZIP 内；换电脑需重新配置翻译服务。离线 OCR 不需要 API Key。

请保留整个目录及所有许可文件。软件署名 GPT-6 ASTRA；原始 OCR 模型由 Xiaomi Corporation 发布，基于 Alibaba Cloud / Qwen 的 Qwen3.5-0.8B；本包采用 longwayxu 发布的 Ollama BF16 格式。完整来源与许可见根目录 `NOTICE`、`THIRD_PARTY_NOTICES.md`、`licenses/` 及 `OcrRuntime/runtime-sources.json`。CUDA、Microsoft 运行库和其他依赖分别适用各自条款，不能统一视为 Apache-2.0。

## English

1. Download `MiOCRDesk-0.3.3-win-x64-offline.zip`; optionally verify it with `SHA256SUMS-OFFLINE.txt`.
2. Use Windows **Extract All** into a writable folder. Extract the entire archive; do not launch inside the ZIP or move just the executable.
3. Run **MiOcrDesk.exe**. The first capture automatically starts the bundled engine and loads the bundled model. No CMD setup, model download, Python, .NET or system Ollama installation is needed.
4. **Ctrl+Alt+O** captures and recognizes; **Ctrl+Alt+T** also translates. Translation requires your own configured provider API key, model and internet connection.

Windows 10 2004+ / Windows 11 x64; at least 8 GB RAM is recommended. The ZIP is approximately 2.14 GB and extracts to approximately 2.9 GB; allow 6 GB free space to retain both. First model loading takes longer, and CPU inference is generally slower than GPU inference.

Includes the complete BF16 model and projector, Ollama 0.40.14, .NET 10.0.14, all upstream CPU variants and the CUDA 13 backend. Optional CUDA 12 and Vulkan backends are omitted to stay below GitHub's 2 GiB per-asset limit. Model weights are unchanged. Compatible NVIDIA GPUs/drivers can use CUDA 13; other systems use CPU. GPU drivers are not bundled.

A new installation discovers `OcrRuntime` next to the executable automatically. If upgrading with an old saved runtime path, select this package's `OcrRuntime` under Local model and click Detect and start. Exit old instances through the tray before upgrading. No user settings or API keys are bundled; configure translation separately on each computer. Offline OCR needs no API key.

Keep all copyright and license files. GPT-6 ASTRA is the desktop project attribution; Xiaomi Corporation is the original OCR model author, Alibaba Cloud / Qwen supplies the Qwen3.5-0.8B base, and longwayxu publishes the Ollama BF16 package. See `NOTICE`, `THIRD_PARTY_NOTICES.md`, `licenses/` and `OcrRuntime/runtime-sources.json`. CUDA and Microsoft runtime components retain their own terms, separate from this project's Apache-2.0 license.

`OFFLINE-CONTENTS.sha256` records every packaged file except itself. The separate `SHA256SUMS-OFFLINE.txt` verifies the complete ZIP.
