# -*- coding: utf-8 -*-
"""
Chill Build GUI — 树形构建管理器 (ttk.Treeview)

用法:
  python scripts/build_gui.py

功能:
  - ttk.Treeview 原生树形缩进, 每行一个节点
  - 单击叶子 → 右侧显示任务详情 (状态/时间/日志)
  - 双击叶子 → 切换启用/禁用 (☑ ↔ ☐)
  - 实时颜色标识
  - 每个任务日志写入 build_logs/<session>/<task>.log
"""
from __future__ import annotations

import os
import queue
import subprocess
import sys
import threading
import tkinter as tk
from tkinter import ttk, messagebox
from pathlib import Path
from datetime import datetime

sys.path.insert(0, str(Path(__file__).resolve().parent))

from build_config import setup_toolchain
from build_runner import BuildRunner, BUILD_LOGS_DIR, restore_status_from_logs
from tasks.base import TaskNode, TaskStatus
from build_tree import build_tree


STATUS_MAP = {
    TaskStatus.PENDING:   {"icon": "○", "label": "等待中", "tag": "pending"},
    TaskStatus.RUNNING:   {"icon": "◌", "label": "运行中", "tag": "running"},
    TaskStatus.SUCCESS:   {"icon": "✔", "label": "成功",   "tag": "success"},
    TaskStatus.FAILED:    {"icon": "✘", "label": "失败",   "tag": "failed"},
    TaskStatus.SKIPPED:   {"icon": "→", "label": "已跳过", "tag": "skipped"},
    TaskStatus.DISABLED:  {"icon": "—", "label": "已禁用", "tag": "disabled"},
}

STATUS_BG = {
    "pending":  "#f5f5f5", "running": "#fff8e1",
    "success":  "#e8f5e9", "failed":  "#ffebee",
    "skipped":  "#eeeeee", "disabled":"#fafafa",
}

CHK_ON  = "☑"
CHK_OFF = "☐"


# ════════════════════════════════════════════
#  日志重定向
# ════════════════════════════════════════════

class LogRedirector:
    def __init__(self, text_widget: tk.Text):
        self.text_widget = text_widget
        self.queue: queue.Queue[str] = queue.Queue()
        self._stdout = sys.stdout
        self._stderr = sys.stderr

    def write(self, s: str):
        if s:
            self.queue.put(s)

    def flush(self):
        pass

    def activate(self):
        sys.stdout = self
        sys.stderr = self

    def deactivate(self):
        sys.stdout = self._stdout
        sys.stderr = self._stderr

    def poll(self, widget):
        try:
            while True:
                msg = self.queue.get_nowait()
                widget.insert(tk.END, msg + "\n")
                widget.see(tk.END)
        except queue.Empty:
            pass
        widget.after(100, self.poll, widget)


# ════════════════════════════════════════════
#  主 GUI
# ════════════════════════════════════════════

