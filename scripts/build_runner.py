# -*- coding: utf-8 -*-
"""
构建执行引擎
- 按深度优先顺序遍历任务树
- 每个叶子写入独立日志文件到 build_logs/ (每次覆写)
- 通过回调通知 GUI 状态变化
"""
import re
import threading
from collections.abc import Callable
from datetime import datetime
from pathlib import Path

from tasks.base import TaskNode, TaskStatus
from tasks.common import set_log_file, info

BUILD_LOGS_DIR = Path(__file__).resolve().parent.parent / "build_logs"


class BuildRunner:
    """构建执行引擎。"""

    def __init__(self):
        self.root: TaskNode | None = None
        self._running = False
        self._thread: threading.Thread | None = None

        self.on_status_change: Callable[[TaskNode], None] | None = None
        self.on_build_start: Callable[[], None] | None = None
        self.on_build_done: Callable[[bool], None] | None = None

    def set_tree(self, root: TaskNode):
        self.root = root

    @property
    def running(self) -> bool:
        return self._running

    def start(self):
        if self._running:
            return
        self._running = True
        BUILD_LOGS_DIR.mkdir(parents=True, exist_ok=True)
        if self.on_build_start:
            self.on_build_start()
        self._thread = threading.Thread(target=self._run_all, daemon=True)
        self._thread.start()

    def stop(self):
        self._running = False

    def _run_all(self):
        if not self.root:
            self._running = False
            return
        self.root.reset()
        all_ok = self._run_node(self.root)
        if self.on_build_done:
            self.on_build_done(all_ok)
        self._running = False
        set_log_file(None)

    def _run_node(self, node: TaskNode) -> bool:
        if not self._running:
            node.status = TaskStatus.SKIPPED
            self._notify(node)
            return False
        if not node.enabled:
            node.status = TaskStatus.DISABLED
            self._notify(node)
            return True

        if node.is_leaf:
            log_path = _log_path_for(node)
            node.log_path = log_path
            set_log_file(log_path)
            info(f"=== {node.full_path} ===")
            info(f"Started: {datetime.now().isoformat()}")
            status = node.run()
            info(f"Finished: {node.status.value} at {datetime.now().isoformat()}")
            set_log_file(None)
            self._notify(node)
            return status in (TaskStatus.SUCCESS, TaskStatus.DISABLED,
                              TaskStatus.SKIPPED)

        all_ok = True
        fail_occurred = False
        for child in node.children:
            if fail_occurred:
                if child.enabled:
                    child.status = TaskStatus.SKIPPED
                    self._notify(child)
                continue
            ok = self._run_node(child)
            if not ok:
                all_ok = False
                fail_occurred = True

        if not node.enabled:
            node.status = TaskStatus.DISABLED
        elif fail_occurred:
            node.status = TaskStatus.FAILED
        elif all(c.status in (TaskStatus.DISABLED, TaskStatus.SKIPPED)
                 for c in node.children):
            node.status = TaskStatus.DISABLED
        else:
            node.status = TaskStatus.SUCCESS
        self._notify(node)
        return all_ok

    def _notify(self, node: TaskNode):
        if self.on_status_change:
            self.on_status_change(node)


# ════════════════════════════════════════════
#  日志路径 & 状态恢复
# ════════════════════════════════════════════

def _safe_name(node: TaskNode) -> str:
    """安全的文件名, 基于 full_path。"""
    s = node.full_path.replace("/", "_").replace("\\", "_")
    s = s.replace(":", "").replace(" ", "_")
    if len(s) > 120:
        s = s[:120]
    return f"{s}.log"


def _log_path_for(node: TaskNode) -> Path:
    return BUILD_LOGS_DIR / _safe_name(node)


def restore_status_from_logs(root: TaskNode):
    """从 build_logs/ 中的已有日志恢复各叶子的状态和时间。
    遍历所有叶子, 如果日志文件存在, 从其内容解析状态和起止时间。
    """
    for leaf in root.all_leaves():
        log_path = _log_path_for(leaf)
        if not log_path.exists():
            continue
        leaf.log_path = log_path

        # 从 mtime 获取大致时间
        mtime = datetime.fromtimestamp(log_path.stat().st_mtime)

        try:
            content = log_path.read_text(encoding="utf-8", errors="replace")
            # 解析状态
            if "Finished: success" in content:
                leaf.status = TaskStatus.SUCCESS
            elif "Finished: failed" in content:
                leaf.status = TaskStatus.FAILED
            elif "Finished: skipped" in content:
                leaf.status = TaskStatus.SKIPPED
            else:
                leaf.status = TaskStatus.SUCCESS  # 默认算成功

            # 解析开始时间
            m = re.search(r"Started:\s*(.+)", content)
            if m:
                try:
                    leaf.start_time = datetime.fromisoformat(m.group(1).strip())
                except ValueError:
                    leaf.start_time = mtime
            else:
                leaf.start_time = mtime

            # 解析结束时间
            m = re.search(r"Finished:\s*\w+\s*at\s*(.+)", content)
            if m:
                try:
                    leaf.end_time = datetime.fromisoformat(m.group(1).strip())
                except ValueError:
                    leaf.end_time = mtime
            else:
                leaf.end_time = mtime

        except Exception:
            leaf.start_time = mtime
            leaf.end_time = mtime
            leaf.status = TaskStatus.SUCCESS

    def _notify(self, node: TaskNode):
        if self.on_status_change:
            self.on_status_change(node)
