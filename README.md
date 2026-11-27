# Gridline · 单人格斗训练场

靶场可以选择港区夜市屋顶或校园广场，两张地图都使用原有角色和固定训练靶。

[主菜单背景](Assets/Training/Art/CampusMenuBackdrop.png)用内置 ImageGen 参考校园照片重绘。画面以黄昏中的白色环顶、弧形露台和玻璃立面为主体，左侧留给菜单按钮。

[校园广场](Assets/Training/Art/CampusPlaza.png)用内置 ImageGen 参考主菜单建筑和夜市屋顶的侧视构图绘制，保留环顶和弧形露台，前景是开阔平地。

选角画面提供根据全身照制作的 [Photo Fighter](Assets/Training/Art/PhotoFighter/PhotoFighter.asset) 和原创的 [Rooftop Boxer](Assets/Training/Art/RooftopBoxer/RooftopBoxer.asset)。Photo Fighter 的走路循环有五帧，素材见 [角色原始帧表](ArtSource/PhotoFighter.png) 和 [新增过步帧](ArtSource/PhotoFighter-WalkTransition.png)。

## 启动

在 Unity Hub 中打开 `Webbox` 工程（Unity **6000.6.5f1**），打开 `Assets/Scenes/SampleScene.unity` 并点击 **Play**。主菜单有 **Training Range（靶场）** 和 **Two Players（双人，暂未开放）**。从靶场入口先选地图，再选角色。点击地图卡片或角色头像，也可以用左右方向键 / **A / D** 选择，按 **Enter** 确认。训练中按 **Esc** 可以依次返回选角、选地图和主菜单。每次进入训练都会重置拳手位置和靶子血量。如果键盘没有响应，先点击 **Game** 视图。

| 键位 | 操作 |
|---|---|
| A / D | 向左 / 向右移动并转向 |
| W | 跳跃，空中可用 A / D 调整位置 |
| S（按住） | 下蹲 |
| Shift（按住） | 抱架。训练靶不会反击，可用来查看抱架姿势 |
| J | 轻拳，命中后可再按 J 接勾拳 |
| K | 重拳，也可在勾拳命中后接入 |
| R | 回到初始位置，恢复靶子生命值并清除本次出拳状态 |
| H | 开关判定框。绿色为靶子受击框，红色为命中窗口内的攻击框 |

站在起点按 J 可打空，血量保持 100。按 D 靠近靶子，松开后按 J 出拳。底部出现 `LINK J` 时再按一次 J，勾拳的 `LINK K` 出现时按 K。三段连击全中会显示 `3 HITS`，靶子生命值从 100 降到 58。重拳也可以单独使用。按 R 清空当前动作并恢复靶子。可走过靶子，从另一侧转身出拳。

底部显示前摇（WINDUP）、命中窗口（ACTIVE）、收招（RECOVERY）和结果（HIT / MISS）。出拳期间锁定位置和朝向。命中时，人物短暂停在接触帧，训练靶闪光并向后摆动。

## 制作人物包

《拳皇 ’97》街机版运行在 Neo Geo MVS 上。硬件把 **16 × 16 px** 的图块拼成精灵，图块使用的调色板有 **15 个可见颜色加 1 个透明槽位**。**320 × 224 px** 是 NTSC 显示区域。整个人物帧由多个图块构成，因此按姿势留出空间即可。[SNK Neo Geo 开发手册](https://www.neogeodev.org/NG.pdf) · [MAME 街机版记录](https://github.com/mamedev/mame/blob/master/src/mame/snk/neogeo.cpp)

1. 以 **16 px 网格**整理自制或有权使用的透明 PNG。示例包的每帧为 **96 × 80 px**（6 × 5 个图块），角色朝右。需要更多空间的动作可以扩展画布，所有帧使用同一个脚底基准。
2. 导入为 **Sprite (2D and UI)**。单帧 PNG 选 **Single**，精灵图集选 **Multiple** 后在 **Sprite Editor** 切分。脚底在帧底边中央时用 **Bottom Center**，在其他位置时用 **Custom Pivot**。
3. 用 **Create → Webbox → Fighter Sprite Pack** 新建包，或复制 [RooftopBoxer.asset](Assets/Training/Art/RooftopBoxer/RooftopBoxer.asset)。按顺序填入各姿势帧和每种拳法的 **Windup / Active / Recovery Frames**。跳跃帧使用透明背景，地面影子可单独填入 **Air Shadow**。在包的 Inspector 点 **Match Arcade Sprite Scale**，工具会用首张待机帧里完全不透明的像素算站立高度，为全包统一 PPU，并应用 Point 采样、关闭 mipmap 和默认纹理压缩。共享同一个 PNG 的人物包会一起受到导入设置的影响。目标站立高度约 **2.37 个 Unity 单位**。示例角色为 **71 px ÷ 30 PPU**。[Unity PPU 文档](https://docs.unity3d.com/cn/6000.0/ScriptReference/TextureImporterSettings-spritePixelsPerUnit.html)
4. 将包拖到场景的 **Player → Training Fighter → Sprite Pack**。在 **Training Fighter** 的 **Light Punch** 和 **Follow-up Punches** 中可调整各拳的帧段时长、伤害、命中停顿和攻击框，在 **Training Target** 中可调整受击框。按 **H** 对齐新动作。场景中的 **Training Target → Artwork** 可替换靶子贴图。

## Web 构建

安装与编辑器版本匹配的 **Web Build Support** 后，从 **File → Build Profiles** 切换至 **Web**，使用 **Build And Run** 启动浏览器，点击游戏画面接收键盘输入。

也可从本工作区的 `Webbox` 目录启动已生成的版本：

```sh
python3 -m http.server 8000 --bind 127.0.0.1 --directory Builds/Web
```

在浏览器打开 `http://localhost:8000`。


<details>
<summary>原创舞台背景生成记录</summary>

使用内置 ImageGen 生成背景，再缩放到 480 × 270 并以 Point 方式导入 Unity。

> Use case: stylized-concept
> Asset type: an original in-game background for a side-on 2D arcade fighting game. Produce a full-bleed WIDE 16:9 game environment plate in detailed late-1990s Japanese 16-bit/32-bit arcade pixel-art style. Scene: a rooftop night market above a coastal rail district just after sunset. Peeling dark teal industrial siding, rooftop machinery and mesh railings in the middle distance, low rich plum and cobalt skyline, a faint coral horizon, small warm amber windows and a few restrained cyan lights. Left periphery has a small closed market stall with a weathered awning, right periphery has a rooftop water tower and access hut. The mood is lived-in and urban, saturated palette accents offset by near-black blues.
>
> Composition: exactly side-on fighter camera with straight horizontal ground across the full width. Distant skyline in the upper third, market details near the left and right edges. Keep the central half behind the future fighters visually quiet and darker, from 35% to 75% image height, with an iron railing and no bright central sign. A substantial walkable flat rain-dark rooftop fighting platform fills the bottom 32%, with a straight horizontal back edge at 67% image height and muted warm reflected lights on concrete tiles. Keep the floor flush and walkable all the way across, allowing feet to be composited at 77.5% of image height. The screen itself will be placed over this intact image. Rich hand-placed 1–3-pixel clusters with selective dithering and crisp stair-stepped edges, visual palette of a polished 1990s arcade stage at native ~480 × 270. No JPEG-like softness or modern filters. No characters, spectators, silhouettes of people, weapons, readable text, logos, UI, health bars, borders, or existing game assets.

</details>
