# Changelog / 更新记录

## 0.3.3 — 2026-11-04

- Add separately downloadable software and offline model ZIPs; extract both into the same directory for offline OCR without CMD downloads. Preserve the model directory across app updates.
- 新增软件与模型独立 ZIP，各自压缩、各自上传；两个包解压到同一目录即可离线识别，不提供合并包。
- Prepare the public MI_OCR repository and first GitHub Release; project attribution: GPT-6 ASTRA.
- Add English/Chinese READMEs, Apache-2.0, upstream model provenance and third-party licenses/notices.
- Add pinned, checksum-verified model/runtime setup and reproducible portable-release packaging.
- Add CI tests and tag-triggered GitHub Release publication. Exclude keys, caches, generated binaries and model weights from Git.
- 首次公开发布；整理源码、双语文档、署名、开源许可、模型安装与自动发布流程。

## 0.3.2

- Configurable capture-and-translate shortcut, default Ctrl+Alt+T; capture-only remains Ctrl+Alt+O.
- Esc closes the OCR floating window and cancels the active operation.
- Two-shortcut conflict validation, swapping and rollback; cancellable OCR-to-translation pipeline.

## 0.3.1

- Move output language to embedded preferences; consolidate result modes, copy, export and translate in the bottom toolbar.

## 0.3.0

- Floating capture results, bilingual comparison, tray background operation, silent/login startup and single-instance handling.
- Embedded preferences, arbitrary text input, selectable/custom output language and explicit model shutdown.

## 0.2.2 and earlier

- .NET 10 / WPF redesign, local Xiaomi OCR, OpenAI and DeepSeek profiles, dynamic model selection and reasoning controls.
