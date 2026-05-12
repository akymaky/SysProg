#!/usr/bin/env python3
import os
import random
import string
import requests
from pathlib import Path

import requests
from dotenv import load_dotenv

load_dotenv()
GIPHY_API_KEY = os.getenv("GIPHY_API_KEY")

if not GIPHY_API_KEY:
    raise RuntimeError("Missing GIPHY_API_KEY in .env")

# Config
ROOT_DIR = Path("public")
GIF_COUNT = 50
MAX_DEPTH = 5
MAX_SUBFOLDERS_PER_GIF = 5

# Optional: tags make results less repetitive
TAGS = ["cat", "dog", "meme", "funny", "reaction", "gaming", "anime", "dance"]

def random_name(length=8):
    return ''.join(random.choices(string.ascii_lowercase + string.digits, k=length))

def random_folder_path(root: Path, max_depth: int) -> Path:
    depth = random.randint(0, max_depth)
    path = root
    for _ in range(depth):
        path /= f"folder_{random_name(6)}"
    return path

def fetch_random_gif_url():
    tag = random.choice(TAGS)
    r = requests.get(
        "https://api.giphy.com/v1/gifs/random",
        params={"api_key": GIPHY_API_KEY, "tag": tag},
        timeout=20,
    )
    r.raise_for_status()
    data = r.json()["data"]

    # Try the direct original GIF first
    return data["images"]["original"]["url"]

def download_file(url: str, dest: Path):
    with requests.get(url, stream=True, timeout=30) as r:
        r.raise_for_status()
        with open(dest, "wb") as f:
            for chunk in r.iter_content(chunk_size=8192):
                if chunk:
                    f.write(chunk)

def main():
    ROOT_DIR.mkdir(parents=True, exist_ok=True)

    for i in range(GIF_COUNT):
        folder = random_folder_path(ROOT_DIR, MAX_DEPTH)
        folder.mkdir(parents=True, exist_ok=True)

        file_name = f"gif_{i:03}_{random_name(5)}.gif"
        dest = folder / file_name

        try:
            gif_url = fetch_random_gif_url()
            download_file(gif_url, dest)
            print(f"[OK] {dest}")
        except Exception as e:
            print(f"[FAIL] {dest}: {e}")

if __name__ == "__main__":
    main()
