"""Local card portrait editor. Run with Python 3.10+ and Pillow."""
from __future__ import annotations

import json
import os
from pathlib import Path
import re
import shutil
import sys
import tempfile
from time import monotonic
from dataclasses import dataclass
from datetime import datetime
import tkinter as tk
from tkinter import filedialog, messagebox, ttk

from PIL import Image, ImageOps, ImageTk

TOOLS = Path(__file__).resolve().parent
ROOT = TOOLS.parent
GAME = ROOT / "ChevalGrandSlay"
ART = GAME / "ChevalGrandSlay/images/cards"
DEFAULT_SIZE = (850, 646)  # Most common existing portrait size; not an engine requirement.
PORTRAIT = 'PortraitPath: $"{Entry.ResPath}/images/cards/{GetType().Name}.png"'


def log_action(action, details=""):
    """Console output must never interrupt image editing or saving."""
    try:
        if sys.stdout is not None:
            print(f"[{datetime.now():%H:%M:%S}] [{action}] {details}", flush=True)
    except (OSError, UnicodeError, ValueError):
        pass


@dataclass(frozen=True)
class Card:
    name: str
    title: str
    configured: bool

    @property
    def path(self):
        return ART / (self.name + ".png")


def discover_cards() -> list[Card]:
    """Read actual class names and supported portrait expressions, never comments."""
    titles = json.loads((GAME / "ChevalGrandSlay/localization/zhs/cards.json").read_text(encoding="utf-8-sig"))
    cards = []
    for path in sorted((GAME / "ChevalGrandSlayCode/Cards").glob("*.cs")):
        source = path.read_text(encoding="utf-8-sig")
        source = re.sub(r"/\*.*?\*/|//[^\r\n]*", "", source, flags=re.S)
        match = re.search(r"\bclass\s+(\w+)\s*:\s*ModCardTemplate\b", source)
        if not match:
            continue
        name = match[1]
        key = re.sub(r"([A-Z]+)([A-Z][a-z])", r"\1_\2", name)
        key = re.sub(r"([a-z0-9])([A-Z])", r"\1_\2", key).upper()
        title = titles.get("CHEVAL_GRAND_SLAY_CARD_" + key + ".title", name)
        configured = re.sub(r"\s+", "", PORTRAIT) in re.sub(r"\s+", "", source)
        cards.append(Card(name, title, configured))
    if not cards:
        raise ValueError("未找到卡牌，请保持 tools 位于项目根目录内。")
    return cards


def preferred_size(card: Card) -> tuple[int, int]:
    if card.path.exists():
        with Image.open(card.path) as image:
            return image.size
    return DEFAULT_SIZE


def crop_box(size, target, center=(0.5, 0.5), zoom=1.0):
    """Largest in-bounds crop at target aspect, reduced by zoom; no stretching."""
    sw, sh = size
    tw, th = target
    if min(sw, sh, tw, th) <= 0 or not 1 <= zoom <= 8:
        raise ValueError("尺寸必须大于零，放大倍率须为 1–8。")
    cw = min(sw, sh * tw / th) / zoom
    ch = cw * th / tw
    x = max(0, min(sw - cw, center[0] * sw - cw / 2))
    y = max(0, min(sh - ch, center[1] * sh - ch / 2))
    return x, y, x + cw, y + ch


def render_crop(image, target, center, zoom):
    return image.resize(target, Image.Resampling.LANCZOS,
                        box=crop_box(image.size, target, center, zoom))


