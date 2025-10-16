明日方舟地图文件查看工具
ArknightsMapViewer
Version: 0.5 (20251016)
By winny727

https://github.com/winny727/ArknightsMapViewer
b站专栏：https://www.bilibili.com/opus/1038286649940770816

================
2025.10.16 更新
1. 新增关卡选择窗口，在菜单栏-Data-Open Stages Window打开，通过读取游戏数据（gamedata/excel）中的多个json文件中的关卡配置数据来显示关卡列表。
（可能不全，游戏数据中不存在的关卡可以在Config/stage_info.txt中手动配置，比如多维合作和危机合约）
选择关卡后，可以直接下载对应地图文件到本地打开查看。
（注：部分地图无法正确跳转到PRTS.Map对应页面，不过还是可以正常下载的）
2. 新增游戏数据更新功能，在菜单栏-Data-Update GameData中执行，可更新用于显示敌人名称等的敌人数据文件或新增关卡列表等；

================

使用说明：
找到你要查看的地图的文件，并拖拽到程序中打开即可；
Config文件夹中存放了方舟解包出来的敌人数据enemy_database.json和角色数据character_table.json，用于显示敌人名称等，若有需要可自行更新覆盖；

地图文件和角色/敌人数据可以自己解包，Github也有大佬传了：
地图文件：https://github.com/Kengxxiao/ArknightsGameData/tree/master/zh_CN/gamedata/levels
角色数据文件：https://github.com/Kengxxiao/ArknightsGameData/blob/master/zh_CN/gamedata/excel/chapter_table.json
敌人数据文件：https://github.com/Kengxxiao/ArknightsGameData/tree/master/zh_CN/gamedata/levels/enemydata/enemy_database.json

右侧地图格子可以点击查看信息，每个格子具体的显示颜色在Config/tile_info.csv中配置，若未配置则会按格子的属性（高台/地面/禁入区）自动填颜色；
勾选“标记未定义地块”会用斜线来标记这些未定义格子；

routes、extraRoutes、waves、branches、predefines是地图数据文件中的数据结构，spawns是按时间排序的出怪（或装置激活）行为，groups是刷怪分组整理；
地图中的装置初始有显示和隐藏两种状态，隐藏的装置在出怪的时候可以被激活；
“显示预设激活行为”勾选框是指spawns中是只显示刷怪路线还是也要显示装置激活的事件；
地图文件中刷怪分组分为隐藏分组hiddenGroup和随机分组randomSpawnGroup；
隐藏分组是由外部控制的刷怪分组，比如危机合约中点tag就多几个怪这种；
随机分组分为互斥分组和打包分组；互斥分组按权重刷出后再把同在一个打包分组里的怪一起刷出；
spawns中可以通过点击来主动选择互斥分组，选中后互斥分组内的其它出怪就会变成灰色，如果勾选了“隐藏未选择分组”就会隐藏这些灰色的出怪；

左侧上方有搜索框，可以高亮搜索项；
左侧节点右键点击可选择导出当前图片；

若后续明日方舟官方又更新了地图文件或者敌人数据文件的格式，可能会因为格式不兼容导致本工具无法解析。
如有bug可以找我。