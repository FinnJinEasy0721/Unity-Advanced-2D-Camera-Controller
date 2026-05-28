# CameraController — Unity 2D 智能摄像机跟随系统

[![Unity](https://img.shields.io/badge/Unity-2020.3%2B-black?logo=unity)](https://unity.com)
[![License: MIT](https://img.shields.io/badge/License-MIT-green.svg)](LICENSE)
[![Language](https://img.shields.io/badge/Language-C%23-blue.svg)](CameraController.cs)

一个面向 2D 横版平台游戏的智能摄像机跟随系统，支持 **Look-ahead 预判偏移**、**状态机驱动垂直偏移**、**程序化屏幕震动**，基于独立轴 `SmoothDamp` 实现低延迟、无过冲的平滑跟随。

---

## ✨ 核心特性

| 特性 | 说明 |
|------|------|
| **双轴独立 SmoothDamp** | X / Y 轴分别维护速度状态量，水平跟随灵敏，垂直过渡柔和 |
| **Look-ahead 预判偏移** | 根据玩家水平输入向量实时移动视野中心，扩大前方可视域 |
| **状态机驱动垂直镜头** | 对接 `PlayerStateMachine`，Jump / Fall / Slide 各阶段独立镜头策略 |
| **程序化屏幕震动** | 协程驱动的 `CameraShake` API，三参数控制震动次数、频率、幅度 |
| **吸附阈值消抖** | 极近距离直接吸附并清零速度，消除 `SmoothDamp` 收敛尾段微抖动 |
| **全参数 Inspector 暴露** | 所有调节参数通过 `[Header]` / `[Tooltip]` 分组标注，无需修改源码 |
| **懒加载组件引用** | 运行时自动获取 `PlayerController` 与 `PlayerStateMachine` 引用，无需手动绑定 |

---

## 📦 安装

将 `CameraController.cs` 复制到你的 Unity 项目 `Assets` 目录中即可，无第三方依赖。

> **兼容性**：Unity 2020.3 LTS 及以上版本，使用标准 `UnityEngine` 命名空间。

---

## 🚀 快速开始

### 1. 挂载组件

将 `CameraController` 脚本挂载到场景中的 **Main Camera** 对象上。

> 脚本在 `LateUpdate` 中执行，确保摄像机始终在物理模拟与动画更新之后采样目标位置，避免画面抖动。

### 2. 绑定跟随目标

在 Inspector 的 **跟随目标** 分组中，将玩家的 `Transform` 赋值给 `Target` 字段。

`PlayerController` 与 `PlayerStateMachine` 组件引用通过懒加载自动获取，无需额外手动绑定。

### 3. 接入玩家组件

确保玩家对象上挂有以下两个组件并满足接口约定：

```csharp
// PlayerController 需暴露以下公共方法
public float GetHorizontalInput() { ... }

// PlayerStateMachine 需暴露以下字段与枚举
public PlayerState currentState;
public enum PlayerState { ..., Jump, Fall, Slide, ... }
```

若场景中不存在 `PlayerStateMachine`，系统将以纯输入驱动模式运行，不会产生运行时错误。

### 4. 调整基础偏移

根据摄像机正交深度修改 `Offset` 的 Z 分量（默认值为 `-10`）。Y 分量默认为 `1`，使镜头略高于角色重心。

---

## ⚙️ 参数说明

### 跟随参数

| 参数 | 类型 | 默认值 | 说明 |
|------|------|--------|------|
| `smoothTimeX` | `float` | `0.06` | 水平轴 SmoothDamp 平滑时间，值越小跟随越灵敏 |
| `smoothTimeY` | `float` | `0.15` | 垂直轴平滑时间，略大于水平以使跳跃镜头更自然 |
| `maxSpeed` | `float` | `50` | SmoothDamp 最大速度上限，防止瞬移时镜头失控 |

### 水平 Look-ahead

| 参数 | 类型 | 默认值 | 说明 |
|------|------|--------|------|
| `lookAheadDistX` | `float` | `2.0` | 预判偏移最大距离（世界单位），控制视野前移幅度 |
| `lookAheadSpeed` | `float` | `12` | 偏移 MoveTowards 速率，匀速移动无过冲 |

> 滑行状态下，预判偏移改由角色朝向（`localScale.x` 符号）驱动，而非实时输入，避免滑行中无按键时偏移归零。

### 垂直镜头偏移

| 参数 | 类型 | 默认值 | 说明 |
|------|------|--------|------|
| `jumpLookUpDist` | `float` | `2.5` | Jump 状态下摄像机向上偏移量 |
| `fallLookCancelSpeed` | `float` | `4×` | Fall 状态下偏移归零速率倍率，加速回收以跟随下落 |
| `slideLookDownDist` | `float` | `1.5` | Slide 状态下摄像机向下偏移量 |
| `verticalLookSpeed` | `float` | `12` | 垂直偏移的 MoveTowards 基础速率 |

### 基础偏移

| 参数 | 类型 | 默认值 | 说明 |
|------|------|--------|------|
| `offset` | `Vector3` | `(0, 1, -10)` | 相对目标的静态偏移，Z 值须与正交摄像机深度一致 |

---

## 📷 屏幕震动 API

```csharp
/// <summary>
/// 触发程序化屏幕震动
/// </summary>
/// <param name="Count">震动次数（每次包含一次偏移 + 一次归零）</param>
/// <param name="Speed">震动频率（次/秒），控制单次震动持续时间</param>
/// <param name="Strength">震动幅度（世界单位），基于圆形随机采样</param>
public void CameraShake(int Count, float Speed, float Strength)
```

**调用示例：**

```csharp
// 受击反馈：5次轻微震动
Camera.main.GetComponent<CameraController>().CameraShake(5, 20f, 0.15f);

// 爆炸冲击：3次强震动
Camera.main.GetComponent<CameraController>().CameraShake(3, 8f, 0.5f);
```

> `CameraShake` 内部会调用 `StopAllCoroutines()`，多次连续触发时会中断前一次震动，以最新一次为准。若需要叠加震动，可自行修改为独立协程管理。

---

## 🔧 接口依赖

本系统通过 `GetComponent` 懒加载以下组件，如需集成请确保接口签名一致：

```csharp
// 必须：提供水平输入量（范围 -1 ~ 1）
float PlayerController.GetHorizontalInput()

// 可选：提供玩家当前状态
PlayerStateMachine.PlayerState PlayerStateMachine.currentState
// 需包含以下枚举值：
PlayerStateMachine.PlayerState.Jump
PlayerStateMachine.PlayerState.Fall
PlayerStateMachine.PlayerState.Slide
```

---

## 🏗️ 工作原理

```
LateUpdate()
├── 懒加载 PlayerController / PlayerStateMachine
├── 计算 lookAheadOffsetX
│   ├── Slide 状态 → 用 localScale.x 符号确定朝向
│   └── 其他状态  → 用 GetHorizontalInput() 驱动
├── 计算 verticalLookOffset
│   ├── Jump  → 目标值 = +jumpLookUpDist
│   ├── Fall  → 目标值 = 0，速率 × fallLookCancelSpeed
│   └── Slide → 目标值 = -slideLookDownDist
├── 合成 targetPos = target.position + offset + 各偏移
├── SmoothDamp X 轴（smoothTimeX）
├── SmoothDamp Y 轴（smoothTimeY）
├── 吸附阈值检测（snapThreshold = 0.005）
└── transform.position = 平滑结果 + shakeOffset
```

---

## 📄 License

[MIT](LICENSE) © 2024
