# Translate 0.4.1

**English** | [简体中文](README.zh-CN.md)

**Project attribution: GPT-6 ASTRA** · [GitHub](https://github.com/2dwa/MI_OCR) · [Download](https://github.com/2dwa/MI_OCR/releases/latest) · [Apache-2.0](LICENSE)

A Windows desktop OCR and translation app built with .NET 10 and WPF. It uses a light, rounded, iOS-inspired interface and runs Xiaomi OCR locally. Translation connects to the provider you configure.

## Download and start

| Package | Contents | Download |
| --- | --- | --- |
| Translate 0.4.1 software, about 72 MB | Desktop app and .NET 10 | [Software ZIP](https://github.com/2dwa/MI_OCR/releases/download/v0.4.1/Translate-0.4.1-win-x64-portable.zip) · [Software SHA256](https://github.com/2dwa/MI_OCR/releases/download/v0.4.1/SHA256SUMS.txt) |
| Xiaomi OCR model package, about 2.05 GB | Complete BF16 model, projector and Ollama with CPU/CUDA 13 | [Model ZIP](https://github.com/2dwa/MI_OCR/releases/download/v0.3.3/MiOCRDesk-0.3.3-win-x64-models.zip) · [Model SHA256](https://github.com/2dwa/MI_OCR/releases/download/v0.3.3/SHA256SUMS-OFFLINE.txt) |

1. Extract the software ZIP into a writable folder and run **Translate.exe**. For text translation, configure a provider, API key and model in **API 设置** (API settings).
2. For screenshot OCR, extract the separate model ZIP into the same folder. `Translate.exe` and `OcrRuntime` should be side by side. An existing model directory can also be selected under **本地模型** (Local model).
3. Exit the old application through its tray before upgrading. Translate migrates saved settings and encrypted provider profiles on first launch. Keep your `OcrRuntime` directory when updating the software.

The model download is the separately published 0.3.3 asset, compatible with Translate 0.4.1. Verify each ZIP with its matching checksum file. The [offline guide](docs/OFFLINE.md) covers extraction and model reuse. `Setup-OCR.cmd` remains an optional online setup helper.

Windows 10 2004+ / Windows 11 x64 is required. .NET is included. For local OCR, at least 8 GB RAM and 6 GB free disk space for both downloads and extracted files are recommended. The model package includes CPU variants and CUDA 13. Other optional CUDA 12/Vulkan backends are omitted. Compatible NVIDIA GPUs/drivers can use CUDA 13 and other systems can use CPU. Translation requires access to your configured provider. The interface uses Chinese labels. Release binaries are unsigned.

## Translation workspace

| Action | Default shortcut |
| --- | --- |
| Capture and recognize | `Ctrl+Alt+O` |
| Capture, recognize and translate | `Ctrl+Alt+T` |
| Translate selected text | `Ctrl+Alt+Y` |
| Open the quick-input translation popup | `Ctrl+Alt+E` |
| Translate typed text | `Ctrl+Enter` |

Global shortcuts are configurable in Software settings.

The **Translation** page appears above Capture in the sidebar. Its original/translated text cards, mode selector, copy/export/translate toolbar and progress feedback share the screenshot results component, with side-by-side comparison selected by default. Type or paste text there, or press **Ctrl+Alt+E** for a compact input popup. Input translation defaults to English. **Ctrl+Enter** translates and **Esc** closes the popup and cancels its request.

Select copyable text in a PDF reader or another app, then press **Ctrl+Alt+Y** and release the keys to translate the selection in a popup. The popup appears immediately with reading progress while the original app retains keyboard focus. A background STA thread uses the application's copy command, waits for fresh clipboard contents and attempts to restore the previous clipboard. Closing or cancelling the popup cancels the pending read. If copying is blocked, no text is sent. Paste text into the input popup or use screenshot OCR for scanned pages.

**Software settings → Target languages by entry** saves four independent targets for screenshot translation, selected-text translation, the main Translation page and the quick-input popup. Each surface displays its own target and uses it for prompt substitution. Older settings migrate selection from the previous screenshot target and the main page from the previous quick-input target. Software settings also contains all four shortcuts and startup/background behavior. The section order is Startup and background, Quick translation, Capture and recognition, then Target languages. Quick-translation shortcut rows use the same left-hand descriptions and fixed-width right-hand controls as the capture rows. **API settings** contains provider profiles, models, reasoning and the shared system prompt. Every translation entry point uses that same saved prompt and substitutes `{targetLanguage}`. Enter your own terminology or variable-naming constraints there. API settings and software settings are saved independently.

On first launch, Translate migrates settings and DPAPI-protected provider profiles from `%APPDATA%/Mi OCR Desk` to `%APPDATA%/Translate`. Exit the old application through its tray before launching Translate. An existing enabled login-startup entry is updated to the new executable path.

## Features

- Global, configurable shortcuts for capture-only and capture-and-translate. Shortcut conflicts are reported and previous registrations are restored.
- A floating OCR window near the selected screen region, with progress, cancellation, editable text, optional pinning and another-capture action. **Esc closes the OCR popup and cancels its current task**; background shortcuts remain available.
- Original text, translation and side-by-side comparison. The bottom toolbar is ordered: original / translation / comparison → copy → export → translate. Comparison exports include both languages. There is no translated-text overlay on the image.
- Import, drag-and-drop and clipboard images; PNG, JPEG, BMP and TIFF are supported (first TIFF page). You can also type or paste text in any language without an image.
- Output language selection, including custom language names, lives in Software settings → Target languages by entry. Changing source text or output language clears an outdated translation. OCR language coverage depends on the local model.
- OpenAI, DeepSeek and other Chat Completions-compatible endpoints. Models are selected from the provider's `/models` response; reasoning options follow known or advertised model capabilities. A listed model still needs actual endpoint/account access.
- Independent provider profiles retain API keys, endpoints, models, reasoning and timeouts when switching. Keys are encrypted with Windows DPAPI for the current user.
- Embedded preferences, optional auto-copy, tray background operation, optional login startup and silent startup. Startup registration is off by default. Existing enabled registrations migrate when upgrading.
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

No model weights, upstream demo checkout, Ollama binaries, API keys, user settings or local development caches are committed to this repository. The separate model Release asset includes pinned, unmodified model blobs and selected upstream runtime backends, together with their notices. CUDA and Microsoft runtime libraries retain their own terms; see [runtime license sources](licenses/runtime/SOURCES.md). Keep all license and notice files when redistributing either package.

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

Outputs are in `dist/releases`. A `v*` tag triggers CI tests followed by a Windows x64 self-contained ZIP and a GitHub Release with SHA256 checksums. An `offline-v*` tag runs the supplemental offline workflow: verify the existing portable release, download and verify pinned components, create separate app/model ZIPs, extract both and run a real CPU OCR check, then upload the two files independently to the same release. The optional Inno Setup script in `installer/` needs Inno Setup 6+.

To package existing local model files without downloading them again:

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File tools/build-offline-release.ps1 -PortableZip dist/releases/Translate-0.4.1-win-x64-portable.zip -PortableChecksums dist/releases/SHA256SUMS.txt -RuntimeRoot OcrRuntime
```

Use a new output directory if either output ZIP already exists. The builder validates all model hashes, isolates software from models, retains licenses in each package, and writes archive checksums and separate per-file inventories. `RuntimeRoot` must contain `runtime/` and `models/`. `tools/test-offline-bundle.ps1 -BundleDirectory <folder-with-both-extracted> -ForceCpu` tests the extracted runtime without opening or automating the desktop app.

```text
Translate/          WPF application
Translate.Tests/    Tests without desktop automation
.vscode/           Win App build/debug configuration
.github/workflows/ CI and tagged releases
tools/             Build, model setup and pinned manifests
installer/         Optional offline installer definition
licenses/          Upstream licenses and notices
docs/              Release notes
```

## Runtime, settings and testing

The local OCR service listens at `http://127.0.0.1:11435`. Runtime resolution uses the saved directory, `TRANSLATE_RUNTIME_ROOT` (with `MIOCR_RUNTIME_ROOT` as a compatibility fallback), `OcrRuntime` beside the executable, then development-directory discovery. The default model is `longwayxu/xiaomi-ocr-0:bf16`. The model can idle out after about ten minutes; subsequent capture reloads it.

Settings live in `%APPDATA%/Translate/settings.json`. DPAPI-encrypted keys cannot simply be moved to another Windows account or machine. `Translate.exe --silent` starts in the tray; `--capture` begins capture. Repeated launches reuse the existing instance.

The default suite currently contains 33 checks and uses simulated translation/OCR responses, does not display or automate desktop windows, does not edit login-startup registration, and does not call paid translation APIs. It covers UI construction, crop geometry, provider persistence, reasoning, all four hotkeys, rollback, Unicode, cancellation, model cleanup and the capture-to-translation pipeline.

Optional real OCR integration uses a generated test image and tests inference → unload → reload → cleanup:

```powershell
dotnet run --project Translate.Tests -- --integration
```

The separately opt-in `--translation-integration` flag uses your saved provider key and sends one short translation request. Do not enable it in CI. UI layout, mixed-DPI capture, actual global shortcuts and login startup should be checked manually. See the [Chinese guide](README.zh-CN.md#验证与手动检查) and [changelog](CHANGELOG.md).
