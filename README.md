# ArknightsMapViewer

明日方舟地图文件查看工具：读取解包后的关卡 JSON，查看地图地块、敌人路线、出怪安排和预设装置，并导出图片。

- 作者：winny727
- 当前源码程序集版本：`0.5.0.1`
- 文档更新：2026-10-29（文档日期，不代表程序发布日期）
- [项目主页](https://github.com/winny727/ArknightsMapViewer) · [使用介绍（b站专栏）](https://www.bilibili.com/opus/1038286649940770816)

![程序界面](https://github.com/user-attachments/assets/7a8b4025-3e46-47da-b744-6ebba85f9b5b)

## 功能与范围

- 打开单个或多个地图 JSON，也可拖入文件夹递归读取其中的 JSON。
- 显示地块类型、坐标和属性，查看初始装置、隐藏装置及预设激活事件。
- 查看普通路线和额外路线，标记途经点、等待时间和路径长度。
- 按波次、片段、出怪事件浏览数据，并按配置时间整理出怪列表。
- 查看敌人数据，选择随机出怪分组，控制隐藏组和未选择分组的显示。
- 搜索树状列表，导出地图背景、路线图或合成图片（PNG）。
- 搜索关卡列表，从 PRTS.Map 或 GitHub 下载地图，更新本地游戏配置数据。

**时间轴模拟尚未完成，控件默认隐藏。** 源码中的 `Timeline Simulation（施工中）` 使用占位波次和时长，没有接入真实关卡的动态模拟。出怪列表中的时间按配置计算，不能据此精确复现实战中的波次推进；绘制的寻路结果也不保证与游戏完全一致。本工具读取已解包的 JSON，不提供游戏资源解包功能。

## 运行与快速使用

程序采用 Windows Forms，目标运行环境为 Windows / .NET Framework 4.6.1。运行时请保留 EXE 同目录下的依赖 DLL 和 `Config` 文件夹。用户配置、在线更新数据和下载地图保存在 EXE 同级的 `UserData` 中，移动程序时请复制整个程序目录。

1. 启动 `ArknightsMapViewer.exe`。
2. 通过 `Menu → Open` 选择地图 JSON，或将文件、文件夹拖入窗口。
3. 在左侧树状列表选择路线、波次或出怪事件，在右侧查看地图和详细数据。点击地图格子可以查看地块信息。
4. 使用左上方搜索框高亮匹配节点，通过复选框切换敌人数据、装置、路径长度等显示。
5. 右键树节点，选择 `Export`：`Full Image` 导出合成图，`Route Only` 导出路线图，`Background` 导出地图背景，`All` 导出上述各类图片。导出会遍历所选节点及其子节点中的路线。

### 获取关卡和更新数据

- 打开 `Data → Open MapList Window`，搜索并选择关卡。已有本地文件可直接打开；也可以选择下载源并点击“下载”，完成后按提示打开。
- 下载的地图保存到程序目录下的 `UserData/gamedata/levels/`。读取时优先使用这里的文件，找不到时仍可打开旧的 `Config/gamedata/levels/` 中的地图，不会自动搬移或删除旧文件。
- `Data → Update GameData` 从 GitHub 更新默认目录 `Config/gamedata/excel/` 与用户目录 `UserData/gamedata/excel/` 中已有 JSON 文件的去重并集，以及敌人数据库。更新结果只写入 `UserData/gamedata/`，完成后需要重启程序。此操作不会批量下载所有关卡地图，也不会更新程序代码。
- 关卡列表由多个游戏配置表生成，可能不完整。可将 `Config/stage_info.txt` 复制到 `UserData/Config/stage_info.txt` 后补充关卡信息；部分关卡的 PRTS.Map 页面跳转可能不可用。

### 数据节点和分组

- `routes` / `extraRoutes`：普通路线 / 额外路线。
- `waves` / `branches`：波次及其事件 / 分支数据。
- `predefines`：预设角色、装置等数据。
- `spawns`：按配置时间排序的出怪或预设激活事件。
- `groups`：出怪分组整理。

隐藏组 `hiddenGroup` 用于外部条件控制的出怪，例如部分危机合约条件。随机组通过 `randomSpawnGroupKey` 表示互斥选择，通过 `randomSpawnGroupPackKey` 表示打包关联，并带有权重信息。

在 `spawns` 中点击事件可以选择互斥分组；其他选项会变灰，勾选“隐藏未选择分组”后会隐藏这些节点。“显示预设激活行为”用于控制出怪列表中是否同时显示装置等预设对象的激活事件。

## 配置文件与数据来源

下列路径均相对于程序所在目录。`Config` 保存随程序提供的默认配置和基础游戏数据；`UserData` 保存用户修改和下载数据。

| 默认路径 | 用户覆盖路径 | 用途 |
| --- | --- | --- |
| `Config/draw_config.json` | `UserData/Config/draw_config.json` | 绘图尺寸、字体和颜色 |
| `Config/tile_info.txt` | `UserData/Config/tile_info.txt` | 地块名称、颜色、文字等，采用制表符分隔 |
| `Config/stage_info.txt` | `UserData/Config/stage_info.txt` | 手动补充关卡信息，采用制表符分隔 |
| `Config/gamedata/excel/character_table.json` | `UserData/gamedata/excel/character_table.json` | 角色数据 |
| `Config/gamedata/excel/` 中的其他 JSON | `UserData/gamedata/excel/` 中的同名文件 | 关卡列表及相关分类数据 |
| `Config/gamedata/levels/enemydata/enemy_database.json` | `UserData/gamedata/levels/enemydata/enemy_database.json` | 敌人数据库 |

读取按文件优先使用用户覆盖，不存在时回退到默认文件；不会合并两个文件的内容。修改配置前，请将对应默认文件复制到用户覆盖路径再编辑，修改后重启程序。删除对应用户覆盖文件即可恢复使用默认文件。

`draw_config.json` 可以只填写需要修改的字段。缺失字段使用内置默认值；对象组为 `null`、类型错误、非法颜色、字体或数值会按字段回退并写入日志，其他有效设置继续生效。JSON 语法错误或根节点不是对象时使用整份内置默认配置。程序保留原配置文件，不会用修正结果覆盖它；可在 `Help → Show Log` 查看具体字段和原因。

绘图参数规则：

- `Size.tilePixle`、`circleRadius`、`pointRadius` 为 1–512 的整数；`lineWidth`、`circleEdgeWidth` 为 1–128 的整数。沿用原有 DPI 缩放，缩放取整后的尺寸至少为 1 像素。这些范围用于限制异常参数，不保证任意大地图的图片都能放入内存。
- 字号为 1–256 的有限数字，不接受 0、负数、NaN 或 Infinity。字体名应为系统已安装字体；不可用时使用默认字体，默认字体也不可用时使用系统字体。样式支持 `Regular`、`Bold`、`Italic`、`Underline`、`Strikeout` 及逗号分隔的组合，例如 `"Bold, Italic"`。
- 颜色支持 `#RGB`、`#RRGGBB`、`#AARRGGBB` 和已知颜色名称（如 `Red`、`Transparent`）；空串、未知名称、裸数字以及其他长度的十六进制串视为无效。
- 数值字段使用 JSON 数字，名称、样式和颜色使用 JSON 字符串。现有键名 `lenghtFont` / `lenghtFontSize` / `lenghtFontStyle` 保持兼容，未改拼写。

如果以前直接修改过程序目录下的 `Config`，请在重新构建或覆盖安装前，将需要保留的配置复制到上述用户覆盖路径；已更新的游戏数据对应复制到 `UserData/gamedata/`。旧地图可以继续留在原目录，也可手动复制到 `UserData/gamedata/levels/`。程序不会自动迁移这些文件。

没有配置颜色的地块会按高台、地面及可部署属性使用默认颜色；界面提供“标记未定义地块”选项辅助识别。

地图与游戏配置可自行解包，或从以下项目取得：

- [关卡地图文件](https://github.com/Kengxxiao/ArknightsGameData/tree/master/zh_CN/gamedata/levels)
- [角色数据 character_table.json](https://github.com/Kengxxiao/ArknightsGameData/blob/master/zh_CN/gamedata/excel/character_table.json)
- [敌人数据 enemy_database.json](https://github.com/Kengxxiao/ArknightsGameData/blob/master/zh_CN/gamedata/levels/enemydata/enemy_database.json)

游戏更新可能改变数据格式，导致解析失败或关卡列表不完整。更新数据不能自动解决所有格式兼容问题，必要时还需修改程序。

## 从源码构建

1. 在 Windows 上使用支持 C# 8.0 的 Visual Studio / MSBuild，并安装 .NET 桌面开发工具及 .NET Framework 4.6.1 目标框架引用程序集。
2. 打开 `ArknightsMapViewer.sln`，还原 NuGet 依赖（Newtonsoft.Json 13.0.3、System.ValueTuple 4.5.0）。
3. 构建 Debug 或 Release 配置。输出位于 `ArknightsMapViewer/bin/Debug/` 或 `ArknightsMapViewer/bin/Release/`；MSBuild 使用 `CopyToOutputDirectory=PreserveNewest` 增量复制默认 `Config` 和 `Readme.txt`。

`UserData` 不作为构建内容复制或打包，构建清理不会主动删除它；手动删除整个输出目录仍会丢失其中的用户数据。清理或重新部署前请保留 `UserData`，便携迁移时将它与 EXE 一起复制。

主要代码：`ArknightsMap` 定义并解析关卡数据，`View/DataView` 和 `View/LevelViewHelper.cs` 组织显示数据，`View/Drawer` 绘制地图与路线，`Utils` 提供寻路和配置读取，`MainForm.cs` 与 `StageForm.cs` 负责窗口交互。

`Tools/ArknightsMapTileCollector` 是独立辅助工具，不包含在主解决方案中。它可扫描目录内的 JSON 并收集 `tileKey`，输出 `MapTileCollect.txt`，用于维护地块配置。

## 维护记录与反馈

- 2026-07-18：更新黑流树海、全息作战矩阵第二期相关读取和游戏数据。
- 2026-06：更新游戏数据及部分关卡配置，适配生息演算“重启锚点”。
- 2025-10：新增关卡选择、地图下载与游戏数据更新，后续修复部分关卡解析；当前源码程序集版本为 `0.5.0.1`。

以上记录依据仓库提交整理。程序信息窗口中的日期仍为代码内写死的 `20251021`，不能用来判断最近一次维护时间。

遇到问题时，可通过 `Help → Show Log` 查看日志，并携带问题截图、关卡文件名和日志联系 b站作者 winny727。
