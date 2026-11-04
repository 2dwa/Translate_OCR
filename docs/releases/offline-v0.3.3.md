<!-- offline-packages -->
## 软件与离线模型分开下载 / Separate software and offline model downloads

| 下载 / Download | 内容 / Contents |
| --- | --- |
| **`MiOCRDesk-0.3.3-win-x64-app.zip`** (~72 MB) | 软件 + .NET / Desktop app + .NET |
| **`MiOCRDesk-0.3.3-win-x64-models.zip`** (~2.07 GB) | 完整 OCR 模型 + Ollama / Complete OCR model + Ollama |

**两个 ZIP 分别下载、解压到同一目录，确保 `MiOcrDesk.exe` 与 `OcrRuntime` 并排，然后运行 EXE 即可，无需 CMD 下载。没有合并上传包。** 以后仅更新软件可保留模型目录。用 `SHA256SUMS-OFFLINE.txt` 校验两个文件。

模型包保留完整 BF16 权重、CPU 与 CUDA 13；不含 CUDA 12/Vulkan，显卡或驱动不兼容时使用 CPU。翻译需要自行配置 API 并联网。双语说明与原作者署名、第三方许可随各包提供。

**Download both ZIPs separately and extract them into the same directory. `MiOcrDesk.exe` and `OcrRuntime` must be side by side. Run the EXE; no CMD setup or further downloads are needed. No combined archive is uploaded.** Keep the model directory when updating only the app. Verify both files with `SHA256SUMS-OFFLINE.txt`. The model package includes complete BF16 weights, CPU and CUDA 13; optional CUDA 12/Vulkan are omitted. Translation needs your own API configuration and internet. Original licenses and bilingual instructions accompany each package.

The original `portable.zip` below is retained as the initial online-setup distribution. / 下方原始 `portable.zip` 保留，属于最初提供下载助手的版本；新电脑请使用上面两个分开的包。
