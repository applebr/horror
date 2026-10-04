#!/usr/bin/env python3
"""
BDF 비트맵 폰트를 게임 엔진이 읽는 작은 바이너리(.bytes)로 굽는 스크립트.

사용법:
    python3 bake_font.py <input.bdf> <output.bytes>

갈무리(Galmuri) 폰트 BDF는 npm 패키지에서 받을 수 있다:
    npm pack galmuri && tar xzf galmuri-*.tgz   # package/dist/Galmuri11.bdf 등

출력 포맷 (리틀 엔디언):
    "GFNT" u8 version u8 ascent u8 descent u8 lineHeight u32 glyphCount
    glyph * N (코드포인트 오름차순):
        u32 codepoint, u8 advance, u8 w, u8 h, i8 xoff, i8 yoff,
        h * ceil(w/8) bytes (행 단위, MSB가 왼쪽 픽셀)
"""
import struct
import sys

RANGES = [
    (0x0020, 0x007E),  # ASCII
    (0x00A0, 0x00FF),  # Latin-1 (· × 등)
    (0x2010, 0x206F),  # 일반 문장부호 (… ‘ ’ “ ” — 등)
    (0x2190, 0x21FF),  # 화살표
    (0x2460, 0x24FF),  # 원문자 숫자
    (0x2500, 0x257F),  # 박스 그리기
    (0x2580, 0x25FF),  # 블록 / 도형 (■ □ ● ○ ▶)
    (0x2600, 0x26FF),  # 기타 기호 (★ ♥ ♪)
    (0x2700, 0x27BF),  # 딩뱃 (✂)
    (0x3000, 0x303F),  # CJK 기호 (「」『』)
    (0x3130, 0x318F),  # 한글 호환 자모 (ㅋ ㅠ)
    (0xAC00, 0xD7A3),  # 한글 음절 전체
    (0xFF01, 0xFF5E),  # 전각 문자
]


def wanted(cp):
    for a, b in RANGES:
        if a <= cp <= b:
            return True
    return False


def parse_bdf(path):
    glyphs = {}
    ascent = descent = 0
    with open(path, "r", encoding="latin-1") as f:
        lines = f.read().splitlines()
    i = 0
    n = len(lines)
    while i < n:
        line = lines[i]
        if line.startswith("FONT_ASCENT"):
            ascent = int(line.split()[1])
        elif line.startswith("FONT_DESCENT"):
            descent = int(line.split()[1])
        elif line.startswith("STARTCHAR"):
            cp = None
            adv = 0
            bbx = (0, 0, 0, 0)
            rows = []
            i += 1
            while i < n and not lines[i].startswith("ENDCHAR"):
                l = lines[i]
                if l.startswith("ENCODING"):
                    cp = int(l.split()[1])
                elif l.startswith("DWIDTH"):
                    adv = int(l.split()[1])
                elif l.startswith("BBX"):
                    p = l.split()
                    bbx = (int(p[1]), int(p[2]), int(p[3]), int(p[4]))
                elif l.startswith("BITMAP"):
                    i += 1
                    while i < n and not lines[i].startswith("ENDCHAR"):
                        rows.append(lines[i].strip())
                        i += 1
                    break
                i += 1
            if cp is not None and cp >= 0 and wanted(cp):
                w, h, xo, yo = bbx
                bpr = (w + 7) // 8
                data = bytearray()
                for r in rows[:h]:
                    v = int(r, 16) if r else 0
                    hexbytes = len(r) // 2
                    raw = v.to_bytes(max(hexbytes, 1), "big")
                    raw = raw[:bpr] if len(raw) >= bpr else raw + bytes(bpr - len(raw))
                    data += raw
                while len(data) < h * bpr:
                    data += bytes(bpr)
                glyphs[cp] = (adv, w, h, xo, yo, bytes(data))
        i += 1
    return ascent, descent, glyphs


def main():
    if len(sys.argv) != 3:
        print(__doc__)
        sys.exit(1)
    ascent, descent, glyphs = parse_bdf(sys.argv[1])
    out = bytearray(b"GFNT")
    out += struct.pack("<BBBBI", 1, ascent, descent, ascent + descent, len(glyphs))
    for cp in sorted(glyphs):
        adv, w, h, xo, yo, data = glyphs[cp]
        out += struct.pack("<IBBBbb", cp, adv, w, h, xo, yo)
        out += data
    with open(sys.argv[2], "wb") as f:
        f.write(out)
    print(f"{sys.argv[1]} -> {sys.argv[2]}: {len(glyphs)} glyphs, {len(out)} bytes, ascent={ascent} descent={descent}")


if __name__ == "__main__":
    main()
