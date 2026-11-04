"""Regenerate the app's original scan icon. Development-only; requires Pillow."""
from pathlib import Path
from PIL import Image, ImageDraw

root = Path(__file__).resolve().parent.parent
image = Image.new("RGBA", (256, 256), (0, 0, 0, 0))
draw = ImageDraw.Draw(image)
draw.rounded_rectangle((4, 4, 252, 252), radius=60, fill="#007AFF")
for points in (
    [(91, 64), (64, 64), (64, 91)],
    [(165, 64), (192, 64), (192, 91)],
    [(64, 165), (64, 192), (91, 192)],
    [(192, 165), (192, 192), (165, 192)],
):
    draw.line(points, fill="white", width=12, joint="curve")
    for x, y in (points[0], points[-1]):
        draw.ellipse((x - 6, y - 6, x + 6, y + 6), fill="white")
draw.rounded_rectangle((91, 105, 165, 117), radius=6, fill="white")
draw.rounded_rectangle((91, 139, 146, 151), radius=6, fill="#B8DCFF")
target = root / "Translate" / "Assets"
target.mkdir(parents=True, exist_ok=True)
image.save(target / "App.ico", sizes=[(16, 16), (24, 24), (32, 32), (48, 48), (64, 64), (128, 128), (256, 256)])
