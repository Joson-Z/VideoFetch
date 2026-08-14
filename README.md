# VideoFetch

VideoFetch 是一个面向 Windows 的桌面视频拉取客户端。首期支持用户对自己有权访问和保存的 B 站视频进行格式探测、画质选择、下载及 MP4 合并。

## 当前状态

项目处于早期开发阶段。产品范围与技术设计见 [视频拉取客户端-需求与技术设计.md](./视频拉取客户端-需求与技术设计.md)。

计划中的核心能力：

- 从本机 Edge/Chrome 读取用户已有登录态，不收集账号密码。
- 展示当前登录账号实际可用的画质与音质。
- 默认下载最高可用的视频流和音频流。
- 使用 FFmpeg 自动合并或转封装为 MP4。
- 按视频标题生成 Windows 兼容的文件名。
- 提供任务队列、进度、取消、重试和结果校验。

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

## 使用边界

本项目仅用于下载用户拥有版权、已获授权或平台明确允许离线保存的内容。项目不提供绕过 DRM、付费权限、区域限制、验证码、风控或其他访问控制的功能，也不会上传用户 Cookie。

## 许可证

[Apache License 2.0](./LICENSE)
