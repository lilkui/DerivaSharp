# DerivaSharp

[English](README.md) | [简体中文](README.zh-CN.md)

![License: MIT](https://img.shields.io/badge/License-MIT-blue.svg)
![Stage: Experimental](https://img.shields.io/badge/stage-experimental-orange)

DerivaSharp 是一个现代化、注重性能的 C# 金融衍生品定价库，提供了丰富的产品支持与多种高级定价算法。

> [!NOTE]
> 该项目目前处于实验阶段，API 可能频繁发生破坏性变更，稳定性尚未保证。

## 快速开始

### 前置要求

- [.NET 10 SDK](https://dotnet.microsoft.com/download/dotnet/10.0)

### 基础用法

下面示例演示如何使用解析解引擎为欧式看涨期权定价：

```csharp
using DerivaSharp.Instruments;
using DerivaSharp.Models;
using DerivaSharp.PricingEngines;
using DerivaSharp.Time;

DateOnly valuationDate = new(2025, 1, 6);
DateOnly expirationDate = valuationDate.AddYears(1);

// 1. 定义产品
EuropeanOption option = new(
    OptionType.Call,
    100.0,
    valuationDate,
    expirationDate);

// 2. 设置模型参数
BsmModelParameters modelParameters = new(0.3, 0.04, 0.01);

// 3. 创建定价上下文
PricingContext<BsmModelParameters> context = new(
    modelParameters,
    assetPrice: 100.0,
    valuationDate,
    NullCalendar.Shared);

// 4. 计算价格与 Greeks
AnalyticEuropeanEngine engine = new();
double price = engine.Value(option, context);
double delta = engine.Delta(option, context);

Console.WriteLine($"Price: {price:F4}");
Console.WriteLine($"Delta: {delta:F4}");
```

### 交互式 Notebook

可查看 [notebooks/](notebooks/) 目录中的交互示例。这些 Notebook 演示了如何通过 [Python.NET](https://pythonnet.github.io/) 在 Jupyter 中使用本库。

在使用 Notebook 前，请先为目标平台构建库以生成所需二进制文件。例如在 Windows 上：

```bash
dotnet publish -c Release -r win-x64
```

## 支持的产品与定价算法

| 产品类别 | 子类别 | 定价算法 |
| :--- | :--- | :--- |
| **香草期权** | 欧式、美式 | 解析法、有限差分、蒙特卡洛、二叉树、数值积分 |
| **障碍期权** | 标准障碍 | 解析法、有限差分 |
| **数字期权** | 现金或无、资产或无、二元障碍 | 解析法、有限差分、数值积分 |
| **亚式期权** | 几何平均、算术平均 | 解析法 |
| **自动赎回票据** | 雪球、凤凰、FCN、DCN | 有限差分、蒙特卡洛 |
| **累计期权** | KODA | 有限差分、蒙特卡洛 |

## 性能基准

**测试环境：**

- **CPU**: AMD Ryzen 9 5900X @ 3.70GHz
- **GPU**: NVIDIA GeForce RTX 3090
- **OS**: Windows 11 (25H2)
- **Runtime**: .NET 10.0.2

| 产品 | 定价算法 | 参数 | 耗时（JIT） | 耗时（AOT） |
| :--- | :--- | :--- | ---: | ---: |
| **欧式香草** | 解析法 | - | 28.3 ns | 41.2 ns |
| | 有限差分 | 1000×1000 网格（CN） | 11.15 ms | 11.25 ms |
| | 蒙特卡洛 | 500000 条路径 | 6.16 ms | 6.94 ms |
| | 二叉树 | 1000 步 | 444 μs | 549 μs |
| | 数值积分 | - | 335 ns | 462 ns |
| **美式香草** | Bjerksund-Stensland | - | 5.41 μs | 6.47 μs |
| | 有限差分 | 1000×1000 网格（CN） | 12.24 ms | 12.54 ms |
| | 蒙特卡洛 | 100000 条路径，250 步 | 913 ms | 968 ms |
| | 蒙特卡洛 | 100000 条路径，250 步，GPU 加速 | 628 ms | 628 ms |
| **雪球** | 有限差分 | 1000×1000 网格（CN） | 25.96 ms | 27.07 ms |
| | 蒙特卡洛 | 500000 条路径 | 1.78 s | 2.00 s |
| | 蒙特卡洛 | 500000 条路径，GPU 加速 | 40.86 ms | 40.73 ms |

*CN = Crank-Nicolson 格式*
