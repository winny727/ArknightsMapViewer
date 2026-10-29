明日方舟地图文件查看工具
ArknightsMapViewer
当前源码程序集版本：0.5.0.1
文档更新：2026-10-29（文档日期，不代表程序发布日期）
By winny727

项目主页：https://github.com/winny727/ArknightsMapViewer
b站专栏：https://www.bilibili.com/opus/1038286649940770816

================
用途与运行环境

读取解包后的关卡 JSON，查看地图地块、敌人路线、出怪安排、分组和预设装置，并导出 PNG 图片。
程序采用 Windows Forms，目标运行环境为 Windows / .NET Framework 4.6.1。
运行时请保留 ArknightsMapViewer.exe 同目录下的依赖 DLL 和 Config 文件夹。
用户配置、在线更新数据和下载地图保存在 EXE 同级的 UserData 中，移动程序时请复制整个程序目录。

时间轴模拟尚未完成，控件默认隐藏：源码中的 Timeline Simulation（施工中）使用占位波次和时长，没有接入真实关卡动态模拟。
出怪列表中的时间按配置计算，不能据此精确复现实战中的波次推进；绘制的寻路结果也不保证与游戏完全一致。
本工具读取已解包的 JSON，不提供游戏资源解包功能。

================
快速使用

1. 启动 ArknightsMapViewer.exe，通过 Menu → Open 选择地图 JSON，或将文件、文件夹拖入窗口。支持多文件及递归读取文件夹。
2. 在左侧树状列表选择路线、波次或出怪事件，查看右侧地图和详细数据；点击地图格子可查看属性。
3. 左上方搜索框可高亮匹配节点，复选框可切换敌人数据、装置、路径长度等显示。
4. 右键树节点 → Export，可选择 Full Image（合成图）、Route Only（路线图）、Background（地图背景）或 All（上述各类图片）。导出会遍历所选节点及其子节点中的路线，格式为 PNG。

获取关卡：
在 Data → Open MapList Window 中搜索和选择关卡。已有本地文件可直接打开，也可以选择 PRTS.Map 或 Github/Kengxxiao 下载源，点击“下载”，完成后按提示打开。
下载的地图保存到程序目录下的 UserData/gamedata/levels/。读取时优先使用这里的文件，找不到时仍可打开旧的 Config/gamedata/levels/ 中的地图，不会自动搬移或删除旧文件。
关卡列表可能不完整，可将 Config/stage_info.txt 复制到 UserData/Config/stage_info.txt 后补充关卡信息；部分关卡的 PRTS.Map 页面跳转可能不可用。

更新游戏数据：
执行 Data → Update GameData，从 GitHub 更新默认目录 Config/gamedata/excel/ 与用户目录 UserData/gamedata/excel/ 中已有 JSON 文件的去重并集，以及敌人数据库。
更新结果只写入 UserData/gamedata/，更新后需要重启程序。
此功能不会批量下载所有关卡地图，也不会更新程序代码。

================
数据节点与分组

routes：普通路线；extraRoutes：额外路线。
waves：波次及其事件；branches：分支数据；predefines：预设角色、装置等数据。
spawns：按配置时间排序的出怪或预设激活事件；groups：出怪分组整理。

地图中的装置可有初始显示和隐藏状态，隐藏装置可通过激活事件出现。
“显示预设激活行为”用于控制出怪列表中是否同时显示装置等预设对象的激活事件。

隐藏组 hiddenGroup 用于外部条件控制的出怪，例如部分危机合约条件。
随机组通过 randomSpawnGroupKey 表示互斥选择，通过 randomSpawnGroupPackKey 表示打包关联，并带有权重信息。
在 spawns 中点击事件可以选择互斥分组；其他选项会变灰，勾选“隐藏未选择分组”后会隐藏这些节点。

================
配置文件（路径相对于程序目录）

Config 保存随程序提供的默认配置和基础游戏数据；UserData 保存用户修改和下载数据。

绘图尺寸、字体和颜色：默认 Config/draw_config.json，用户覆盖 UserData/Config/draw_config.json。
地块名称、颜色和文字：默认 Config/tile_info.txt，用户覆盖 UserData/Config/tile_info.txt，采用制表符分隔。
补充关卡信息：默认 Config/stage_info.txt，用户覆盖 UserData/Config/stage_info.txt，采用制表符分隔。
角色数据：默认 Config/gamedata/excel/character_table.json，用户覆盖 UserData/gamedata/excel/character_table.json。
关卡列表及相关分类数据：默认 Config/gamedata/excel/ 中的其他 JSON，用户覆盖 UserData/gamedata/excel/ 中的同名文件。
敌人数据库：默认 Config/gamedata/levels/enemydata/enemy_database.json，用户覆盖 UserData/gamedata/levels/enemydata/enemy_database.json。

读取按文件优先使用用户覆盖，不存在时回退到默认文件；不会合并两个文件的内容。修改配置前，请将对应默认文件复制到用户覆盖路径再编辑，修改后重启程序。删除对应用户覆盖文件即可恢复使用默认文件。

如果以前直接修改过程序目录下的 Config，请在重新构建或覆盖安装前，将需要保留的配置复制到上述用户覆盖路径；已更新的游戏数据对应复制到 UserData/gamedata/。
旧地图可以继续留在原目录，也可手动复制到 UserData/gamedata/levels/。程序不会自动迁移这些文件。

没有配置颜色的地块会按高台、地面及可部署属性使用默认颜色；界面提供“标记未定义地块”选项辅助识别。

地图与游戏配置可自行解包，也可从以下项目取得：
地图：https://github.com/Kengxxiao/ArknightsGameData/tree/master/zh_CN/gamedata/levels
角色：https://github.com/Kengxxiao/ArknightsGameData/blob/master/zh_CN/gamedata/excel/character_table.json
敌人：https://github.com/Kengxxiao/ArknightsGameData/blob/master/zh_CN/gamedata/levels/enemydata/enemy_database.json

游戏更新可能改变数据格式，导致解析失败或关卡列表不完整。更新数据不能自动解决所有格式兼容问题，必要时还需修改程序。

================
从源码构建

在 Windows 上使用支持 C# 8.0 的 Visual Studio / MSBuild，安装 .NET 桌面开发工具及 .NET Framework 4.6.1 目标框架引用程序集。
打开 ArknightsMapViewer.sln，还原 NuGet 依赖（Newtonsoft.Json 13.0.3、System.ValueTuple 4.5.0），构建 Debug 或 Release 配置。
输出位于 ArknightsMapViewer/bin/Debug/ 或 ArknightsMapViewer/bin/Release/；MSBuild 使用 CopyToOutputDirectory=PreserveNewest 增量复制默认 Config 和本说明文件。
UserData 不作为构建内容复制或打包，构建清理不会主动删除它；手动删除整个输出目录仍会丢失其中的用户数据。清理或重新部署前请保留 UserData，便携迁移时将它与 EXE 一起复制。
代码结构和辅助地块收集工具说明见源码仓库中的 README.md。

================
维护记录与反馈

2026-07-18：更新黑流树海、全息作战矩阵第二期相关读取和游戏数据。
2026-06：更新游戏数据及部分关卡配置，适配生息演算“重启锚点”。
2025-10：新增关卡选择、地图下载与游戏数据更新，后续修复部分关卡解析；当前源码程序集版本为 0.5.0.1。

以上记录依据仓库提交整理。程序信息窗口中的日期仍为代码内写死的 20251021，不能用来判断最近一次维护时间。
遇到问题时，可通过 Help → Show Log 查看日志，并携带问题截图、关卡文件名和日志联系 b站作者 winny727。