def save_png(image, target: Path, *, overwrite=False, backup_dir=TOOLS / "backups"):
    """Stage a complete PNG; preserve an old file before an explicit overwrite."""
    target = Path(target)
    target.parent.mkdir(parents=True, exist_ok=True)
    backup = None
    with tempfile.NamedTemporaryFile(dir=target.parent, suffix=".tmp", delete=False) as f:
        staged = Path(f.name)
    try:
        image.save(staged, format="PNG")
        if overwrite and target.exists():
            backup_dir.mkdir(parents=True, exist_ok=True)
            stamp = datetime.now().strftime("%Y%m%d-%H%M%S-%f")
            backup = backup_dir / (target.stem + "-" + stamp + ".png")
            shutil.copy2(target, backup)
            os.replace(staged, target)
        else:
            # Windows rename is atomic and refuses an existing destination.
            if os.name == "nt":
                os.rename(staged, target)
            else:
                os.link(staged, target)
    finally:
        staged.unlink(missing_ok=True)
    return backup


class CardList(tk.Text):
    """Read-only, keyboard-accessible rows with separately colored status fields."""

    def __init__(self, parent):
        super().__init__(parent, width=32, wrap="none", state="disabled",
                         font=("Microsoft YaHei UI", 10), cursor="arrow",
                         exportselection=False, takefocus=True)
        # Read-only Text still permits native text selection. Use only our row
        # bindings so dragging, multi-clicks and selection shortcuts cannot
        # create a second, multi-row highlight over the selected card.
        self.bindtags(tuple(tag for tag in self.bindtags() if tag != "Text"))
        self.selected = None
        self.row_count = 0
        self.tag_configure("missing", foreground="#c62828")
        self.tag_configure("existing", foreground="#16803c")
        self.tag_configure("selected", background="#dbeafe")
        self.bind("<Button-1>", self.click_row)
        self.bind("<Up>", lambda _: self.move_selection(-1))
        self.bind("<Down>", lambda _: self.move_selection(1))
        self.bind("<Home>", lambda _: self.choose(0))
        self.bind("<End>", lambda _: self.choose(self.row_count - 1))
        self.bind("<MouseWheel>", self.scroll_wheel)
        self.bind("<Button-4>", lambda _: self.scroll(-3))
        self.bind("<Button-5>", lambda _: self.scroll(3))
        self.bind("<Prior>", lambda _: self.scroll(-1, "pages"))
        self.bind("<Next>", lambda _: self.scroll(1, "pages"))

    def scroll(self, amount, units="units"):
        self.yview_scroll(amount, units)
        return "break"

    def scroll_wheel(self, event):
        if event.delta:
            amount = max(1, abs(event.delta) // 120) * 3
            self.scroll(-amount if event.delta > 0 else amount)
        return "break"

    def set_cards(self, cards):
        self.configure(state="normal")
        self.delete("1.0", "end")
        self.selected = None
        self.row_count = len(cards)
        for card in cards:
            exists = card.path.exists()
            self.insert("end", "[已有]" if exists else "[缺图]", "existing" if exists else "missing")
            note = "（未接入）" if not card.configured else ""
            self.insert("end", f" {card.title} · {card.name}{note}\n")
        self.configure(state="disabled")

    def select(self, index):
        self.tag_remove("selected", "1.0", "end")
        self.selected = index
        if index is not None:
            self.tag_add("selected", f"{index + 1}.0", f"{index + 1}.end+1c")
            self.see(f"{index + 1}.0")

    def choose(self, index):
        if 0 <= index < self.row_count and index != self.selected:
            self.select(index)
            self.event_generate("<<CardSelect>>")
        return "break"

    def click_row(self, event):
        self.focus_set()
        return self.choose(int(self.index(f"@{event.x},{event.y}").split(".")[0]) - 1)

    def move_selection(self, step):
        return self.choose(0 if self.selected is None else self.selected + step)


class Editor:
    def __init__(self, root):
        self.root = root
        root.title("卡图工坊 · ChevalGrandSlay")
        display_scale = max(1, root.winfo_fpixels("1i") / 96)
        window_width = min(root.winfo_screenwidth()-80, round(1200*display_scale))
        window_height = min(root.winfo_screenheight()-100, round(800*display_scale))
        root.geometry(f"{window_width}x{window_height}")
        root.minsize(min(window_width, round(980*display_scale)),
                     min(window_height, round(720*display_scale)))
        self.cards = discover_cards()
        self.card = None
        self.image = None
        self.image_revision = 0
        self.saved_edit = None
        self.center = (0.5, 0.5)
        self.drag = None
        self.pending = None
        self.pending_logs = {}
        self.last_drag_log = 0
        self.filtered = []
        self.search = tk.StringVar()
        self.only_missing = tk.BooleanVar()
        self.width = tk.StringVar(value="850")
        self.height = tk.StringVar(value="646")
        self.zoom = tk.DoubleVar(value=1)
        self.status = tk.StringVar(value="选择卡牌，然后导入图片。所有操作在本机完成。")
        self.info = tk.StringVar()
        self.preview_info = tk.StringVar()
        style = ttk.Style(root)
        style.configure("TButton", padding=(10, 6))
        style.configure("Title.TLabel", font=("Microsoft YaHei UI", 15, "bold"))
        outer = ttk.Frame(root, padding=16)
        outer.pack(fill="both", expand=True)
        ttk.Label(outer, text="卡图工坊", style="Title.TLabel").pack(anchor="w")
        ttk.Label(outer, text="选择卡牌 → 导入图片 → 调整裁剪 → 保存 PNG").pack(anchor="w", pady=(4, 12))
        body = ttk.Frame(outer)
        body.pack(fill="both", expand=True)
        left = ttk.Frame(body, width=280)
        left.pack(side="left", fill="y", padx=(0, 16))
        ttk.Entry(left, textvariable=self.search, width=32).pack(fill="x")
        ttk.Checkbutton(left, text="仅显示缺少卡图的卡牌", variable=self.only_missing,
                        command=lambda: self.filter_cards(announce=True)).pack(anchor="w", pady=8)
        list_frame = ttk.Frame(left)
        list_frame.pack(fill="both", expand=True)
        self.card_list = CardList(list_frame)
        scroll = ttk.Scrollbar(list_frame, command=self.card_list.yview)
        self.card_list.configure(yscrollcommand=scroll.set)
        scroll.pack(side="right", fill="y")
        self.card_list.pack(side="left", fill="both", expand=True)
        self.card_list.bind("<<CardSelect>>", self.select_card)
        self.count = ttk.Label(left)
        self.count.pack(anchor="w", pady=6)
        right = ttk.Frame(body)
        right.pack(side="left", fill="both", expand=True)
        ttk.Label(right, textvariable=self.info, wraplength=790).pack(anchor="w")
        bar = ttk.Frame(right)
        bar.pack(fill="x", pady=10)
        ttk.Button(bar, text="导入图片…", command=self.load_image).pack(side="left")
        ttk.Button(bar, text="载入已有卡图", command=self.load_existing).pack(side="left", padx=6)
        ttk.Button(bar, text="居中 / 重置", command=self.reset).pack(side="left")
        self.canvas = tk.Canvas(right, background="#222833", highlightthickness=0,
                                width=700, height=330, cursor="fleur")
        self.canvas.pack(fill="both", expand=True)
        self.canvas.bind("<Configure>", lambda e: self.schedule_draw())
        self.canvas.bind("<ButtonPress-1>", self.drag_start)
        self.canvas.bind("<B1-Motion>", self.drag_move)
        self.canvas.bind("<ButtonRelease-1>", self.drag_end)
        dimensions = ttk.Frame(right)
        dimensions.pack(fill="x", pady=(10, 4))
        ttk.Label(dimensions, text="输出尺寸").pack(side="left")
        for text, size in (("850×646", (850, 646)), ("1000×760", (1000, 760))):
            ttk.Button(dimensions, text=text, command=lambda s=size: self.set_size(s)).pack(side="left", padx=3)
        ttk.Entry(dimensions, textvariable=self.width, width=6).pack(side="left", padx=(8, 2))
        ttk.Label(dimensions, text="×").pack(side="left")
        ttk.Entry(dimensions, textvariable=self.height, width=6).pack(side="left", padx=2)
        zoom_row = ttk.Frame(right)
        zoom_row.pack(fill="x")
        ttk.Label(zoom_row, text="取景放大").pack(side="left")
        ttk.Scale(zoom_row, from_=1, to=8, variable=self.zoom,
                  command=self.zoom_changed).pack(side="left", fill="x", expand=True, padx=8)
        ttk.Label(zoom_row, text="拖动白框移动取景").pack(side="left")
        bottom = ttk.Frame(right)
        bottom.pack(fill="x", pady=(8, 0))
        self.preview = ttk.Label(bottom)
        self.preview.pack(side="left")
        actions = ttk.Frame(bottom)
        actions.pack(side="left", fill="x", expand=True, padx=14)
        ttk.Label(actions, textvariable=self.preview_info, wraplength=480).pack(anchor="w")
        buttons = ttk.Frame(actions)
        buttons.pack(anchor="w", pady=8)
        self.export_button = ttk.Button(buttons, text="导出到 tools/output", command=lambda: self.save(False))
        self.export_button.pack(side="left")
        self.project_button = ttk.Button(buttons, text="保存到项目卡图目录", command=lambda: self.save(True))
        self.project_button.pack(side="left", padx=6)
        ttk.Label(outer, textvariable=self.status, wraplength=1120).pack(anchor="w", pady=(12, 0))
        self.search.trace_add("write", lambda *_: self.filter_cards(announce=True))
        self.width.trace_add("write", self.dimensions_changed)
        self.height.trace_add("write", self.dimensions_changed)
        root.protocol("WM_DELETE_WINDOW", self.close)
        self.filter_cards()
        log_action("就绪", f"已识别 {len(self.cards)} 张卡牌；操作日志显示在此窗口。")

    def queue_log(self, action, details):
        # Merge rapid typing/slider updates, retaining the latest result.
        if action in self.pending_logs:
            self.root.after_cancel(self.pending_logs[action][0])
        timer = self.root.after(300, lambda: self.emit_log(action))
        self.pending_logs[action] = (timer, details)

    def emit_log(self, action):
        _, details = self.pending_logs.pop(action)
        log_action(action, details)

    def flush_logs(self):
        for action, (timer, details) in list(self.pending_logs.items()):
            self.root.after_cancel(timer)
            log_action(action, details)
        self.pending_logs.clear()

    def close(self):
        self.flush_logs()
        self.drag_end()
        log_action("退出", "应用已关闭。")
        self.root.destroy()

    def dimensions_changed(self, *_):
        self.schedule_draw()
        if self.image is not None:
            self.queue_log("输出尺寸", f"{self.width.get()}×{self.height.get()} 像素")

    def zoom_changed(self, _=None):
        self.schedule_draw()
        if self.image is not None:
            self.queue_log("取景放大", self.crop_details())

    def crop_details(self):
        if self.image is None:
            return "尚未导入图片"
        try:
            box = crop_box(self.image.size, self.target_size(), self.center, self.zoom.get())
            coordinates = ", ".join(f"{value:.1f}" for value in box)
            return f"原图裁剪区域（左, 上, 右, 下）：({coordinates})；倍率 {self.zoom.get():.2f}"
        except ValueError as exc:
            return str(exc)

    def filter_cards(self, *, announce=False):
        query = self.search.get().strip().casefold()
        self.filtered = [c for c in self.cards if query in (c.title + c.name).casefold()
                         and (not self.only_missing.get() or not c.path.exists())]
        self.card_list.set_cards(self.filtered)
        self.count.config(text=f"显示 {len(self.filtered)} / 共 {len(self.cards)} 张")
        if self.card in self.filtered:
            self.card_list.select(self.filtered.index(self.card))
        elif self.card is None:
            self.info.set("请选择卡牌；可搜索中文名或类名。")
        # Filtering must never silently detach unsaved work from its card.
        self.schedule_draw()
        if announce:
            self.queue_log("筛选卡牌", f"关键词：{query or '全部'}；仅缺图：{'是' if self.only_missing.get() else '否'}；显示 {len(self.filtered)} 张")

    def select_card(self, _=None):
        selected = self.card_list.selected
        if selected is None:
            return
        card = self.filtered[selected]
        if card == self.card:
            return
        self.flush_logs()
        unsaved = self.has_unsaved_edit()
        if unsaved:
            log_action("切换确认", f"当前编辑尚未保存，等待确认是否切换到 {card.title}（{card.name}）。")
        if unsaved and not messagebox.askyesno(
                "尚未保存的卡图", "当前图片或裁剪编辑尚未保存或导出。\n切换后将丢弃当前编辑，并加载所选卡牌的已有卡图；缺图则清空预览。\n\n仍要切换吗？",
                icon="warning", default="no", parent=self.root):
            self.card_list.select(self.filtered.index(self.card) if self.card in self.filtered else None)
            log_action("取消切换", "保留当前卡牌和编辑。")
            return
        if unsaved:
            log_action("放弃编辑", "已确认放弃上一张卡牌的未保存编辑。")
        self.clear_image()
        self.card = card
        log_action("选择卡牌", f"{card.title}（{card.name}）")
        self.info.set(f"{self.card.title} · {self.card.name}\n目标：{self.card.path.relative_to(ROOT)}")
        try:
            self.set_size(preferred_size(self.card))
        except OSError:
            self.set_size(DEFAULT_SIZE)
            self.status.set("已有卡图无法读取，已使用默认尺寸；可导入新图片替换。")
        if self.card.path.exists():
            self.load_existing()
        else:
            self.status.set("此卡牌尚无卡图，请导入图片开始编辑；默认输出尺寸为 850×646。")
            log_action("缺少卡图", "预览已清空，等待导入图片。")
        if not self.card.configured:
            self.status.set("此卡未配置受支持的 PortraitPath，暂不能保存到项目；导出文件也不会自动在游戏显示。")
            log_action("未接入", "卡牌未配置受支持的卡图路径，仅可导出暂存。")
        self.schedule_draw()

    def edit_signature(self):
        if self.image is None:
            return None
        try:
            target = self.target_size()
            box = crop_box(self.image.size, target, self.center, self.zoom.get())
            return self.image_revision, target, tuple(round(value, 6) for value in box)
        except ValueError:
            return self.image_revision, self.width.get(), self.height.get(), self.center, self.zoom.get()

    def has_unsaved_edit(self):
        return self.image is not None and self.edit_signature() != self.saved_edit

    def clear_image(self):
        self.image = None
        self.saved_edit = None
        self.source_name = ""
        self.center = (0.5, 0.5)
        self.zoom.set(1)
        self.drag = None
        if self.pending:
            self.root.after_cancel(self.pending)
            self.pending = None
        self.draw()

    def set_size(self, size):
        self.width.set(str(size[0]))
        self.height.set(str(size[1]))

    def target_size(self):
        try:
            size = int(self.width.get()), int(self.height.get())
        except ValueError:
            raise ValueError("请填写整数宽度和高度。") from None
        if not all(16 <= n <= 4096 for n in size):
            raise ValueError("输出宽度和高度须在 16–4096 像素之间。")
        return size

    def load_image(self, path=None, *, already_saved=False):
        self.flush_logs()
        if path is None:
            log_action("导入图片", "正在选择图片文件。")
            path = filedialog.askopenfilename(title="选择卡图原图", filetypes=[
                ("图片", "*.png *.jpg *.jpeg *.webp *.bmp *.tif *.tiff"), ("所有文件", "*.*")])
        if not path:
            log_action("取消导入", "未选择图片。")
            return
        action = "加载已有卡图" if already_saved else "导入图片"
        log_action(action, f"正在读取：{path}")
        try:
            with Image.open(path) as original:
                # The preview is bounded, but retain source pixels for final output.
                if original.width * original.height > 40_000_000:
                    raise ValueError("原图超过 4000 万像素，请先缩小后导入。")
                image = ImageOps.exif_transpose(original).convert("RGBA")
            self.image = image
            self.image_revision += 1
            self.saved_edit = None
            self.source_name = Path(path).name
            self.reset(announce=False)
            if already_saved and self.target_size() == image.size:
                self.saved_edit = self.edit_signature()
            self.status.set(f"已导入 {self.source_name}（{image.width}×{image.height}）；白框内是最终画面。")
            log_action(action + "成功", f"{path}；原图 {image.width}×{image.height}；输出 {self.width.get()}×{self.height.get()}")
        except (OSError, ValueError, Image.DecompressionBombError) as exc:
            log_action(action + "失败", f"{path}；原因：{exc}")
            self.status.set("图片加载失败，请检查文件或导入其他图片。")
            messagebox.showerror("无法导入图片", str(exc))

    def load_existing(self):
        if self.card and self.card.path.exists():
            self.load_image(self.card.path, already_saved=True)
        else:
            log_action("暂无卡图", "当前未选择卡牌，或所选卡牌没有已有卡图。")
            messagebox.showinfo("暂无卡图", "请先选择有卡图的卡牌，或使用“导入图片”。")

    def reset(self, *, announce=True):
        if announce:
            self.flush_logs()
        self.center = (0.5, 0.5)
        self.zoom.set(1)
        self.schedule_draw()
        if announce:
            log_action("重置裁剪", self.crop_details())

    def schedule_draw(self):
        if self.pending:
            self.root.after_cancel(self.pending)
        self.pending = self.root.after(20, self.draw)

    def draw(self):
        self.pending = None
        self.canvas.delete("all")
        self.project_button.state(["!disabled"] if self.card and self.card.configured and self.image else ["disabled"])
        self.export_button.state(["!disabled"] if self.card and self.image else ["disabled"])
        if self.image is None:
            self.preview.config(image="")
            self.preview_info.set("")
            self.photo = None
            self.preview_photo = None
            self.canvas.create_text(self.canvas.winfo_width()/2, self.canvas.winfo_height()/2,
                                    text="导入图片后，在此拖动裁剪框", fill="#cbd5e1", font=("Microsoft YaHei UI", 13))
            return
        try:
            target = self.target_size()
        except ValueError as exc:
            self.preview_info.set(str(exc))
            self.project_button.state(["disabled"])
            self.export_button.state(["disabled"])
            return
        self.scale = min(max(1, self.canvas.winfo_width()-24)/self.image.width,
                         max(1, self.canvas.winfo_height()-24)/self.image.height)
        w, h = max(1, round(self.image.width*self.scale)), max(1, round(self.image.height*self.scale))
        self.offset = ((self.canvas.winfo_width()-w)/2, (self.canvas.winfo_height()-h)/2)
        # Avoid resizing the entire source to output size on every pointer movement.
        self.photo = ImageTk.PhotoImage(self.image.resize((w, h), Image.Resampling.BILINEAR))
        self.canvas.create_image(*self.offset, image=self.photo, anchor="nw")
        box = crop_box(self.image.size, target, self.center, self.zoom.get())
        self.center = ((box[0]+box[2])/2/self.image.width, (box[1]+box[3])/2/self.image.height)
        x1, y1, x2, y2 = [v*self.scale+self.offset[i % 2] for i, v in enumerate(box)]
        ox, oy = self.offset
        for coords in ((ox, oy, ox+w, y1), (ox, y2, ox+w, oy+h),
                       (ox, y1, x1, y2), (x2, y1, ox+w, y2)):
            self.canvas.create_rectangle(*coords, fill="#101820", stipple="gray50", outline="")
        self.canvas.create_rectangle(x1, y1, x2, y2, outline="white", width=2)
        preview_size = (max(1, round(160*target[0]/max(target))), max(1, round(160*target[1]/max(target))))
        preview_image = self.image.resize(preview_size, Image.Resampling.LANCZOS, box=box)
        self.preview_photo = ImageTk.PhotoImage(preview_image)
        self.preview.config(image=self.preview_photo)
        enlarged = target[0] > box[2]-box[0]+0.01 or target[1] > box[3]-box[1]+0.01
        name = self.card.name + ".png" if self.card else "请先选择卡牌"
        self.preview_info.set(f"成图预览 · {target[0]}×{target[1]} PNG\n{name}" +
                              ("\n取景像素小于输出尺寸，保存会放大，可能变模糊。" if enlarged else ""))

    def drag_start(self, event):
        if self.image is not None and hasattr(self, "scale"):
            self.flush_logs()
            self.drag = (event.x, event.y, self.center)
            self.last_drag_log = monotonic()
            log_action("开始移动白框", self.crop_details())

    def drag_move(self, event):
        if self.drag is None:
            return
        x, y, center = self.drag
        self.center = (center[0]+(event.x-x)/self.scale/self.image.width,
                       center[1]+(event.y-y)/self.scale/self.image.height)
        self.schedule_draw()
        if monotonic() - self.last_drag_log >= 0.3:
            self.last_drag_log = monotonic()
            log_action("移动白框", self.crop_details())

    def drag_end(self, _=None):
        if self.drag is not None:
            self.drag = None
            log_action("结束移动白框", self.crop_details())

    def save(self, to_project):
        self.flush_logs()
        card = self.card
        if not card or self.image is None:
            log_action("无法保存", "请先选择卡牌并导入图片。")
            return
        if to_project and not card.configured:
            log_action("无法保存", f"{card.name} 尚未配置受支持的卡图路径。")
            messagebox.showerror("无法接入", "该卡牌尚未配置受支持的卡图路径。")
            return
        target = card.path if to_project else TOOLS / "output" / (card.name + ".png")
        action = "保存到项目" if to_project else "导出图片"
        log_action(action, f"目标：{target}")
        overwrite = target.exists()
        if overwrite:
            log_action("覆盖确认", "目标文件已存在，等待确认；确认后会备份原文件。")
        if overwrite and not messagebox.askyesno("确认替换卡图", f"即将替换：\n{target}\n\n原文件会先备份到 tools/backups。是否继续？"):
            log_action("取消保存", f"保留已有文件：{target}")
            return
        try:
            image = render_crop(self.image, self.target_size(), self.center, self.zoom.get())
            backup = save_png(image, target, overwrite=overwrite)
            if backup:
                log_action("已备份", str(backup))
            log_action(action + "成功", f"{image.width}×{image.height} PNG → {target}")
            self.saved_edit = self.edit_signature()
            self.filter_cards()
            self.status.set(f"已保存：{target}" + (f"；原图备份：{backup.name}" if backup else ""))
            messagebox.showinfo("保存成功", f"已保存 {image.width}×{image.height} PNG：\n{target}" +
                                (f"\n\n原图备份：\n{backup}" if backup else ""))
        except (OSError, ValueError) as exc:
            log_action(action + "失败", f"{target}；原因：{exc}")
            messagebox.showerror("保存失败", str(exc))


def main():
    log_action("启动", "正在启动卡图工坊。")
    if os.name == "nt":
        # Keep Tk geometry and pointer coordinates consistent on scaled displays.
        import ctypes
        try:
            ctypes.windll.shcore.SetProcessDpiAwareness(1)
        except (AttributeError, OSError):
            ctypes.windll.user32.SetProcessDPIAware()
    root = tk.Tk()
    try:
        Editor(root)
    except (OSError, ValueError) as exc:
        log_action("启动失败", str(exc))
        messagebox.showerror("启动失败", str(exc))
        root.destroy()
        return
    root.mainloop()


if __name__ == "__main__":
    main()