class BuildGUI:
    def __init__(self):
        setup_toolchain()

        self.root = tk.Tk()
        self.root.title("Chill Build Manager")
        self.root.geometry("1350x850")

        self.runner = BuildRunner()
        self.runner.on_status_change = self._on_node_status
        self.runner.on_build_start = self._on_build_start
        self.runner.on_build_done = self._on_build_done

        self._iid_to_node: dict[str, TaskNode] = {}
        self._node_to_iid: dict[TaskNode, str] = {}
        self._iid_counter = 0
        self._roots: list[TaskNode] = []
        self._selected_node: TaskNode | None = None

        self._build_ui()
        self._rebuild_tree()

        self.root.protocol("WM_DELETE_WINDOW", self._on_close)
        self.root.mainloop()

    # ── UI 布局 ──

    def _build_ui(self):
        # 顶部控制栏
        top = ttk.Frame(self.root, padding=5)
        top.pack(fill=tk.X)

        ttk.Label(top, text="模式:").pack(side=tk.LEFT, padx=(0, 3))
        self._mode_var = tk.StringVar(value="all")
        for text, val in [("全部", "all"), ("仅 Mod", "mod"),
                          ("仅 Player", "player"), ("FH6 Asset", "fh6-asset"),
                          ("FH6 Mod", "fh6")]:
            ttk.Radiobutton(top, text=text, variable=self._mode_var,
                            value=val, command=self._rebuild_tree).pack(
                side=tk.LEFT, padx=2)

        ttk.Separator(top, orient=tk.VERTICAL).pack(side=tk.LEFT, fill=tk.Y, padx=8)
        self._full_var = tk.BooleanVar(value=True)
        ttk.Checkbutton(top, text="完整构建 (--full)", variable=self._full_var,
                        command=self._rebuild_tree).pack(side=tk.LEFT, padx=3)
        self._skip_flutter_var = tk.BooleanVar(value=False)
        ttk.Checkbutton(top, text="跳过 Flutter", variable=self._skip_flutter_var,
                        command=self._rebuild_tree).pack(side=tk.LEFT, padx=3)

        ttk.Separator(top, orient=tk.VERTICAL).pack(side=tk.LEFT, fill=tk.Y, padx=8)
        ttk.Button(top, text="▶ 构建", command=self._start_build).pack(side=tk.LEFT, padx=3)
        ttk.Button(top, text="■ 停止", command=self._stop_build).pack(side=tk.LEFT, padx=3)
        ttk.Button(top, text="↺ 重置", command=self._reset_status).pack(side=tk.LEFT, padx=3)

        ttk.Separator(top, orient=tk.VERTICAL).pack(side=tk.LEFT, fill=tk.Y, padx=8)
        ttk.Button(top, text="全选叶子", command=self._select_all).pack(side=tk.LEFT, padx=2)
        ttk.Button(top, text="全不选", command=self._deselect_all).pack(side=tk.LEFT, padx=2)

        # 主区域: 左 | 右上+右下
        pw = ttk.PanedWindow(self.root, orient=tk.HORIZONTAL)
        pw.pack(fill=tk.BOTH, expand=True, padx=5, pady=5)

        # ── 左侧: Treeview ──
        left = ttk.Frame(pw)
        pw.add(left, weight=4)

        ttk.Label(left, text="构建任务树", font=("", 10, "bold")).pack(anchor=tk.W)

        style = ttk.Style()
        style.configure("Build.Treeview", rowheight=24,
                        font=("Microsoft YaHei UI", 9))

        self._tree = ttk.Treeview(left, columns=("chk", "st"),
                                  displaycolumns=("chk", "st"),
                                  show="tree headings", style="Build.Treeview")
        self._tree.heading("#0", text="任务")
        self._tree.heading("chk", text="")
        self._tree.heading("st", text="")
        self._tree.column("#0", width=500, anchor=tk.W, stretch=True)
        self._tree.column("chk", width=28, anchor=tk.CENTER, stretch=False)
        self._tree.column("st", width=28, anchor=tk.CENTER, stretch=False)

        vsb = ttk.Scrollbar(left, orient=tk.VERTICAL, command=self._tree.yview)
        self._tree.configure(yscrollcommand=vsb.set)
        self._tree.pack(side=tk.LEFT, fill=tk.BOTH, expand=True)
        vsb.pack(side=tk.RIGHT, fill=tk.Y)

        self._tree.bind("<Button-1>", self._on_click)
        self._tree.bind("<Double-1>", self._on_double_click)

        for tag, bg in STATUS_BG.items():
            self._tree.tag_configure(tag, background=bg)

        # ── 右侧: 上下分 ──
        right_pw = ttk.PanedWindow(pw, orient=tk.VERTICAL)
        pw.add(right_pw, weight=6)

        # 右上: 构建日志
        right_top = ttk.Frame(right_pw)
        right_pw.add(right_top, weight=6)

        ttk.Label(right_top, text="构建日志", font=("", 10, "bold")).pack(anchor=tk.W)
        log_frame = ttk.Frame(right_top)
        log_frame.pack(fill=tk.BOTH, expand=True)

        self._log = tk.Text(log_frame, wrap=tk.WORD, state=tk.NORMAL,
                            font=("Consolas", 9), bg="#f5f5f5", fg="#333333",
                            insertbackground="#333333")
        log_sb = ttk.Scrollbar(log_frame, orient=tk.VERTICAL, command=self._log.yview)
        self._log.configure(yscrollcommand=log_sb.set)
        self._log.pack(side=tk.LEFT, fill=tk.BOTH, expand=True)
        log_sb.pack(side=tk.RIGHT, fill=tk.Y)

        self._log_redirector = LogRedirector(self._log)
        self._log_redirector.activate()
        self._log_redirector.poll(self._log)  # 启动定时刷新

        # 右下: 任务详情
        right_bot = ttk.Frame(right_pw)
        right_pw.add(right_bot, weight=4)

        self._detail_frame = ttk.LabelFrame(right_bot, text="任务详情", padding=5)
        self._detail_frame.pack(fill=tk.BOTH, expand=True)

        # 头部信息行
        info_row = ttk.Frame(self._detail_frame)
        info_row.pack(fill=tk.X)

        self._detail_name = ttk.Label(info_row, text="", font=("", 11, "bold"))
        self._detail_name.pack(anchor=tk.W)

        self._detail_status = ttk.Label(info_row, text="")
        self._detail_status.pack(anchor=tk.W, pady=(2, 0))

        self._detail_time = ttk.Label(info_row, text="")
        self._detail_time.pack(anchor=tk.W)

        self._detail_log = ttk.Label(info_row, text="", foreground="blue",
                                     cursor="hand2")
        self._detail_log.pack(anchor=tk.W)
        self._detail_log.bind("<Button-1>", self._on_open_log)

        ttk.Separator(self._detail_frame, orient=tk.HORIZONTAL).pack(fill=tk.X, pady=3)

        # 日志内容查看器
        self._detail_log_view = tk.Text(self._detail_frame, wrap=tk.WORD,
                                        state=tk.DISABLED,
                                        font=("Consolas", 8), bg="#fafafa",
                                        fg="#333333", height=8)
        detail_log_sb = ttk.Scrollbar(self._detail_frame, orient=tk.VERTICAL,
                                      command=self._detail_log_view.yview)
        self._detail_log_view.configure(yscrollcommand=detail_log_sb.set)
        self._detail_log_view.pack(side=tk.LEFT, fill=tk.BOTH, expand=True)
        detail_log_sb.pack(side=tk.RIGHT, fill=tk.Y)

        # 底部状态栏
        bar = ttk.Frame(self.root, relief=tk.SUNKEN, padding=3)
        bar.pack(fill=tk.X)
        self._status_lbl = ttk.Label(bar, text="就绪")
        self._status_lbl.pack(side=tk.LEFT)
        self._progress_var = tk.StringVar(value="")
        ttk.Label(bar, textvariable=self._progress_var).pack(side=tk.RIGHT)

    # ── 树构建 ──

    def _rebuild_tree(self):
        for item in self._tree.get_children():
            self._tree.delete(item)
        self._iid_to_node.clear()
        self._node_to_iid.clear()
        self._iid_counter = 0
        self._selected_node = None

        mode = self._mode_var.get()
        full = self._full_var.get()
        skip = self._skip_flutter_var.get()
        self._roots = build_tree(mode, full, skip)

        virtual = TaskNode("__ROOT__")
        for r in self._roots:
            virtual.children.append(r)

        self._populate_tree("", virtual)
        self._update_progress()
        self._show_detail(None)

        # 从上次构建日志恢复状态和时间
        restore_status_from_logs(virtual)
        self._update_all_rows()
        self._update_progress()

    def _populate_tree(self, parent_iid: str, node: TaskNode):
        iid = str(self._iid_counter)
        self._iid_counter += 1
        self._iid_to_node[iid] = node
        self._node_to_iid[node] = iid

        is_leaf = node.is_leaf
        chk = CHK_ON if (is_leaf and node.enabled) else (CHK_OFF if is_leaf else "")
        st = STATUS_MAP[node.status]["icon"]
        tag = STATUS_MAP[node.status]["tag"]

        self._tree.insert(
            parent_iid, tk.END, iid=iid,
            text=node.name,
            values=(chk, st),
            tags=(tag,),
            open=True,
        )
        for child in node.children:
            self._populate_tree(iid, child)

    # ── 单击: 查看详情 ──

    def _on_click(self, event):
        iid = self._tree.identify_row(event.y)
        if not iid or iid not in self._iid_to_node:
            return
        node = self._iid_to_node[iid]
        self._show_detail(node)

    # ── 双击: 切换启用 ──

    def _on_double_click(self, event):
        col = self._tree.identify_column(event.x)
        iid = self._tree.identify_row(event.y)
        if not iid or iid not in self._iid_to_node:
            return
        node = self._iid_to_node[iid]
        if not node.is_leaf:
            return
        # 双击任意列都可切换
        node.enabled = not node.enabled
        self._update_row(iid, node)
        self._update_progress()
        self._show_detail(node)  # 刷新详情

    # ── 详情面板 ──

    def _show_detail(self, node: TaskNode | None):
        if node is None:
            self._detail_name.config(text="")
            self._detail_status.config(text="")
            self._detail_time.config(text="")
            self._detail_log.config(text="")
            self._set_log_content("")
            return

        self._selected_node = node
        self._detail_name.config(text=node.name)

        sm = STATUS_MAP[node.status]
        self._detail_status.config(
            text=f"状态: {sm['icon']} {sm['label']}")

        # 时间
        time_parts = []
        if node.start_time:
            time_parts.append(f"开始: {node.start_time.strftime('%Y-%m-%d %H:%M:%S')}")
        if node.end_time:
            time_parts.append(f"结束: {node.end_time.strftime('%Y-%m-%d %H:%M:%S')}")
            if node.start_time:
                delta = node.end_time - node.start_time
                time_parts.append(f"耗时: {delta.total_seconds():.1f}s")
        time_str = "  |  ".join(time_parts) if time_parts else "尚未构建"
        # 加描述
        if node.description:
            time_str += f"  —  {node.description}"
        if node.output_message:
            time_str += f"  [错误: {node.output_message}]"
        self._detail_time.config(text=time_str)

        # 日志链接 & 内容
        if node.log_path and node.log_path.exists():
            self._detail_log.config(
                text=f"📄 build_logs/{node.log_path.name}",
                foreground="blue", cursor="hand2")
            self._detail_log._log_path = node.log_path
            # 读取日志内容
            try:
                content = node.log_path.read_text(encoding="utf-8", errors="replace")
            except Exception:
                content = "(无法读取日志文件)"
            self._set_log_content(content)
        else:
            self._detail_log.config(text="")
            self._set_log_content("(尚未生成日志)")

    def _set_log_content(self, text: str):
        """设置日志查看器内容。"""
        self._detail_log_view.configure(state=tk.NORMAL)
        self._detail_log_view.delete("1.0", tk.END)
        self._detail_log_view.insert("1.0", text)
        self._detail_log_view.configure(state=tk.DISABLED)
        # 滚到底部
        self._detail_log_view.see(tk.END)

    def _on_open_log(self, event):
        """点击日志链接, 用默认编辑器打开。"""
        lp = getattr(self._detail_log, "_log_path", None)
        if lp and lp.exists():
            os.startfile(str(lp))

    # ── 行更新 ──

    def _update_row(self, iid: str, node: TaskNode):
        sm = STATUS_MAP[node.status]
        chk = CHK_ON if (node.is_leaf and node.enabled) else (
            CHK_OFF if node.is_leaf else "")
        self._tree.item(iid, text=node.name, values=(chk, sm["icon"]),
                        tags=(sm["tag"],))

    def _update_all_rows(self):
        for iid, node in self._iid_to_node.items():
            self._update_row(iid, node)

    # ── 构建控制 ──

    def _start_build(self):
        if self.runner.running:
            messagebox.showinfo("提示", "构建已在运行")
            return
        if not self._roots:
            return

        # 重置 native 去重集合 (新一次构建)
        from build_tree import _built_natives
        _built_natives.clear()

        virtual = TaskNode("__ALL__")
        for r in self._roots:
            for leaf in r.all_leaves():
                if leaf.enabled and leaf.status != TaskStatus.DISABLED:
                    leaf.status = TaskStatus.PENDING
            virtual.children.append(r)

        self.runner.set_tree(virtual)
        self._update_all_rows()
        self._update_progress()
        self.runner.start()

    def _stop_build(self):
        if self.runner.running:
            self.runner.stop()
            self._status_lbl.config(text="正在停止...")

    def _reset_status(self):
        for r in self._roots:
            r.reset()
        self._update_all_rows()
        self._update_progress()
        self._show_detail(self._selected_node)
        self._status_lbl.config(text="已重置")

    def _select_all(self):
        for r in self._roots:
            for leaf in r.all_leaves():
                leaf.enabled = True
        self._update_all_rows()
        self._update_progress()

    def _deselect_all(self):
        for r in self._roots:
            for leaf in r.all_leaves():
                leaf.enabled = False
        self._update_all_rows()
        self._update_progress()

    # ── Runner 回调 ──

    def _on_node_status(self, node: TaskNode):
        def _update():
            if node in self._node_to_iid:
                iid = self._node_to_iid[node]
                self._update_row(iid, node)
                # 如果当前选中的是这个节点, 刷新详情
                if node is self._selected_node:
                    self._show_detail(node)
            self._update_progress()
        self.root.after(0, _update)

    def _on_build_start(self):
        self.root.after(0, lambda: self._status_lbl.config(text="构建中..."))

    def _on_build_done(self, all_ok: bool):
        def _done():
            self._status_lbl.config(
                text="✓ 构建完成" if all_ok else "✘ 构建完成 (有失败)")
            if all_ok:
                messagebox.showinfo("完成", "全部构建成功!")
            else:
                messagebox.showwarning("完成", "部分任务失败, 请检查日志。")
            self._update_all_rows()
            self._update_progress()
        self.root.after(0, _done)

    def _update_progress(self):
        total = 0
        done = 0
        for r in self._roots:
            for leaf in r.all_leaves():
                if not leaf.enabled:
                    continue
                total += 1
                if leaf.status in (TaskStatus.SUCCESS, TaskStatus.FAILED,
                                   TaskStatus.SKIPPED, TaskStatus.DISABLED):
                    done += 1
        self._progress_var.set(f"{done}/{total}" if total else "")

    def _on_close(self):
        if self.runner.running:
            self.runner.stop()
        if self._log_redirector:
            self._log_redirector.deactivate()
        self.root.destroy()


if __name__ == "__main__":
    BuildGUI()
