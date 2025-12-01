#!/usr/bin/env python3
"""
copilotmd_skeleton.py - 从 .copilotmd 文件提取骨架结构

功能：
- 解析 Copilot 对话日志文件
- 识别 Tool 结果块并省略其具体内容
- 截断 Assistant 工具调用中的长字符串参数
- 保留对话结构，便于快速浏览

Tool 结果块识别规则：
- 以 `🛠️ toolu_` 开头的行是 Tool 返回结果的标识
- 与 Assistant 调用格式 `🛠️ xxx (toolu_` 区分

用法：
    python copilotmd_skeleton.py input.copilotmd [-o output.copilotmd] [--stats]
"""

import argparse
import json
import re
import sys
from pathlib import Path

# 字符串截断配置
STRING_TRUNCATE_THRESHOLD = 100  # 超过此长度的字符串将被截断
STRING_KEEP_HEAD = 40            # 保留开头的字符数
STRING_KEEP_TAIL = 40            # 保留结尾的字符数


def truncate_string(s: str, stats: dict) -> str:
    """截断单个长字符串"""
    if len(s) > STRING_TRUNCATE_THRESHOLD:
        omitted = len(s) - STRING_KEEP_HEAD - STRING_KEEP_TAIL
        stats['strings_truncated'] = stats.get('strings_truncated', 0) + 1
        stats['chars_saved'] = stats.get('chars_saved', 0) + omitted
        return f"{s[:STRING_KEEP_HEAD]}... ({omitted} chars omitted) ...{s[-STRING_KEEP_TAIL:]}"
    return s


def truncate_long_strings(obj, stats: dict):
    """
    递归遍历 JSON 对象，截断长字符串
    
    Args:
        obj: JSON 对象（dict, list, 或基本类型）
        stats: 统计信息字典，用于记录截断次数
        
    Returns:
        处理后的对象
    """
    if isinstance(obj, dict):
        return {k: truncate_long_strings(v, stats) for k, v in obj.items()}
    elif isinstance(obj, list):
        return [truncate_long_strings(item, stats) for item in obj]
    elif isinstance(obj, str):
        return truncate_string(obj, stats)
    else:
        return obj


def truncate_json_strings_regex(json_str: str, stats: dict) -> str:
    """
    使用正则表达式直接在 JSON 字符串中截断长字符串值
    用于处理包含控制字符的无效 JSON
    """
    def replace_long_string(match):
        # match.group(1) 是键名，match.group(2) 是值
        key = match.group(1)
        value = match.group(2)
        truncated = truncate_string(value, stats)
        # 需要转义特殊字符以便重新嵌入 JSON
        truncated = truncated.replace('\\', '\\\\').replace('"', '\\"')
        return f'"{key}": "{truncated}"'
    
    # 匹配 "key": "value" 模式
    # 值可以包含转义序列 (\\.) 或非引号非反斜杠字符
    pattern = r'"(\w+)":\s*"((?:[^"\\]|\\.)*)"'
    return re.sub(pattern, replace_long_string, json_str, flags=re.DOTALL)


def process_tool_call_json(json_str: str, stats: dict) -> str:
    """
    处理工具调用的 JSON 参数，截断长字符串
    
    Args:
        json_str: JSON 字符串
        stats: 统计信息字典
        
    Returns:
        处理后的 JSON 字符串（保持格式化）
    """
    try:
        obj = json.loads(json_str)
        processed = truncate_long_strings(obj, stats)
        return json.dumps(processed, ensure_ascii=False, indent=2)
    except json.JSONDecodeError:
        # JSON 解析失败（可能包含控制字符），使用正则表达式直接处理
        return truncate_json_strings_regex(json_str, stats)


def is_tool_result_marker(line: str) -> bool:
    """
    判断是否是 Tool 结果块的起始标识行
    
    Tool 结果格式: 🛠️ toolu_vrtx_xxx
    Assistant 调用格式: 🛠️ read_file (toolu_vrtx_xxx) { ... }
    
    区分方式：Tool 结果直接以 `🛠️ toolu_` 开头
    """
    stripped = line.strip()
    # 匹配 Tool 结果标识：🛠️ 后直接跟 toolu_
    return bool(re.match(r'^🛠️\s+toolu_', stripped))


