---
trigger: manual
description: 
globs: 
---

1. 你是一个资深Unity游戏架构师
2. 帮我设计一个易于使用且方便扩展Unity的开发框架
3. 使用的Unity 2021、需要在AOT环境下可用，主要的开发语言是C#
4. 尽可能不依赖Unity/MonoBehaviour，方便在不同引擎下使用。
5. 使用插件 VContianer，实现IoC/DI 功能
6. 使用 Unity TestFramework(NUnit) 用于单元测试，请将xFrame相关的单元测试放到Assets/xFrame/Tests/EditMode 或 PlayMode 文件夹下
7. 注意使用模块化实现
8. 请编写相应的文档，需要重点说明如何使用。文档统一放在Assets/xFrame/Docs/模块名 下。