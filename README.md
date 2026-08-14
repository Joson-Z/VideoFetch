# VideoFetch

VideoFetch 是一个面向 Windows 的桌面视频拉取客户端。首期支持用户对自己有权访问和保存的 B 站视频进行格式探测、画质选择、下载及 MP4 合并。

## 当前状态

首个可运行 MVP 已完成。产品范围与技术设计见 [视频拉取客户端-需求与技术设计.md](./视频拉取客户端-需求与技术设计.md)。

已经实现：

- 从本机 Edge/Chrome 读取用户已有登录态，不收集账号密码。
- 展示当前登录账号实际可用的画质与音质。
- 默认下载最高可用的视频流和音频流。
- 使用 FFmpeg 自动合并或转封装为 MP4。
- 按视频标题生成 Windows 兼容的文件名。
- 提供下载进度、取消、错误提示和结果校验。
- “通用兼容 MP4”模式在必要时使用 FFmpeg 转码为 H.264/AAC。

多任务队列、多 P/合集选择和应用内工具更新属于后续版本范围。

## 项目结构

```text
src/
  VideoFetch.Domain/          领域模型与状态
  VideoFetch.Application/     用例、接口与业务规则
  VideoFetch.Infrastructure/  yt-dlp、FFmpeg、文件系统实现
  VideoFetch.App/             WPF 桌面应用
tests/
  VideoFetch.UnitTests/       单元测试
```

## 开发环境

- Windows 10/11 x64
- .NET 8 SDK 或能构建 `net8.0-windows` 的更新版 SDK
- 后续运行阶段需要 `yt-dlp.exe`、`ffmpeg.exe` 和 `ffprobe.exe`

```powershell
dotnet restore VideoFetch.sln
dotnet build VideoFetch.sln
dotnet test VideoFetch.sln
```

也可以运行统一验证脚本：

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File .\scripts\verify.ps1
```

## 准备外部工具

客户端启动后会检测以下文件：

- `yt-dlp.exe`
- `ffmpeg.exe`
- `ffprobe.exe`

将它们放到应用同目录下的 `tools` 文件夹，或在界面中选择其他工具目录。仓库中的 [tools/README.md](./tools/README.md) 列出了官方获取渠道。不要提交 Cookie 文件或第三方二进制文件到仓库。

## 使用流程

1. 启动客户端并选择工具目录，确认三个工具均检测通过。
2. 选择 Edge、Chrome、Cookie 文件或匿名模式。客户端不提供账号密码输入框。
3. 输入 B 站视频链接，点击“检测登录并解析画质”。
4. 选择画质上限和 MP4 输出模式。
5. 选择保存目录并开始下载。
6. 客户端下载最高可用音频，自动合并为 MP4，并使用 ffprobe 校验结果。

## 发布 Windows x64 版本

先把需要随包分发的三个工具放入仓库根目录的 `tools` 文件夹，然后运行：

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File .\scripts\publish-win-x64.ps1
```

输出位于 `artifacts\VideoFetch-win-x64`。发布脚本生成 .NET 自包含的单文件客户端，因此目标电脑无需预装 Python 或 .NET 运行时。

如果刚运行过完整验证，可用 `-SkipTests` 跳过重复测试：

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File .\scripts\publish-win-x64.ps1 -SkipTests
```

## 使用边界

本项目仅用于下载用户拥有版权、已获授权或平台明确允许离线保存的内容。项目不提供绕过 DRM、付费权限、区域限制、验证码、风控或其他访问控制的功能，也不会上传用户 Cookie。

## 许可证

[Apache License 2.0](./LICENSE)
