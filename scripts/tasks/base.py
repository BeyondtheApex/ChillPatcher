# -*- coding: utf-8 -*-
"""
构建任务基类 - TaskNode
树形任务节点, 支持 enable/disable 和状态追踪
"""
from __future__ import annotations
from enum import Enum
from datetime import datetime
from pathlib import Path
from typing import Callable


class TaskStatus(Enum):
    PENDING = "pending"       # 等待执行
    RUNNING = "running"       # 执行中
    SUCCESS = "success"       # 成功
    FAILED = "failed"         # 失败
    SKIPPED = "skipped"       # 跳过(依赖失败)
    DISABLED = "disabled"     # 用户禁用了


class TaskNode:
    """树形任务节点。每个节点可以是一组子任务(Group)或一个叶子任务(Leaf)。"""

    def __init__(self, name: str, description: str = "",
                 parent: TaskNode | None = None):
        self.name = name
        self.description = description
        self.parent = parent
        self.children: list[TaskNode] = []
        self.status = TaskStatus.PENDING
        self._enabled = True
        self._run_fn: Callable | None = None  # 叶子节点的执行函数
        self.output_message: str = ""          # 执行输出摘要
        self.start_time: datetime | None = None
        self.end_time: datetime | None = None
        self.log_path: Path | None = None       # 构建日志文件路径

    @property
    def enabled(self) -> bool:
        return self._enabled

    @enabled.setter
    def enabled(self, val: bool):
        self._enabled = val
        # 递归设置子节点
        for c in self.children:
            c.enabled = val

    @property
    def is_leaf(self) -> bool:
        return len(self.children) == 0

    @property
    def full_path(self) -> str:
        """从根到当前节点的完整路径, 用 / 分隔。"""
        if self.parent is None:
            return self.name
        return f"{self.parent.full_path}/{self.name}"

    def add_child(self, child: TaskNode) -> TaskNode:
        child.parent = self
        self.children.append(child)
        return child

    def create_child(self, name: str, description: str = "",
                     run_fn: Callable | None = None) -> TaskNode:
        """创建并添加子节点。如提供 run_fn 则为叶子节点。"""
        child = TaskNode(name, description, parent=self)
        child._run_fn = run_fn
        self.children.append(child)
        return child

    def create_group(self, name: str, description: str = "") -> TaskNode:
        """创建分组节点(非叶子)。"""
        return self.create_child(name, description)

    def create_leaf(self, name: str, description: str = "",
                    run_fn: Callable | None = None) -> TaskNode:
        """创建叶子节点(可执行)。"""
        return self.create_child(name, description, run_fn)

    def run(self) -> TaskStatus:
        """执行此节点(仅叶子节点)。返回最终状态。"""
        if not self._enabled:
            self.status = TaskStatus.DISABLED
            return self.status
        if not self._run_fn:
            self.status = TaskStatus.SUCCESS
            return self.status
        self.status = TaskStatus.RUNNING
        self.start_time = datetime.now()
        self.end_time = None
        try:
            result = self._run_fn()
            if isinstance(result, bool):
                self.status = TaskStatus.SUCCESS if result else TaskStatus.FAILED
            elif isinstance(result, int):
                self.status = TaskStatus.SUCCESS if result == 0 else TaskStatus.FAILED
            elif isinstance(result, TaskStatus):
                self.status = result
            else:
                self.status = TaskStatus.SUCCESS
        except Exception as e:
            self.status = TaskStatus.FAILED
            self.output_message = str(e)
        self.end_time = datetime.now()
        return self.status
        return self.status

    def reset(self):
        """重置状态为 PENDING。"""
        self.status = TaskStatus.PENDING
        self.output_message = ""
        self.start_time = None
        self.end_time = None
        self.log_path = None
        for c in self.children:
            c.reset()

    # ── 遍历 ──

    def all_leaves(self) -> list[TaskNode]:
        """获取所有叶子节点(深度优先)。"""
        result = []
        self._collect_leaves(result)
        return result

    def _collect_leaves(self, out: list[TaskNode]):
        if self.is_leaf:
            out.append(self)
        else:
            for c in self.children:
                c._collect_leaves(out)

    def all_nodes(self) -> list[TaskNode]:
        """获取所有节点(深度优先)。"""
        result = [self]
        for c in self.children:
            result.extend(c.all_nodes())
        return result

    def __repr__(self):
        return f"TaskNode({self.name!r}, status={self.status.value})"
