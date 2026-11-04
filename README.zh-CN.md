# Mi OCR Desk 0.3.3

[English](README.md) | **简体中文**

**项目署名：GPT-6 ASTRA** · [GitHub](https://github.com/2dwa/MI_OCR) · [下载 Release](https://github.com/2dwa/MI_OCR/releases/latest) · [Apache-2.0](LICENSE)

Windows 本地截图识别与翻译工作台。界面采用浅色、圆角卡片、系统蓝、分段切换和分组设置的类 iOS 风格，使用 .NET 10 + WPF 原生桌面技术。

## 使用

从 [Releases](https://github.com/2dwa/MI_OCR/releases/latest) 分别下载 **`MiOCRDesk-0.3.3-win-x64-app.zip`（软件）** 和 **`MiOCRDesk-0.3.3-win-x64-models.zip`（模型）**，用 `SHA256SUMS-OFFLINE.txt` 核验。将两个 ZIP 解压到**同一个可写目录**，确保 `MiOcrDesk.exe` 与 `OcrRuntime` 文件夹并排，再双击 EXE。首次截图时自动启动引擎并加载模型，无需运行 CMD 下载，也无需安装 Python、.NET 或 Ollama。

| 下载包 | 内容与适用情况 |
| --- | --- |
| `app.zip`（约 72 MB） | 桌面程序、.NET 与双语说明，不含 OCR 模型。 |
| `models.zip`（约 2.07 GB） | 完整 BF16 模型、Ollama、CPU 与 CUDA 13，不含桌面程序。 |
| `portable.zip`（约 72 MB） | 小体积程序与下载助手，不含模型和 Ollama；适合已有模型的电脑，或先运行 `Setup-OCR.cmd` 在线安装（约下载 3.4 GB，预留 8 GB 空间）。校验文件为 `SHA256SUMS.txt`。 |

**软件与模型分开压缩、分开上传，不提供合并包。** 以后只更新软件时保留 `OcrRuntime` 即可，无需重复下载模型。系统要求 Windows 10 2004+ / Windows 11 x64，建议至少 8 GB 内存。两个 ZIP 加解压目录建议预留 6 GB 空间；不要直接在 ZIP 中运行。模型包采用 CPU + CUDA 13 配置，保留全部 CPU 变体，未包含其他可选 CUDA 12/Vulkan 后端；权重完整且未经修改。不支持 CUDA 13 的显卡或驱动使用 CPU，速度可能较慢。OCR 首次使用即可离线，翻译仍需自行配置 API 并联网。详见[分包解压说明](docs/OFFLINE.md)。

已有安装若保存了旧模型路径，请在「本地模型」选择本包的 `OcrRuntime` 后「检测并启动」。用户设置和 API Key 不包含在分发包中。

源码开发目录可双击 `Start-MiOCR.cmd`。检查新版前请从托盘彻底退出旧版，以免快捷键被占用。发布程序暂未进行 Authenticode 签名。

## 模型出处与许可证

| 组件 | 来源 / 署名 | 许可证与说明 |
| --- | --- | --- |
| 本桌面项目 | GPT-6 ASTRA · [MI_OCR](https://github.com/2dwa/MI_OCR) | [Apache-2.0](LICENSE) |
| Xiaomi-OCR-0 原始模型 | Xiaomi Corporation · [GitHub](https://github.com/xiaomi-research/Xiaomi-OCR-0) · [Hugging Face](https://huggingface.co/xiaomi-research/Xiaomi-OCR-0) | [Apache-2.0](licenses/Xiaomi-OCR-0-LICENSE.txt)；旧地址 SeerRay-Lab 已重定向至 xiaomi-research |
| Qwen3.5-0.8B 底座 | Alibaba Cloud / Qwen · [模型页](https://huggingface.co/Qwen/Qwen3.5-0.8B) | [Apache-2.0](licenses/Qwen3.5-0.8B-LICENSE.txt) |
| 实际加载的 BF16 包 | longwayxu · [Ollama 模型页](https://ollama.com/longwayxu/xiaomi-ocr-0:bf16) | 上述模型的 Ollama / GGUF 分发格式；不将打包者标为原模型作者 |
| Ollama 0.40.14 | [ollama/ollama](https://github.com/ollama/ollama/tree/v0.40.14) | [MIT](licenses/Ollama-LICENSE.txt)，其依赖保留各自许可 |
| .NET 10 / WPF / Windows Forms | Microsoft / .NET contributors | MIT 及所附第三方许可，见 [第三方声明](THIRD_PARTY_NOTICES.md) |

项目署名仅适用于桌面程序，不替代模型、运行时与依赖的原作者署名。本项目是独立集成应用，不是小米、阿里、Ollama 或 OpenAI 官方产品。完整许可、第三方声明及模型来源清单随源码和两个独立发布包一并提供，见 [NOTICE](NOTICE)、[第三方声明](THIRD_PARTY_NOTICES.md)、[固定来源与摘要](tools/runtime-sources.json)。模型权重、Ollama 二进制、API Key、用户设置和本机缓存不提交到 Git 仓库。离线模型 ZIP 附带模型及运行库；CUDA 与 Microsoft 运行库分别适用各自许可，见[运行库许可来源](licenses/runtime/SOURCES.md)，不统一归为 Apache-2.0。

## 功能

- 点击「框选截图」，或按 `Ctrl+Alt+O`；拖拽选区，松开开始识别。`Esc` / 右键取消。快捷键和托盘截图直接显示独立悬浮结果窗，不会打开主界面；取消截图恢复此前的窗口状态。
- 按 `Ctrl+Alt+T` 可「截图并翻译」：框选后自动识别，再用当前供应商、模型、推理强度及输出语言翻译，在悬浮窗显示双语对照。两个快捷键均可在「偏好设置 → 截图与识别」修改并保存，不能重复；被其他软件占用时会提示并保留原快捷键。
- 自动翻译只在识别成功且有文字时执行；取消截图、取消识别或识别失败均不会发起翻译请求。翻译失败保留识别文字。未配置翻译服务时会先引导到设置页。
- OCR 悬浮窗靠近截图区域显示，可拖动、缩放、取消置顶、再次截图或打开主界面；按 `Esc` 或关闭按钮可直接关闭悬浮窗，取消正在进行的任务，后台快捷键继续可用。
- 截图前冻结屏幕，原文 / 译文模式保留图片预览；底部显示当前阶段、耗时及取消按钮。模型没有真实百分比接口，因此使用不定进度条。
- 主窗口和悬浮窗底部统一排列「原文 / 译文 / 双语对照 → 复制 → 导出 → 翻译文本」。翻译时自动进入并排对照；对照模式的复制、导出包含原文和译文，切到「译文」可只复制译文。不提供原图覆盖功能。
- 可直接在原文框输入或粘贴任意语言文字，无需先截图。输出语言统一在「偏好设置 → 输出选项」中选择或输入，保存后同时用于主窗口和悬浮窗，并在下次启动时保留。修改原文或输出语言会清空旧译文，避免混淆。图片 OCR 的语言识别能力仍取决于本地模型。
- 也支持导入图片、拖入一张图片、粘贴剪贴板图片、重新识别。支持 PNG、JPEG、BMP、TIFF（多页 TIFF 读取第一页）。
- 识别结果可以编辑、复制、导出 TXT / Markdown；偏好设置支持识别后自动复制。
- 翻译前在「偏好设置」填写服务提供商、HTTPS Base URL 和 API Key，点击「刷新模型」后从下拉列表选择模型，无需手写模型名。选择目标语言并调整系统提示词，`{targetLanguage}` 会替换成所选语言。
- 偏好设置与「截图识别」「本地模型」一样，在主界面内切换。提供商可选 OpenAI、DeepSeek 或其他兼容服务。每家分别保存 API Key、地址、模型、推理强度和超时；切换时恢复自己的配置，不会清空或复用另一家的 Key。修改完成后点击「保存设置」即可跨重启保留；「放弃修改」恢复上次保存值。
- 模型下方可选择推理强度。OpenAI 根据已知模型能力显示对应档位；不支持调节的模型（如 GPT-4.1）只有「模型默认」。DeepSeek 可关闭思考，或选择低 / 高 / 最高，优先采用模型接口返回的支持档位。强度越高，通常耗时和用量也越高。
- 设置页首次加载时会使用已保存的密钥自动调用 `/models` 获取列表；更换地址或密钥后清空旧列表。获取失败时可重试，也可先保存连接信息，稍后再选择模型。列表获取不发送翻译内容。
- OpenAI 列表会隐藏明显不适合文本翻译的语音、图像等模型，兼容服务保留其返回的模型 ID。列表接口不提供完整的端点兼容性信息，因此列出某模型不代表翻译权限、计费额度或 Chat Completions 支持已经验证，请使用「测试翻译连接」确认。
- 「测试翻译连接」会发送固定测试文字 `Hello, world.` 给所选服务，可能产生少量服务商计费。仅点击测试或翻译时会访问翻译接口。

## 启动与后台

- 默认关闭主窗口后进入托盘后台，快捷键仍可截图。双击托盘图标恢复主界面；右键可截图、打开设置或「退出」。重复运行程序会唤起已有实例。
- 在「偏好设置 → 启动与后台」设置开机自启动、静默启动、关闭主窗口后后台运行，并点击保存。开机启动默认关闭，仅在用户开启并保存后写入当前用户的登录启动项，无需管理员权限。静默启动不打开主窗口，OCR 在首次使用时加载。
- 支持 `MiOcrDesk.exe --silent` 仅启动后台，或 `MiOcrDesk.exe --capture` 直接截图；若已经运行，则复用原实例。
- 「本地模型 → 关闭 OCR 模型」会卸载小米模型，释放模型占用的内存 / 显存，并停止本软件启动的识别服务。下次截图自动启动和加载。若复用的是外部已运行服务，只卸载小米模型，保留外部服务及其他模型。
- 托盘「退出」或关闭主窗口且未开启后台运行时，先取消当前任务，再自动清理 OCR 模型及本软件启动的服务。进入托盘后台和彻底退出是两种不同操作。

## VS Code 开发

配置入口为 [.vscode/win_app.json](.vscode/win_app.json)。这是本项目的任务配置文件，由 PowerShell 构建脚本读取，不依赖同名 VS Code 扩展。

需要 .NET 10 SDK。脚本优先使用项目内 `.dotnet/dotnet.exe`，否则使用 PATH 中的 SDK。`global.json` 固定在 .NET 10，允许使用更新的稳定功能版本。

`MiOcrDesk.slnx` 使用 XML 解决方案格式，包含桌面项目和自动测试项目，便于 C# Dev Kit 发现与加载。

VS Code 任务：

- `Win App: Run`：构建并运行，自动连接此目录中的本地模型。
- `Win App: Build`：编译。
- `Win App: Test`：执行不展示窗口的自动测试。
- `Win App: Publish bundle`：生成自包含离线目录。
- `Win App: Build installer`：生成离线安装程序，需要 Inno Setup 6+ 的 `ISCC.exe` 在 PATH 中，或位于 `.tools/InnoSetup`。

也可运行：

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File tools/win-app.ps1 -Action run
powershell -NoProfile -ExecutionPolicy Bypass -File tools/win-app.ps1 -Action test
```

F5 调试配置在 `.vscode/launch.json`：安装系统 SDK 时选 `system .NET SDK`，只在项目内解压 SDK 时选 `local .dotnet SDK`。修改目标框架时需同步该文件的可执行文件路径。

## 本地模型与新电脑安装

桌面端直接调用 `http://127.0.0.1:11435/api/chat`，使用 `longwayxu/xiaomi-ocr-0:bf16`。不依赖 Python、上游网页演示或额外的全局 Ollama 安装。旧演示部署仅留在本机，不纳入本仓库。

离线目录结构：

```text
MiOcrDesk.exe
OcrRuntime/
  runtime/ollama.exe
  runtime/lib/...
  models/manifests/...
  models/blobs/...
```

需要 Windows 10 2004+ / Windows 11 x64。GitHub Release 提供自包含便携程序和联网模型安装脚本，不把模型权重装进 ZIP。运行 `Setup-OCR.cmd` 后，在「本地模型」点击「检测并启动」，首次识别会加载模型。安装完成的整个目录可复制到另一台电脑离线使用；也可通过发布脚本生成包含已有模型的完整离线包。

`tools/build-release.ps1` 生成便携 ZIP 和 SHA256 校验文件，输出在 `dist/releases`。可选 Inno Setup 安装程序由开发者另行构建，输出在 `dist/installer`；如果产生 `.bin` 分卷，复制时需与 Setup.exe 放在同一目录。完整离线目录可用 `tools/publish-bundle.ps1` 构建。

模型路径优先级：用户选择的目录 → `MIOCR_RUNTIME_ROOT` → EXE 旁 `OcrRuntime` → 开发目录自动发现。可直接选择已有的、包含 `runtime` 与 `models` 的离线目录；不会重复下载或复制大型模型。已运行的本地服务会继续复用，新目录在下次引擎启动时生效。

## 仓库结构与发布

```text
MiOcrDesk/             WPF 应用源码
MiOcrDesk.Tests/       无桌面自动化的测试
.vscode/               Win App 构建与调试配置
.github/workflows/     CI 与标签触发的 Release
tools/                 构建、模型安装、固定模型清单
installer/             可选离线安装器脚本
licenses/              第三方许可原文
docs/                  发布说明
```

推送 `v*` 标签后，GitHub Actions 会先运行测试，再构建 Windows x64 自包含便携包，将 ZIP 和 SHA256 上传到 GitHub Release。打包与协议说明见 [第三方声明](THIRD_PARTY_NOTICES.md)，版本变化见 [CHANGELOG](CHANGELOG.md)。

后台运行期间模型闲置约 10 分钟后释放，也可手动关闭模型。彻底退出软件会主动卸载模型。首次加载耗时取决于电脑硬件。识别取消会中断客户端请求，底层模型释放资源的时间取决于推理引擎。

## 翻译扩展与设置

`ITranslationService` 是翻译协议扩展边界，当前实现支持 OpenAI Chat Completions 及兼容服务。自定义提供商可填写名称、地址及密钥，再从服务返回的列表选择模型；暂不直接支持 Anthropic 原生等其他协议。

DeepSeek 预设地址为 `https://api.deepseek.com`，使用独立的 DeepSeek API Key。模型从 `/models` 动态获取，不硬编码旧模型名称。模型返回 `effort.supported_levels` 时，会用它生成强度选项；没有元数据时使用已知协议选项，实际支持情况以接口测试为准。

OpenAI 通过 `reasoning_effort` 传递所选档位。DeepSeek 通过 `thinking.type` 开启或关闭思考，并在开启时发送 `reasoning_effort`；默认档位不强制发送这些参数。结果框只显示最终译文，不混入 DeepSeek 的 `reasoning_content`。OpenAI 能力规则集中在 `ReasoningOptions.cs`，新模型或兼容服务支持情况不明确时可使用「模型默认」。

接口自动补全 `/chat/completions`；也允许直接填写完整端点。请求包含 system / user 消息，未强制设置 temperature 等模型专属参数。超时范围为 15–600 秒，并支持取消和明确的鉴权/限流错误提示。

配置保存在 `%APPDATA%/Mi OCR Desk/settings.json`，当前供应商及所有备用供应商的 API Key 都使用当前用户 Windows DPAPI 加密，写入使用临时文件替换。旧版本的单供应商配置会自动迁移，已有 Key 保留。复制此配置到另一台机器后需重新填写密钥。

## 验证与手动检查

自动测试覆盖 XAML 资源与窗口构造（不显示窗口）、快捷键、负坐标桌面裁剪、任务状态、设置加密与损坏恢复、翻译请求/响应/错误及取消。

新增测试覆盖双语视图同步与 Unicode、修改原文 / 语言后清除旧译文、负坐标及缩放屏幕内悬浮窗定位、内嵌设置与自定义语言、启动偏好持久化、启动命令路径引用、单实例通信，以及模型卸载协议、离线退出、失败提示和自有进程清理。测试不修改本机登录启动项，不展示或自动操作桌面窗口。

截图并翻译测试覆盖两个快捷键的独立持久化、等价组合键冲突、互换与占用回滚，以及 OCR → 翻译顺序、输出语言、仅识别、空白结果、失败与各阶段取消。接口均采用模拟响应，不调用付费翻译服务。

供应商与推理测试覆盖：OpenAI → DeepSeek → 自定义 → OpenAI 的未保存草稿恢复、独立密钥、取消编辑隔离、旧配置迁移、所有供应商密钥加密、重启后恢复、模型强度选项、DeepSeek 模型能力元数据、两家推理参数请求格式及最终译文提取。DeepSeek 的模拟接口测试不代表真实账号已连通，需填写自己的 Key 后点击测试连接。

模型列表测试覆盖不可编辑下拉框、凭据变更后清空列表、模型接口地址、鉴权、去重排序、文本候选过滤、自定义服务、空列表、错误及取消。需要主动验证已保存的 OpenAI 配置时，运行以下命令；它会获取模型列表并发送一次 `Hello, world.` 翻译请求，可能产生少量 API 计费，不输出密钥：

```powershell
$env:DOTNET_ROOT = "$PWD/.dotnet"
./.dotnet/dotnet.exe run --project MiOcrDesk.Tests -- --translation-integration
```

本地真实模型集成测试使用程序生成的英文测试图片，验证识别 → 卸载 → 再次自动加载 → 卸载清理，不读取用户截图，也不调用付费翻译服务：

```powershell
$env:DOTNET_ROOT = "$PWD/.dotnet"
$env:MIOCR_RUNTIME_ROOT = "$PWD"
./.dotnet/dotnet.exe run --project MiOcrDesk.Tests -- --integration
```

UI 按用户要求留给手动检查，建议关注：

1. 关闭旧版应用后运行新版，检查 100% / 125% / 150% 缩放及最小窗口布局。
2. 用按钮和全局快捷键截图；快捷键应只显示悬浮窗，Esc / 右键取消应恢复此前状态，后台截图取消后不应弹出主界面。
3. 多显示器、左侧负坐标显示器、混合 DPI 下检查截图预览与所选区域一致。
4. 识别过程中取消、重试；导入图片、剪贴板粘贴、复制及导出。
5. 更换快捷键、测试占用提示；保存设置并重启确认。
6. 填入自己的翻译 API Key，刷新模型列表并选择，测试连接、切换目标语言、修改提示词，再翻译真实文本。
   检查 OpenAI 与 DeepSeek 来回切换时 Key、模型、推理强度均恢复；保存并重启再确认。
7. 在无开发环境的新 Windows 电脑验证完整离线安装包。
8. 关闭主窗口后测试托盘截图；保存静默启动后重启程序，确认仅托盘常驻；开启自启动后重新登录 Windows 确认行为。
9. 对照模式检查中英、日语或其他语言的编辑、复制与导出；设置页切换后确认草稿仍在，保存或放弃修改能正常生效。
10. 点击「关闭 OCR 模型」，确认模型内存释放；再次截图应自动加载。托盘退出后确认模型释放，程序退出；模型加载过程中也检查取消和退出。
11. 按 `Ctrl+Alt+T` 框选文字，确认自动得到双语结果；在识别和翻译阶段分别按 `Esc`，确认只关闭 OCR 悬浮窗并取消任务，之后两个截图快捷键仍可使用。

协议参考：[OpenAI Chat Completions](https://developers.openai.com/api/reference/resources/chat)、[OpenAI 模型列表](https://developers.openai.com/api/reference/resources/models/methods/list)、[Ollama Chat](https://docs.ollama.com/api/chat)、[.NET 10](https://dotnet.microsoft.com/en-us/download/dotnet/10.0)。

推理与 DeepSeek 参考：[OpenAI 推理模型](https://developers.openai.com/api/docs/guides/reasoning)、[DeepSeek 接入](https://api-docs.deepseek.com/)、[DeepSeek 思考模式](https://api-docs.deepseek.com/guides/thinking_mode/)、[DeepSeek 模型元数据](https://api-docs.deepseek.com/api/list-models/)。

生命周期参考：[Windows 当前用户登录启动项](https://learn.microsoft.com/windows/win32/setupapi/run-and-runonce-registry-keys)、[Ollama 模型卸载](https://github.com/ollama/ollama/blob/main/docs/api.md#unload-a-model)。
