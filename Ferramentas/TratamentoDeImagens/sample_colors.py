"""Sample HSV/RGB stats from regions of an image."""
import sys
import numpy as np
import cv2
from PIL import Image

def stats(arr, label):
    rgb = arr[..., :3]
    a = arr[..., 3]
    visible = a >= 200
    if not visible.any():
        print(f"{label}: no visible")
        return
    bgr = rgb[visible][:, ::-1]  # PIL is RGB, cv2 wants BGR
    bgr_img = bgr.reshape(-1, 1, 3).astype(np.uint8)
    hsv = cv2.cvtColor(bgr_img, cv2.COLOR_BGR2HSV).reshape(-1, 3)
    luma = (0.299*rgb[visible,0] + 0.587*rgb[visible,1] + 0.114*rgb[visible,2])
    print(f"{label} ({visible.sum()} px):")
    print(f"  R={rgb[visible,0].mean():.0f}±{rgb[visible,0].std():.0f}  "
          f"G={rgb[visible,1].mean():.0f}±{rgb[visible,1].std():.0f}  "
          f"B={rgb[visible,2].mean():.0f}±{rgb[visible,2].std():.0f}  "
          f"luma={luma.mean():.0f}")
    print(f"  H={hsv[:,0].mean():.0f}±{hsv[:,0].std():.0f}  "
          f"S={hsv[:,1].mean():.0f}±{hsv[:,1].std():.0f}  "
          f"V={hsv[:,2].mean():.0f}±{hsv[:,2].std():.0f}")

def main():
    path = sys.argv[1]
    arr = np.array(Image.open(path).convert("RGBA"))
    h, w, _ = arr.shape
    print(f"Image: {w}x{h}")
    # Foreground area: tree stump on left
    stats(arr[h//2-50:h//2+50, 100:300], "Stump (left center)")
    # Foreground: dirt
    stats(arr[h-50:h-10, w//2-100:w//2+100], "Dirt (bottom mid)")
    # Background blue silhouette area: around middle of image, mid-height
    stats(arr[h//2:h-100, int(w*0.40):int(w*0.55)], "Mid-back trees area")
    # Right dead trees
    stats(arr[h//4:h*3//4, int(w*0.85):int(w*0.95)], "Right dead trees")
    # Brazier fire (orange)
    stats(arr[int(h*0.5):int(h*0.7), 220:280], "Brazier fire")

if __name__ == "__main__":
    main()
