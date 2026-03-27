import sys, os, argparse, asyncio, io, fitz
from pptx import Presentation
from pptx.util import Pt, Inches
from pptx.dml.color import RGBColor
from PIL import Image, ImageDraw
import winsdk.windows.graphics.imaging as imaging
import winsdk.windows.media.ocr as ocr
import winsdk.windows.storage.streams as streams

FONT_MAP = {"Arial":"Arial","TimesNewRoman":"Times New Roman","MalgunGothic":"Malgun Gothic","NanumGothic":"Nanum Gothic"}

async def perform_windows_ocr(image_bytes):
    stream = streams.InMemoryRandomAccessStream()
    writer = streams.DataWriter(stream)
    writer.write_bytes(image_bytes)
    await writer.store_async(); await writer.flush_async(); stream.seek(0)
    decoder = await imaging.BitmapDecoder.create_async(stream)
    bitmap = await decoder.get_software_bitmap_async()
    engine = ocr.OcrEngine.try_create_from_user_profile_languages()
    if not engine: return []
    result = await engine.recognize_async(bitmap)
    return [{"text":w.text, "bbox":[w.bounding_rect.x, w.bounding_rect.y, w.bounding_rect.x+w.bounding_rect.width, w.bounding_rect.y+w.bounding_rect.height]} for l in result.lines for w in l.words]

def get_colors(image, bbox):
    w, h = image.size
    x0, y0, x1, y1 = [int(v) for v in bbox]
    # Sample background (ring)
    pixels = []
    p = 4
    for x in range(max(0, x0-p), min(w, x1+p), 2):
        for y in range(max(0, y0-p), y0, 2): pixels.append(image.getpixel((x,y)))
        for y in range(y1, min(h, y1+p), 2): pixels.append(image.getpixel((x,y)))
    bg = (sum(p[0] for p in pixels)//len(pixels), sum(p[1] for p in pixels)//len(pixels), sum(p[2] for p in pixels)//len(pixels)) if pixels else (255,255,255)
    # Sample font
    f_pixels = []
    for x in range(x0, x1):
        for y in range(y0, y1):
            pix = image.getpixel((x,y))
            if sum((pix[i]-bg[i])**2 for i in range(3))**0.5 > 35: f_pixels.append(pix)
    if not f_pixels: return bg, (0,0,0)
    f_pixels.sort(key=lambda x: sum(x), reverse=(sum(bg)/3.0 > 128))
    sample = f_pixels[:len(f_pixels)//4 or 1]
    font = (sum(p[0] for p in sample)//len(sample), sum(p[1] for p in sample)//len(sample), sum(p[2] for p in sample)//len(sample))
    return bg, font

async def pdf_to_pptx_hybrid(pdf_path, output_dir):
    doc = fitz.open(pdf_path)
    prs = Presentation()
    for page in doc:
        slide = prs.slides.add_slide(prs.slide_layouts[6])
        if page.number == 0: prs.slide_width, prs.slide_height = Inches(page.rect.width/72), Inches(page.rect.height/72)
        
        # 1. Native text
        items = []
        for b in page.get_text("dict")["blocks"]:
            if b["type"] == 0:
                for l in b["lines"]:
                    for s in l["spans"]:
                        items.append({"text":s["text"], "bbox":s["bbox"], "size":s["size"], "color":s["color"], "font":s["font"], "native":True})
        
        # 2. OCR for missing parts (High Res)
        zoom = 4.0
        pix = page.get_pixmap(matrix=fitz.Matrix(zoom, zoom))
        img_data = pix.tobytes("png")
        ocr_res = await perform_windows_ocr(img_data)
        pil_img = Image.open(io.BytesIO(img_data)).convert("RGB")
        draw = ImageDraw.Draw(pil_img)
        
        for o in ocr_res:
            ob = [v/zoom for v in o["bbox"]]
            # Filter duplicates (Overlap > 50%)
            if not any(max(0, min(ob[2], n["bbox"][2]) - max(ob[0], n["bbox"][0])) * max(0, min(ob[3], n["bbox"][3]) - max(ob[1], n["bbox"][1])) / ((ob[2]-ob[0])*(ob[3]-ob[1])) > 0.5 for n in items if n["native"]):
                bg, font = get_colors(pil_img, o["bbox"])
                items.append({"text":o["text"], "bbox":ob, "size":(o["bbox"][3]-o["bbox"][1])/zoom, "rgb":font, "native":False})
                draw.rectangle([v-1 for v in o["bbox"]], fill=bg) # Erase from bg
        
        # Save Background
        bg_io = io.BytesIO(); pil_img.save(bg_io, format="PNG"); bg_io.seek(0)
        slide.shapes.add_picture(bg_io, 0, 0, width=prs.slide_width, height=prs.slide_height)
        
        # Add Text Boxes
        for it in items:
            tx = it["text"].strip()
            if not tx: continue
            b = it["bbox"]
            shape = slide.shapes.add_textbox(Inches(b[0]/72), Inches(b[1]/72), Inches((b[2]-b[0])/72), Inches((b[3]-b[1])/72))
            run = shape.text_frame.paragraphs[0].add_run()
            run.text = tx; run.font.size = Pt(it["size"])
            if "rgb" in it: run.font.color.rgb = RGBColor(*it["rgb"])
            elif "color" in it: 
                c = it["color"]; run.font.color.rgb = RGBColor((c>>16)&0xFF, (c>>8)&0xFF, c&0xFF)
            
    out_p = os.path.join(output_dir, os.path.splitext(os.path.basename(pdf_path))[0] + "_hybrid.pptx")
    prs.save(out_p); print(f"DONE: {out_p}")

if __name__ == "__main__":
    parser = argparse.ArgumentParser(); parser.add_argument("--pdf", required=True); parser.add_argument("--outdir", required=True)
    args = parser.parse_args(); asyncio.run(pdf_to_pptx_hybrid(args.pdf, args.outdir))