def is_tool_call_start(line: str) -> bool:
    """
    判断是否是 Assistant 工具调用块的起始行
    
    格式: 🛠️ tool_name (toolu_xxx) {
    """
    stripped = line.strip()
    return bool(re.match(r'^🛠️\s+\w+\s+\(toolu_', stripped))


def extract_skeleton(content: str) -> tuple[str, dict]:
    """
    从 .copilotmd 内容中提取骨架结构
    
    Args:
        content: 原始文件内容
        
    Returns:
        tuple: (骨架内容, 统计信息字典)
    """
    lines = content.splitlines(keepends=True)
    result_lines = []
    stats = {
        'original_lines': len(lines),
        'kept_lines': 0,
        'omitted_lines': 0,
        'tool_blocks_processed': 0,
        'metadata_tools_omitted': False
    }
    
    i = 0
    in_tool_result_block = False
    in_metadata_tools = False  # 是否在 Metadata 的 tools 列表中
    tools_bracket_depth = 0    # 跟踪 tools 列表的括号深度
    omitted_count = 0
    
    while i < len(lines):
        line = lines[i]
        
        # 检测 Metadata 中的 tools 列表开始
        if not in_metadata_tools and line.strip().startswith('tools') and ': [' in line:
            result_lines.append('tools            : [ ... tools list omitted ... ]\n')
            stats['kept_lines'] += 1
            in_metadata_tools = True
            tools_bracket_depth = 1  # 跟踪括号深度
            stats['metadata_tools_omitted'] = True
            i += 1
            continue
        
        # 在 Metadata tools 列表中，通过括号匹配找结束
        if in_metadata_tools:
            # 计算这一行的括号变化
            tools_bracket_depth += line.count('[') - line.count(']')
            if tools_bracket_depth <= 0:
                in_metadata_tools = False
            stats['omitted_lines'] += 1
            i += 1
            continue
        
        if in_tool_result_block:
            # 在 Tool 结果块内部，查找代码块结束标记
            if line.strip() == '~~~':
                # 找到结束标记，插入省略提示并保留结束标记
                if omitted_count > 0:
                    result_lines.append(f'... ({omitted_count} lines omitted)\n')
                    stats['kept_lines'] += 1
                result_lines.append(line)
                stats['kept_lines'] += 1
                in_tool_result_block = False
                omitted_count = 0
            else:
                # 跳过内容行
                stats['omitted_lines'] += 1
                omitted_count += 1
        elif is_tool_result_marker(line):
            # 发现 Tool 结果块起始
            result_lines.append(line)
            stats['kept_lines'] += 1
            stats['tool_blocks_processed'] += 1
            in_tool_result_block = True
            omitted_count = 0
        elif is_tool_call_start(line):
            # 发现 Assistant 工具调用块，收集 JSON 并处理
            # 起始行格式: 🛠️ tool_name (toolu_xxx) {
            # 需要把起始行的 { 和后续内容合并成完整 JSON
            
            # 提取起始行中 { 之前的部分作为标识
            brace_pos = line.find('{')
            if brace_pos == -1:
                # 没有 {，原样保留
                result_lines.append(line)
                stats['kept_lines'] += 1
                i += 1
                continue
            
            header = line[:brace_pos].rstrip()  # 🛠️ tool_name (toolu_xxx)
            i += 1
            
            # 收集 JSON 内容直到括号平衡
            json_lines = ['{']  # 从起始行取的 {
            brace_depth = 1
            while i < len(lines) and brace_depth > 0:
                json_line = lines[i]
                json_lines.append(json_line.rstrip('\n'))
                brace_depth += json_line.count('{') - json_line.count('}')
                i += 1
            
            # 处理收集到的 JSON
            json_str = '\n'.join(json_lines)
            processed_json = process_tool_call_json(json_str, stats)
            
            # 输出: 标识行 + 处理后的 JSON
            result_lines.append(header + ' ')
            result_lines.append(processed_json + '\n')
            stats['kept_lines'] += 2
            continue  # 已经在内部循环中处理了 i
        else:
            # 普通行，直接保留
            result_lines.append(line)
            stats['kept_lines'] += 1
        
        i += 1
    
    # 处理文件末尾未闭合的情况
    if in_tool_result_block and omitted_count > 0:
        result_lines.append(f'... ({omitted_count} lines omitted)\n')
        stats['kept_lines'] += 1
    
    return ''.join(result_lines), stats


