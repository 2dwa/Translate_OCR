# Runtime license sources

The separate model ZIP retains the original `GO_LICENSE`, `LLAMA_CPP_LICENSE`,
`LLAMA_CPP_VENDORS_LICENSE` and `CPP_HTTPLIB_LICENSE` alongside the unmodified
Ollama 0.40.14 libraries. The following supplemental notices accompany the
CPU/CUDA 13 libraries. They do not change the licenses of those libraries.

| Local file | Original source |
| --- | --- |
| LLVM-LICENSE.txt | https://github.com/llvm/llvm-project/blob/llvmorg-21.1.0/LICENSE.TXT |
| libcxx-LICENSE.txt | https://github.com/llvm/llvm-project/blob/llvmorg-21.1.0/libcxx/LICENSE.TXT |
| libunwind-LICENSE.txt | https://github.com/llvm/llvm-project/blob/llvmorg-21.1.0/libunwind/LICENSE.TXT |
| OpenMP-LICENSE.txt | https://github.com/llvm/llvm-project/blob/llvmorg-21.1.0/openmp/LICENSE.TXT |
| winpthreads-COPYING.txt | https://github.com/mingw-w64/mingw-w64/blob/master/mingw-w64-libraries/winpthreads/COPYING |
| NVIDIA-CUDA-13.0-EULA.html | https://docs.nvidia.com/cuda/archive/13.0.0/eula/index.html |
| Microsoft-VC-Runtime-2015-2022-License.docx | https://visualstudio.microsoft.com/wp-content/uploads/2021/09/Visual-C-Runtime-2015-2022-License-1.docx |

The Microsoft `.txt` file is a text extraction of the accompanying original
`.docx`. LLVM license references identify the source of the notice text, not
the exact compiler version used by the upstream binary builder.

NVIDIA libraries are redistributed only as dependencies of this application's
OCR functionality, under the CUDA license and redistribution supplement,
including its third-party notices. No NVIDIA driver is included. CUDA software
is not licensed under this project's Apache-2.0 license. Use of NVIDIA
components is subject to the included NVIDIA agreement; do not separate or
redistribute them independently of the application except as that agreement
permits. To the extent permitted by applicable law, do not reverse engineer,
decompile or disassemble NVIDIA proprietary components. NVIDIA and its
licensors retain all ownership and intellectual-property rights.

Microsoft runtime DLLs are retained as dependencies of the upstream Ollama
program, with their Microsoft terms. See the official
[Visual Studio redistributable list](https://learn.microsoft.com/en-us/visualstudio/releases/2022/redistribution)
and [runtime license page](https://visualstudio.microsoft.com/license-terms/vs2022-cruntime/).
Neither the Ollama MIT license nor Mi OCR Desk's Apache-2.0 license relicenses
these proprietary components.

分发时请保留全部许可文件。NVIDIA CUDA 与 Microsoft 运行库分别适用其原有条款，
只作为应用依赖提供；项目的 Apache-2.0 许可不覆盖或替代这些条款。
