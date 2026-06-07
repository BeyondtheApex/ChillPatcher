# -*- coding: utf-8 -*-
"""
CI 构建入口 (无 GUI)
用法:
    python ci_build.py player          # 构建 player + installer
    python ci_build.py installer       # 仅打包 installer
    python ci_build.py all             # 全部构建

环境变量:
    GITHUB_STEP_SUMMARY  — 如果存在, 写入 GitHub Actions Job Summary
    GITHUB_ACTIONS       — 如果为 true, 输出 ::group:: / ::endgroup:: 折叠
"""
from __future__ import annotations
import os
import sys
import time
from datetime import datetime, timezone
from pathlib import Path

# 确保 scripts/ 在 sys.path
SCRIPT_DIR = Path(__file__).resolve().parent
sys.path.insert(0, str(SCRIPT_DIR))

import build_config as C
from build_tree import build_tree
from build_runner import BuildRunner, BUILD_LOGS_DIR
from tasks.base import TaskNode, TaskStatus
from tasks.common import set_log_file, info


# ══════════════════════════════════════════════
#  摘要生成
# ══════════════════════════════════════════════

STATUS_ICON = {
    TaskStatus.SUCCESS:  "✅",
    TaskStatus.FAILED:   "❌",
    TaskStatus.SKIPPED:  "⏭️",
    TaskStatus.DISABLED: "⚫",
    TaskStatus.RUNNING:  "🔄",
    TaskStatus.PENDING:  "⏳",
}