def print_stats(stats: dict, input_path: str, output_path: str) -> None:
    """打印压缩统计信息"""
    original = stats['original_lines']
    kept = stats['kept_lines']
    omitted = stats['omitted_lines']
    strings_truncated = stats.get('strings_truncated', 0)
    chars_saved = stats.get('chars_saved', 0)
    
    if original > 0:
        compression_ratio = (1 - kept / original) * 100
    else:
        compression_ratio = 0
    
    print("\n📊 压缩统计:")
    print(f"   输入文件: {input_path}")
    print(f"   输出文件: {output_path}")
    print("   ─────────────────────────────")
    print(f"   原始行数: {original:,}")
    print(f"   保留行数: {kept:,}")
    print(f"   省略行数: {omitted:,}")
    print(f"   压缩比例: {compression_ratio:.1f}%")
    print(f"   处理的 Tool 块: {stats['tool_blocks_processed']}")
    if strings_truncated > 0:
        print(f"   截断的长字符串: {strings_truncated}")
        print(f"   节省的字符数: {chars_saved:,}")


def main():
    parser = argparse.ArgumentParser(
        description='从 .copilotmd 文件提取骨架结构，省略 Tool 结果的具体内容',
        formatter_class=argparse.RawDescriptionHelpFormatter,
        epilog="""
示例:
  python copilotmd_skeleton.py chat.copilotmd
      -> 输出到 chat.skeleton.copilotmd
  
  python copilotmd_skeleton.py chat.copilotmd -o summary.md
      -> 输出到 summary.md
  
  python copilotmd_skeleton.py chat.copilotmd --stats
      -> 输出并显示统计信息
        """
    )
    
    parser.add_argument(
        'input',
        type=str,
        help='输入的 .copilotmd 文件路径'
    )
    
    parser.add_argument(
        '-o', '--output',
        type=str,
        default=None,
        help='输出文件路径（默认: input.skeleton.copilotmd）'
    )
    
    parser.add_argument(
        '--stats',
        action='store_true',
        help='显示压缩统计信息'
    )
    
    args = parser.parse_args()
    
    # 验证输入文件
    input_path = Path(args.input)
    if not input_path.exists():
        print(f"❌ 错误: 输入文件不存在: {input_path}", file=sys.stderr)
        sys.exit(1)
    
    # 确定输出路径
    if args.output:
        output_path = Path(args.output)
    else:
        # 默认: input.copilotmd -> input.skeleton.copilotmd
        stem = input_path.stem
        if stem.endswith('.copilot'):
            # 处理 xxx.copilot.md 的情况
            stem = stem[:-7] + '.skeleton.copilot'
        else:
            stem = stem + '.skeleton'
        output_path = input_path.with_name(stem + input_path.suffix)
    
    # 读取输入文件
    try:
        content = input_path.read_text(encoding='utf-8')
    except Exception as e:
        print(f"❌ 错误: 无法读取输入文件: {e}", file=sys.stderr)
        sys.exit(1)
    
    # 提取骨架
    skeleton, stats = extract_skeleton(content)
    
    # 写入输出文件
    try:
        output_path.parent.mkdir(parents=True, exist_ok=True)
        output_path.write_text(skeleton, encoding='utf-8')
        print(f"✅ 骨架已保存到: {output_path}")
    except Exception as e:
        print(f"❌ 错误: 无法写入输出文件: {e}", file=sys.stderr)
        sys.exit(1)
    
    # 显示统计信息
    if args.stats:
        print_stats(stats, str(input_path), str(output_path))


if __name__ == '__main__':
    main()
