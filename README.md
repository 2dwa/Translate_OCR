# Mi OCR Desk 0.3.3

**English** | [简体中文](README.zh-CN.md)

**Project attribution: GPT-6 ASTRA** · [GitHub](https://github.com/2dwa/MI_OCR) · [Download](https://github.com/2dwa/MI_OCR/releases/latest) · [Apache-2.0](LICENSE)

A Windows desktop OCR and translation app built with .NET 10 and WPF. It uses a light, rounded, iOS-inspired interface and runs Xiaomi OCR locally. Translation connects to the provider you configure.

## Quick start

1. For a new computer, download **`MiOCRDesk-0.3.3-win-x64-offline.zip`** from [Releases](https://github.com/2dwa/MI_OCR/releases/latest), verify it against `SHA256SUMS-OFFLINE.txt`, and extract the entire ZIP to a writable folder.
2. Open **MiOcrDesk.exe**. The bundled model is discovered automatically and loads on first capture. No CMD setup or extra download is needed. Allow 6 GB of free disk space for the ZIP and extracted files.
3. If upgrading with an old saved model path, select this package's `OcrRuntime` under **本地模型** (Local model), then **检测并启动** (Detect and start).
4. Press **Ctrl + Alt + O** to capture and recognize, or **Ctrl + Alt + T** to capture, recognize and translate. Configure your translation API first in **偏好设置** (Preferences).

Requires Windows 10 2004+ / Windows 11, x64; at least 8 GB RAM is recommended. The app includes .NET; Python and a system-wide Ollama installation are not required. The complete package works offline for OCR from its first launch; translation needs access to the selected provider. The interface currently uses Chinese labels. Release binaries are not Authenticode-signed.

| Download | Contents / use |
| --- | --- |
| `offline.zip` (~2.14 GB) | Complete BF16 model, Ollama, .NET, CPU and CUDA 13. Extract and run; recommended for a new PC. |
| `portable.zip` (~72 MB) | App and setup helper, without model/runtime. Reuse an existing model or run `Setup-OCR.cmd` once (about 3.4 GB of downloads; allow 8 GB free space). Verify with `SHA256SUMS.txt`. |

To fit GitHub's 2 GiB per-asset limit, the offline ZIP omits optional CUDA 12 and Vulkan backends. It preserves the complete model and all CPU variants. Compatible NVIDIA GPUs/drivers use CUDA 13; other systems use CPU. See the [bilingual offline guide](docs/OFFLINE.md). Exit an old instance from its tray menu before upgrading.

## Features

- Global, configurable shortcuts for capture-only and capture-and-translate. Shortcut conflicts are reported and previous registrations are restored.
- A floating OCR window near the selected screen region, with progress, cancellation, editable text, optional pinning and another-capture action. **Esc closes the OCR popup and cancels its current task**; background shortcuts remain available.
- Original text, translation and side-by-side comparison. The bottom toolbar is ordered: original / translation / comparison → copy → export → translate. Comparison exports include both languages. There is no translated-text overlay on the image.
- Import, drag-and-drop and clipboard images; PNG, JPEG, BMP and TIFF are supported (first TIFF page). You can also type or paste text in any language without an image.
- Output language selection, including custom language names, lives in Preferences → Output options. Changing source text or output language clears an outdated translation. OCR language coverage depends on the local model.
- OpenAI, DeepSeek and other Chat Completions-compatible endpoints. Models are selected from the provider's `/models` response; reasoning options follow known or advertised model capabilities. A listed model still needs actual endpoint/account access.
- Independent provider profiles retain API keys, endpoints, models, reasoning and timeouts when switching. Keys are encrypted with Windows DPAPI for the current user.
- Embedded preferences, optional auto-copy, tray background operation, optional login startup and silent startup. Startup registration is off by default and is only changed when saved by the user.
- A **关闭 OCR 模型** (Close OCR model) button unloads the model. Exiting the app unloads it and stops the daemon started by the app. A pre-existing external daemon is preserved; only this OCR model is unloaded.

Capture-and-translate is one cancellable operation. Cancelled/failed OCR and empty results never proceed to translation. A translation failure preserves recognized text. Images stay local; only text is sent to the configured translation service. The connection test sends `Hello, world.` and may incur provider charges.

## Model provenance and licenses

| Component | Source / attribution | License and scope |
| --- | --- | --- |
| This desktop application | GPT-6 ASTRA · [MI_OCR](https://github.com/2dwa/MI_OCR) | [Apache-2.0](LICENSE) |
| Original Xiaomi-OCR-0 | Xiaomi Corporation · [source](https://github.com/xiaomi-research/Xiaomi-OCR-0) · [model card](https://huggingface.co/xiaomi-research/Xiaomi-OCR-0) | [Apache-2.0](licenses/Xiaomi-OCR-0-LICENSE.txt); former SeerRay-Lab URLs now redirect to xiaomi-research |
| Qwen3.5-0.8B base model | Alibaba Cloud / Qwen · [model card](https://huggingface.co/Qwen/Qwen3.5-0.8B) | [Apache-2.0](licenses/Qwen3.5-0.8B-LICENSE.txt) |
| BF16 package loaded by this app | longwayxu · [Ollama package](https://ollama.com/longwayxu/xiaomi-ocr-0:bf16) | Ollama/GGUF distribution of the above model; package publisher is distinct from original model authors |
| Ollama 0.40.14 | [ollama/ollama](https://github.com/ollama/ollama/tree/v0.40.14) | [MIT](licenses/Ollama-LICENSE.txt); dependencies retain their own terms |
| .NET 10, WPF, Windows Forms | Microsoft / .NET contributors | MIT and included third-party terms; see [third-party notices](THIRD_PARTY_NOTICES.md) |

**GPT-6 ASTRA is the attribution for this desktop project, not the author of the OCR model or third-party runtimes.** This is an independent integration, not an official Xiaomi, Alibaba, Ollama or OpenAI product. Upstream copyright and license notices remain intact. See [NOTICE](NOTICE), [THIRD_PARTY_NOTICES.md](THIRD_PARTY_NOTICES.md), [licenses/](licenses/) and the [pinned source manifest](tools/runtime-sources.json).

No model weights, upstream demo checkout, Ollama binaries, API keys, user settings or local development caches are committed to this repository. The offline Release asset includes pinned, unmodified model blobs and selected upstream runtime backends, together with their notices. CUDA and Microsoft runtime libraries retain their own terms; see [runtime license sources](licenses/runtime/SOURCES.md). Keep all license and notice files when redistributing a populated offline folder.

## Build and develop

Install a .NET 10 SDK, clone the repository and open it in VS Code:

```powershell
git clone git@github.com:2dwa/MI_OCR.git
cd MI_OCR
powershell -NoProfile -ExecutionPolicy Bypass -File tools/win-app.ps1 -Action build
powershell -NoProfile -ExecutionPolicy Bypass -File tools/win-app.ps1 -Action test
powershell -NoProfile -ExecutionPolicy Bypass -File tools/win-app.ps1 -Action run
```

[.vscode/win_app.json](.vscode/win_app.json) is the project configuration consumed by the PowerShell scripts; it is not a dependency on a VS Code extension of that name. Scripts use `.dotnet/dotnet.exe` when present, otherwise the system SDK. `global.json` selects the .NET 10 feature band family. F5 offers system-SDK and local-`.dotnet` configurations.

Build the same portable release locally:

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File tools/build-release.ps1
```

Outputs are in `dist/releases`. A `v*` tag triggers CI tests followed by a Windows x64 self-contained ZIP and a GitHub Release with SHA256 checksums. An `offline-v*` tag runs the supplemental offline workflow: verify the existing portable release, download and verify pinned components, package the offline ZIP, extract it and run a real CPU OCR check, then attach it to the same release. The optional Inno Setup script in `installer/` needs Inno Setup 6+.

To package existing local model files without downloading them again:

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File tools/build-offline-release.ps1 -PortableZip dist/releases/MiOCRDesk-0.3.3-win-x64-portable.zip -PortableChecksums dist/releases/SHA256SUMS.txt -RuntimeRoot OcrRuntime
```

Use a new output directory if the offline ZIP already exists. The builder validates all model hashes, copies only the pinned model and required runtime files, retains licenses, and writes both a ZIP checksum and a per-file inventory. `RuntimeRoot` must contain `runtime/` and `models/`. `tools/test-offline-bundle.ps1 -BundleDirectory <extracted-folder> -ForceCpu` tests the extracted runtime without opening or automating the desktop app.

```text
MiOcrDesk/          WPF application
MiOcrDesk.Tests/    Tests without desktop automation
.vscode/           Win App build/debug configuration
.github/workflows/ CI and tagged releases
tools/             Build, model setup and pinned manifests
installer/         Optional offline installer definition
licenses/          Upstream licenses and notices
docs/              Release notes
```

## Runtime, settings and testing

The local OCR service listens at `http://127.0.0.1:11435`. Runtime resolution uses the saved directory, `MIOCR_RUNTIME_ROOT`, `OcrRuntime` beside the executable, then development-directory discovery. The default model is `longwayxu/xiaomi-ocr-0:bf16`. The model can idle out after about ten minutes; subsequent capture reloads it.

Settings live in `%APPDATA%/Mi OCR Desk/settings.json`. DPAPI-encrypted keys cannot simply be moved to another Windows account or machine. `MiOcrDesk.exe --silent` starts in the tray; `--capture` begins capture. Repeated launches reuse the existing instance.

The default test suite uses simulated translation/OCR responses, does not display or automate desktop windows, does not edit login-startup registration, and does not call paid translation APIs. It covers UI construction, crop geometry, provider persistence, reasoning, both hotkeys, rollback, Unicode, cancellation, model cleanup and the capture-to-translation pipeline.

Optional real OCR integration uses a generated test image and tests inference → unload → reload → cleanup:

```powershell
dotnet run --project MiOcrDesk.Tests -- --integration
```

The separately opt-in `--translation-integration` flag uses your saved provider key and sends one short translation request. Do not enable it in CI. UI layout, mixed-DPI capture, actual global shortcuts and login startup should be checked manually. See the [Chinese guide](README.zh-CN.md#验证与手动检查) and [changelog](CHANGELOG.md).
