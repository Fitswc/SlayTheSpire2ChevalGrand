"""Local card portrait editor. Run with Python 3.10+ and Pillow."""
from __future__ import annotations

import json
import os
from pathlib import Path
import re
import shutil
import tempfile
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
        self.center = (0.5, 0.5)
        self.drag = None
        self.pending = None
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
                        command=self.filter_cards).pack(anchor="w", pady=8)
        list_frame = ttk.Frame(left)
        list_frame.pack(fill="both", expand=True)
        self.listbox = tk.Listbox(list_frame, width=32, exportselection=False,
                                 font=("Microsoft YaHei UI", 10), activestyle="dotbox")
        scroll = ttk.Scrollbar(list_frame, command=self.listbox.yview)
        self.listbox.configure(yscrollcommand=scroll.set)
        scroll.pack(side="right", fill="y")
        self.listbox.pack(side="left", fill="both", expand=True)
        self.listbox.bind("<<ListboxSelect>>", self.select_card)
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
        self.canvas.bind("<ButtonRelease-1>", lambda e: setattr(self, "drag", None))
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
                  command=lambda _: self.schedule_draw()).pack(side="left", fill="x", expand=True, padx=8)
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
        self.search.trace_add("write", lambda *_: self.filter_cards())
        self.width.trace_add("write", lambda *_: self.schedule_draw())
        self.height.trace_add("write", lambda *_: self.schedule_draw())
        self.filter_cards()

    def filter_cards(self):
        query = self.search.get().strip().casefold()
        self.filtered = [c for c in self.cards if query in (c.title + c.name).casefold()
                         and (not self.only_missing.get() or not c.path.exists())]
        self.listbox.delete(0, "end")
        for card in self.filtered:
            flag = "已有" if card.path.exists() else "缺图"
            if not card.configured:
                flag = "未接入"
            self.listbox.insert("end", f"[{flag}] {card.title} · {card.name}")
        self.count.config(text=f"显示 {len(self.filtered)} / 共 {len(self.cards)} 张")
        if self.card in self.filtered:
            self.listbox.selection_set(self.filtered.index(self.card))
        else:
            self.card = None
            self.info.set("请选择卡牌；可搜索中文名或类名。")
        self.schedule_draw()

    def select_card(self, _=None):
        selected = self.listbox.curselection()
        if not selected:
            return
        self.card = self.filtered[selected[0]]
        self.info.set(f"{self.card.title} · {self.card.name}\n目标：{self.card.path.relative_to(ROOT)}")
        try:
            self.set_size(preferred_size(self.card))
        except OSError:
            self.set_size(DEFAULT_SIZE)
            self.status.set("已有卡图无法读取，已使用默认尺寸；可导入新图片替换。")
        if not self.card.configured:
            self.status.set("此卡未配置受支持的 PortraitPath，暂不能保存到项目；导出文件也不会自动在游戏显示。")
        else:
            self.status.set("尺寸来自已有卡图；没有卡图时默认 850×646。导入图片后可拖动取景。")
        self.schedule_draw()

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

    def load_image(self, path=None):
        if path is None:
            path = filedialog.askopenfilename(title="选择卡图原图", filetypes=[
                ("图片", "*.png *.jpg *.jpeg *.webp *.bmp *.tif *.tiff"), ("所有文件", "*.*")])
        if not path:
            return
        try:
            with Image.open(path) as original:
                # The preview is bounded, but retain source pixels for final output.
                if original.width * original.height > 40_000_000:
                    raise ValueError("原图超过 4000 万像素，请先缩小后导入。")
                image = ImageOps.exif_transpose(original).convert("RGBA")
            self.image = image
            self.source_name = Path(path).name
            self.reset()
            self.status.set(f"已导入 {self.source_name}（{image.width}×{image.height}）；白框内是最终画面。")
        except (OSError, ValueError, Image.DecompressionBombError) as exc:
            messagebox.showerror("无法导入图片", str(exc))

    def load_existing(self):
        if self.card and self.card.path.exists():
            self.load_image(self.card.path)
        else:
            messagebox.showinfo("暂无卡图", "请先选择有卡图的卡牌，或使用“导入图片”。")

    def reset(self):
        self.center = (0.5, 0.5)
        self.zoom.set(1)
        self.schedule_draw()

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
            self.drag = (event.x, event.y, self.center)

    def drag_move(self, event):
        if self.drag is None:
            return
        x, y, center = self.drag
        self.center = (center[0]+(event.x-x)/self.scale/self.image.width,
                       center[1]+(event.y-y)/self.scale/self.image.height)
        self.schedule_draw()

    def save(self, to_project):
        card = self.card
        if not card or self.image is None:
            return
        if to_project and not card.configured:
            messagebox.showerror("无法接入", "该卡牌尚未配置受支持的卡图路径。")
            return
        target = card.path if to_project else TOOLS / "output" / (card.name + ".png")
        overwrite = target.exists()
        if overwrite and not messagebox.askyesno("确认替换卡图", f"即将替换：\n{target}\n\n原文件会先备份到 tools/backups。是否继续？"):
            return
        try:
            image = render_crop(self.image, self.target_size(), self.center, self.zoom.get())
            backup = save_png(image, target, overwrite=overwrite)
            self.filter_cards()
            self.status.set(f"已保存：{target}" + (f"；原图备份：{backup.name}" if backup else ""))
            messagebox.showinfo("保存成功", f"已保存 {image.width}×{image.height} PNG：\n{target}" +
                                (f"\n\n原图备份：\n{backup}" if backup else ""))
        except (OSError, ValueError) as exc:
            messagebox.showerror("保存失败", str(exc))


def main():
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
        messagebox.showerror("启动失败", str(exc))
        root.destroy()
        return
    root.mainloop()


if __name__ == "__main__":
    main()
