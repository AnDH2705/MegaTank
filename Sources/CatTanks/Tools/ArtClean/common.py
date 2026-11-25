"""Hàm dùng chung cho bộ công cụ chuẩn hoá ảnh CatTanks (audit / clean / reslice / compare)."""
import os, re, glob
import numpy as np, cv2
from PIL import Image

HERE = os.path.dirname(os.path.abspath(__file__))
ROOT = os.path.normpath(os.path.join(HERE, "..", ".."))            # Sources/CatTanks
ART = os.path.join(ROOT, "Assets", "CatTanks", "Resources", "Art")
MIRROR = os.path.join(ROOT, "Assets", "CatTanksMockupKit", "Textures", "Original")
OUT = os.path.join(HERE, "out")

# Phạm vi: nhân vật + đồ + vũ khí (+ quái).
SCOPE = re.compile(r"^(cat|hat|armor|uniform|gun|turret|turret_alt|pack|bug|beast)-\d+\.png$|^gear-boots\.png$")
PAD = 8            # khoảng trong suốt chuẩn quanh hình (px)
SOLID = 48         # alpha từ ngưỡng này trở lên coi là "đặc"


def group_of(name):
    return re.sub(r"-?\d+\.png$", "", name).replace(".png", "")


def in_scope(folder=ART):
    return sorted((f for f in os.listdir(folder) if SCOPE.match(f)),
                  key=lambda f: (group_of(f), int((re.findall(r"(\d+)\.png$", f) or ["0"])[0])))


def load(path):
    return np.array(Image.open(path).convert("RGBA"))


def save(path, a):
    os.makedirs(os.path.dirname(path), exist_ok=True)
    Image.fromarray(a, "RGBA").save(path, optimize=True)


def sharpness(a):
    """Phương sai Laplacian trên vùng đặc — số càng cao càng nét (chỉ so trong cùng nhóm)."""
    m = a[..., 3] > 200
    if m.sum() < 50:
        return 0.0
    g = cv2.cvtColor(np.ascontiguousarray(a[..., :3]), cv2.COLOR_RGB2GRAY).astype(np.float32)
    return float(cv2.Laplacian(g, cv2.CV_32F)[m].var())


def bbox(alpha, thr=10):
    ys, xs = np.where(alpha > thr)
    if not len(xs):
        return None
    return int(xs.min()), int(ys.min()), int(xs.max()), int(ys.max())


def padding(alpha, thr=10):
    b = bbox(alpha, thr)
    if b is None:
        return None
    h, w = alpha.shape
    return [b[0], b[1], w - 1 - b[2], h - 1 - b[3]]      # trái, trên, phải, dưới


def edge_touch(alpha, thr=200):
    """Số điểm đặc nằm trên từng mép — nhiều nghĩa là hình bị cắt cụt."""
    return {"T": int((alpha[0] > thr).sum()), "B": int((alpha[-1] > thr).sum()),
            "L": int((alpha[:, 0] > thr).sum()), "R": int((alpha[:, -1] > thr).sum())}


def thin(mask, width=4):
    """True nếu mảnh chỉ là nét mảnh (dày < width px): ăn mòn xong gần như không còn gì."""
    k = np.ones((3, 3), np.uint8)
    er = cv2.erode(mask.astype(np.uint8), k, iterations=max(1, width // 2))
    return er.sum() < max(12, 0.15 * mask.sum())


def stray_parts(a):
    """Tách mảnh rời khỏi hình chính. Trả về (nhãn, danh sách mảnh cần xoá, mảnh chính)."""
    solid = (a[..., 3] >= SOLID).astype(np.uint8)
    n, lab, st, _ = cv2.connectedComponentsWithStats(solid, 8)
    if n <= 1:
        return lab, [], 0
    main = 1 + int(np.argmax(st[1:, cv2.CC_STAT_AREA]))
    main_area = st[main, cv2.CC_STAT_AREA]
    drop = []
    for c in range(1, n):
        if c == main:
            continue
        area = st[c, cv2.CC_STAT_AREA]
        m = lab == c
        if area < 0.002 * main_area or thin(m):
            drop.append(c)
    return lab, drop, main


def bleed_rgb(a, iterations=6):
    """Loang màu mép ra vùng trong suốt (alpha=0) để lọc bilinear không sinh viền đen/trắng.
    Không đổi điểm nhìn thấy được."""
    a = a.copy()
    rgb = a[..., :3].astype(np.float32)
    known = a[..., 3] > 0
    k = np.ones((3, 3), np.float32)
    for _ in range(iterations):
        w = cv2.filter2D(known.astype(np.float32), -1, k, borderType=cv2.BORDER_CONSTANT)
        s = np.stack([cv2.filter2D(rgb[..., i] * known, -1, k, borderType=cv2.BORDER_CONSTANT) for i in range(3)], -1)
        grow = (~known) & (w > 0)
        rgb[grow] = s[grow] / w[grow][:, None]
        known = known | grow
    a[..., :3] = np.where((a[..., 3] == 0)[..., None], np.clip(rgb, 0, 255).astype(np.uint8), a[..., :3])
    return a


def repad(a, pad=PAD):
    b = bbox(a[..., 3], 0)
    if b is None:
        return a
    x0, y0, x1, y1 = b
    crop = a[y0:y1 + 1, x0:x1 + 1]
    out = np.zeros((crop.shape[0] + 2 * pad, crop.shape[1] + 2 * pad, 4), np.uint8)
    out[pad:pad + crop.shape[0], pad:pad + crop.shape[1]] = crop
    return out


def unsharp(a, amount, radius):
    """Làm nét chỉ trong lòng hình (cách mép 2px) để không đẻ quầng ở viền."""
    rgb = a[..., :3].astype(np.float32)
    blur = cv2.GaussianBlur(rgb, (0, 0), radius)
    sharp = np.clip(rgb + amount * (rgb - blur), 0, 255)
    inner = cv2.erode((a[..., 3] == 255).astype(np.uint8), np.ones((3, 3), np.uint8), iterations=2).astype(bool)
    out = a.copy()
    out[..., :3][inner] = sharp[inner].astype(np.uint8)
    return out


def sheet(items, path, cell=170, cols=10, label=True):
    """Bảng ảnh: mỗi hàng xen kẽ nền tối / nền sáng để soi viền. items = [(tên, mảng RGBA)]."""
    from PIL import ImageDraw
    rows = (len(items) + cols - 1) // cols
    im = Image.new("RGBA", (cols * cell, rows * (cell + 16)), (225, 225, 225, 255))
    d = ImageDraw.Draw(im)
    for i, (name, a) in enumerate(items):
        x, y = (i % cols) * cell, (i // cols) * (cell + 16)
        bg = (38, 40, 60, 255) if (i // cols) % 2 == 0 else (250, 250, 250, 255)
        d.rectangle([x, y, x + cell - 1, y + cell - 1], fill=bg)
        s = Image.fromarray(a, "RGBA")
        s.thumbnail((cell - 6, cell - 6), Image.LANCZOS)
        im.alpha_composite(s, (x + (cell - s.width) // 2, y + (cell - s.height) // 2))
        if label:
            d.text((x + 3, y + cell + 2), name, fill=(0, 0, 0, 255))
    os.makedirs(os.path.dirname(path), exist_ok=True)
    im.save(path)
