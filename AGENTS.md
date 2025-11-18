## 跨会话记忆文档
本文档(`./AGENTS.md`)会伴随每个 user 消息注入上下文，是跨会话的外部记忆。完成一个任务、制定或调整计划时务必更新本文件，避免记忆偏差。

## 已知的工具问题
- 需要要删除请用改名替代，因为环境会拦截删除文件操作。
- 不要使用'insert_edit_into_file'工具，经常产生难以补救的错误结果。

## 用户语言
请主要用简体中文与用户交流，对于术语/标识符等实体名称则优先用原始语言。

## 项目概览
**总体目标**是将位于“./ts”目录内的VS Code的无GUI编辑器核心移植为C#类库(dotnet 9.0 + xUnit)。核心目标是“./ts/src/vs/editor/common/model/pieceTreeTextBuffer”, 如果移植顺利后续可以围绕pieceTreeTextBuffer再移植diff/edit/cursor等其他部分。

**用途背景**是为工作在Agent系统中的LLM创建一种DocUI，类似TUI但是不是渲染到2D终端而是渲染为Markdown文本，UI元素被渲染为Markdown元素。渲染出的Markdown会被通过上下文工程注入LLM Context中。可以想象为“把LLM Context作为向LLM展示信息的屏幕”。这需要高质量的Text建模、编辑、比较、查找、装饰功能。举例说明这里“装饰”的含义，例如我们要创建一个TextBox Widget来向LLM呈现可编辑文本，把原始文本和虚拟行号渲染为Markdown代码围栏，把光标/选区这些overlay元素渲染为插入文本中的Mark，并在代码围栏外用图例注解插入的光标/选区起点/终点Mark。像这些虚拟行号、光标/选区Mark，就是前面所说的“装饰”。后续有望用这条DocUI为LLM Agent打造更加LLM Native & Friendly的编程IDE。