def _generate_summary(roots: list[TaskNode], overall_ok: bool, start_ts: datetime):
    """生成 Markdown 树状构建摘要, 写入 GITHUB_STEP_SUMMARY。"""
    lines = []
    duration = (datetime.now(timezone.utc) - start_ts).total_seconds()
    mins = int(duration // 60)
    secs = int(duration % 60)

    icon = "✅" if overall_ok else "❌"
    lines.append(f"## {icon} OmniMixPlayer Build {'Passed' if overall_ok else 'Failed'}")
    lines.append("")

    # ── 收集所有叶子节点 ──
    all_leaves: list[TaskNode] = []

    def _collect_leaves(node: TaskNode):
        if node.is_leaf and node._run_fn:
            all_leaves.append(node)
        for child in node.children:
            _collect_leaves(child)

    for root in roots:
        _collect_leaves(root)

    passed = sum(1 for n in all_leaves if n.status == TaskStatus.SUCCESS)
    failed = sum(1 for n in all_leaves if n.status == TaskStatus.FAILED)
    skipped = sum(1 for n in all_leaves if n.status == TaskStatus.SKIPPED)
    disabled = sum(1 for n in all_leaves if n.status == TaskStatus.DISABLED)
    total = len(all_leaves)

    # ── 统计 ──
    lines.append("| Metrics | Value |")
    lines.append("|---|---|")
    lines.append(f"| ✅ Passed | {passed} / {total} |")
    if failed:   lines.append(f"| ❌ Failed | {failed} |")
    if skipped:  lines.append(f"| ⏭️ Skipped | {skipped} |")
    if disabled: lines.append(f"| ⚫ Disabled | {disabled} |")
    lines.append(f"| ⏱️ Total Duration | {mins}m {secs}s |")
    lines.append("")

    # ── 构建详细层级看板 ──
    lines.append("### 🛠️ Build Stages & Components")
    lines.append("")
    lines.append("| Component / Task | Status | Duration | Log File |")
    lines.append("|:---|---:|:---:|---|")

    def _walk_all_nodes(node: TaskNode, depth: int = 0, parent_full_path: str = ""):
        """递归遍历整棵树, 所有执行了实际任务的节点都打入表格。"""
        indent_html = "&nbsp;" * (depth * 4)
        icon_str = STATUS_ICON.get(node.status, "❓")

        # 计算耗时
        dur = "-"
        if node.start_time and node.end_time:
            d = (node.end_time - node.start_time).total_seconds()
            dur = f"{d:.1f}s"

        # 日志文件 (只有叶子有)
        log_display = "-"
        if node.log_path:
            log_display = f"`{node.log_path.name}`"

        # 叶子节点 (有 _run_fn) —— 总是显示
        # 中间组 —— 仅在 depth < 3 且非空时显示, 用于展示树结构
        is_exec_leaf = node.is_leaf and node._run_fn

        if is_exec_leaf or (node.children and depth < 3 and any(
            c.is_leaf and c._run_fn for c in node.children
        )):
            # Align right for status & duration columns
            lines.append(f"| {indent_html}{node.name} | {icon_str} | {dur} | {log_display} |")
        elif node.children:
            # 中间组, 继续递归但不打表 (树结构靠 depth<3 的组来体现)
            pass

        for child in node.children:
            _walk_all_nodes(child, depth + 1)

    for root in roots:
        _walk_all_nodes(root, depth=0)

    lines.append("")

    # ── 失败快捷排查 ──
    failed_nodes = [n for n in all_leaves if n.status == TaskStatus.FAILED]
    if failed_nodes:
        lines.append("### ❌ Failed Tasks Quick Review")
        lines.append("")
        for n in failed_nodes:
            dur = ""
            if n.start_time and n.end_time:
                d = (n.end_time - n.start_time).total_seconds()
                dur = f" ({d:.1f}s)"
            log_name = n.log_path.name if n.log_path else "N/A"
            lines.append(f"- **{n.full_path}**{dur} → `{log_name}`")
        lines.append("")

    lines.append("---")
    lines.append("💡 Full logs in Artifacts → `build-logs`  |  Or expand `Display build logs in console` step below.")

    summary = "\n".join(lines)

    # 写入 GitHub Actions Job Summary
    summary_file = os.environ.get("GITHUB_STEP_SUMMARY", "")
    if summary_file:
        Path(summary_file).write_text(summary, encoding="utf-8")

    # 同时打印到 stdout (本地调试用)
    print("\n" + "=" * 60)
    print(summary)
    print("=" * 60)


# ══════════════════════════════════════════════
#  CI 回调
# ══════════════════════════════════════════════

def _ci_on_status(node: TaskNode):
    """CI 环境下的状态回调: 折叠输出。"""
    if node.is_leaf and node.status == TaskStatus.RUNNING:
        if os.environ.get("GITHUB_ACTIONS") == "true":
            print(f"::group::{node.full_path}")
        else:
            print(f"\n── {node.full_path} ──")

    if node.is_leaf and node.status in (TaskStatus.SUCCESS, TaskStatus.FAILED, TaskStatus.SKIPPED):
        icon = STATUS_ICON.get(node.status, "?")
        print(f"  {icon} {node.name}: {node.status.value}")
        if os.environ.get("GITHUB_ACTIONS") == "true":
            print("::endgroup::")


# ══════════════════════════════════════════════
#  入口
# ══════════════════════════════════════════════

def main():
    C.setup_toolchain()

    mode = sys.argv[1] if len(sys.argv) > 1 else "player"
    full = "--full" in sys.argv
    skip_flutter = "--skip-flutter" in sys.argv

    print(f"Mode: {mode}  Full: {full}  SkipFlutter: {skip_flutter}")

    start_ts = datetime.now(timezone.utc)
    BUILD_LOGS_DIR.mkdir(parents=True, exist_ok=True)

    roots = build_tree(mode, full, skip_flutter)

    runner = BuildRunner()
    runner.on_status_change = _ci_on_status

    # 结果收集
    result = {"ok": True}

    def on_done(ok: bool):
        result["ok"] = ok

    runner.on_build_done = on_done

    # 包装所有根节点到一个虚拟根
    virtual_root = TaskNode(f"Build ({mode})")
    for r in roots:
        virtual_root.add_child(r)

    runner.set_tree(virtual_root)
    runner.start()

    # 等待完成 (简单轮询, 因为 BuildRunner 用 daemon 线程)
    while runner.running:
        time.sleep(0.5)

    # 生成摘要
    _generate_summary(roots, result["ok"], start_ts)

    sys.exit(0 if result["ok"] else 1)


if __name__ == "__main__":
    main()
