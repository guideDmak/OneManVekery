# 04 Views and CSS Layout

เอกสารนี้อธิบาย markup และ CSS ของหน้าแรก/หน้าสินค้า โดยเน้นส่วนที่เพิ่งแก้: hero image, mobile height, รูปมน, 4x2 best seller และการจัดแถวสุดท้ายให้อยู่กลาง

## ไฟล์ที่เกี่ยวข้อง

| ไฟล์ | หน้าที่ |
| --- | --- |
| `Views/Home/Index.cshtml` | markup หน้าแรก |
| `Views/Home/Shop.cshtml` | markup หน้าสินค้า |
| `wwwroot/css/site.css` | style หลัก |

## โครงสร้างหน้าแรก

`Views/Home/Index.cshtml`

```text
HomeIndexViewModel
  -> heroProducts
  -> store-hero
  -> range-section
  -> catalog-section
```

### Hero section

ตำแหน่ง: `Views/Home/Index.cshtml:10-88`

ส่วนนี้แสดง carousel สินค้าใหม่

| บรรทัด | หน้าที่ |
| --- | --- |
| 10 | เปิด `<section class="store-hero">` |
| 12 | `.store-hero-panel` เป็นกรอบรวมซ้าย/ขวา |
| 13 | `.store-hero-art` ฝั่งรูป |
| 15 | `.store-hero-visual-shell` กรอบรูป/ carousel |
| 16-19 | ถ้าไม่มีสินค้า แสดง fallback image |
| 22 | เปิด Bootstrap carousel |
| 23-33 | carousel indicators |
| 35-51 | carousel items |
| 40-43 | รูปสินค้า ใช้ class `hero-main-image hero-arrival-image` |
| 44-48 | caption ชื่อสินค้า หมวด ราคา |
| 53-60 | ปุ่ม prev/next |
| 76-85 | hero copy ฝั่งขวา |

## Review finding เรื่อง mobile hero image

ปัญหาเดิม:

```css
.hero-main-image,
.hero-arrival-image {
  min-height: 500px;
}
```

ถ้า rule นี้อยู่ท้ายไฟล์หลัง media query, mobile breakpoint ลดความสูง wrapper แล้ว แต่ image ยังบังคับ `min-height: 500px` อยู่ ทำให้ viewport 390px สูงเกินและ text ถูกเบียด/clip

แนวทางแก้ปัจจุบัน:

- ใช้ CSS variable `--hero-media-height` เป็น source of truth
- wrapper และ image ใช้ความสูงจาก variable เดียวกัน
- media query เปลี่ยน variable ที่ `.store-hero-panel`
- `.hero-arrival-image` ตั้ง `min-height: 0` เพื่อไม่ฝืน breakpoint
- fallback `.hero-main-image` ที่ไม่ใช่ carousel ใช้ `min-height: var(--hero-media-height)`

## Hero CSS ทีละส่วน

ตำแหน่ง: `wwwroot/css/site.css:8237-8315`

### `store-hero-panel`

```css
.store-hero-panel {
  --hero-media-height: 500px;
  --hero-media-inset: clamp(12px, 1.6vw, 22px);
  --hero-image-radius: 20px;
  align-items: stretch;
}
```

คำอธิบาย:

- `--hero-media-height` ความสูงหลักของรูป hero บน desktop
- `--hero-media-inset` ระยะ padding ใน slide ปรับตาม viewport แต่ไม่ต่ำกว่า 12px และไม่เกิน 22px
- `--hero-image-radius` ความมนของรูป
- `align-items: stretch` ทำให้ฝั่งซ้าย/ขวายืดให้สัมพันธ์กัน

### `.store-hero-art, .store-hero-card`

```css
.store-hero-art,
.store-hero-card {
  height: var(--hero-media-height);
  min-height: var(--hero-media-height);
}
```

คำอธิบาย:

- ฝั่งรูปและฝั่งข้อความใช้ความสูงเดียวกัน
- `height` ตั้งขนาดจริง
- `min-height` กันไม่ให้เล็กกว่าค่าที่กำหนด
- เพราะใช้ variable จึงเปลี่ยนตาม media query ได้

### `.store-hero-art`

```css
.store-hero-art {
  overflow: hidden;
}
```

กันรูปหรือ carousel หลุดออกนอกกรอบ

### shell และ carousel wrappers

```css
.store-hero-visual-shell,
.hero-arrival-carousel,
.hero-arrival-inner,
.hero-arrival-slide {
  width: 100%;
  height: var(--hero-media-height);
  min-height: var(--hero-media-height);
}
```

ทุกชั้นใน carousel ใช้ขนาดเดียวกัน ไม่ปล่อยให้ slide หรือ inner มีขนาดไม่ตรงกับ shell

### `.store-hero-visual-shell`

```css
.store-hero-visual-shell {
  display: grid;
  place-items: center;
}
```

ใช้ grid เพื่อจัดเนื้อหาด้านในให้อยู่กลางทั้งแนวตั้งและแนวนอน

### `.hero-arrival-slide`

```css
.hero-arrival-slide {
  padding: var(--hero-media-inset);
  box-sizing: border-box;
  overflow: hidden;
}
```

- padding คือกรอบบาง ๆ รอบรูป
- `box-sizing: border-box` ทำให้ padding ถูกนับรวมใน height/width ไม่ดันขนาดเกิน
- `overflow: hidden` ตัดส่วนที่เกิน slide

### `.hero-main-image, .hero-arrival-image`

```css
.hero-main-image,
.hero-arrival-image {
  width: 100%;
  height: 100%;
  box-sizing: border-box;
}
```

รูปกินพื้นที่เต็ม parent และไม่เพิ่มขนาดเกินเพราะ padding/border

### `.hero-arrival-image`

```css
.hero-arrival-image {
  min-height: 0;
  padding: 0;
  border-radius: var(--hero-image-radius);
  object-fit: cover;
}
```

นี่คือจุดแก้ review finding:

- `min-height: 0` ปล่อยให้รูปเล็กลงตาม wrapper บน mobile
- `padding: 0` เพราะ padding ย้ายไปอยู่ที่ slide แล้ว
- `border-radius` ทำให้รูปมน
- `object-fit: cover` ให้รูปเต็มกรอบและ crop แทนการบิดสัดส่วน

### fallback image selector

```css
.store-hero-visual-shell > .hero-main-image:not(.hero-arrival-image) {
  width: 100%;
  height: 100%;
  min-height: var(--hero-media-height);
  max-width: none;
  max-height: none;
  padding: var(--hero-media-inset);
  border-radius: var(--hero-image-radius);
  object-fit: contain;
}
```

ใช้เฉพาะกรณีไม่มี carousel และแสดง fallback `.hero-main-image` ตรง ๆ

- `:not(.hero-arrival-image)` กันไม่ให้ rule นี้ไปชน carousel image
- `min-height` ใช้ variable จึงลดตาม breakpoint ได้
- `object-fit: contain` ให้ภาพ fallback เห็นครบ ไม่ crop

## Hero mobile breakpoints

ตำแหน่ง: `wwwroot/css/site.css:8424-8473`

### Tablet

```css
@media (max-width: 991.98px) {
  .store-hero-panel {
    --hero-media-height: 420px;
  }

  .store-hero-card {
    height: auto;
    min-height: auto;
  }
}
```

ความหมาย:

- เมื่อ viewport ไม่เกิน 991.98px ความสูง hero media ลดจาก 500px เป็น 420px
- ฝั่งข้อความไม่ถูกบังคับสูงเท่าฝั่งรูปแล้ว เพราะ layout เริ่มแคบ

### Mobile

```css
@media (max-width: 767.98px) {
  .store-hero-panel {
    --hero-media-height: 340px;
    --hero-media-inset: 10px;
    --hero-image-radius: 16px;
  }
}
```

ความหมาย:

- mobile ลดความสูงรูปเหลือ 340px
- ลด padding รอบรูปเหลือ 10px
- ลด radius เหลือ 16px
- เพราะ image ใช้ height จาก parent และ `min-height: 0` จึงไม่ค้างที่ 500px

## Range section

ตำแหน่ง: `Views/Home/Index.cshtml:90-120`

ส่วนนี้คือ "เลือกดูตามสไตล์ที่ชอบ"

ข้อมูลมาจาก `Model.Categories`

```text
CategoryCardViewModel
  -> range-card
     -> range-visual image + badge
     -> range-card-copy
     -> range-best-seller
```

## Catalog section 4x2

ตำแหน่ง: `Views/Home/Index.cshtml:122-170`

ส่วนนี้คือ "สินค้าขายดี"

ข้อมูลมาจาก:

```csharp
BuildBestSellingProducts(products, salesLookup, 8)
```

จึงมี 8 card และ CSS desktop ให้ card กว้าง 1/4 ของแถว กลายเป็น 4x2

## Center incomplete rows CSS

ตำแหน่ง: `wwwroot/css/site.css:8475-8513`

```css
.range-grid,
.catalog-grid {
  display: flex;
  flex-wrap: wrap;
  justify-content: center;
  align-items: stretch;
}
```

อธิบาย:

- เปลี่ยน grid เป็น flex layout
- `flex-wrap: wrap` ให้ card ขึ้นแถวใหม่ได้
- `justify-content: center` ทำให้แถวสุดท้ายที่ไม่เต็มอยู่กลาง
- `align-items: stretch` ทำให้ card ในแถวเดียวกันสูงเท่ากัน

### Range card width

```css
.range-card {
  flex: 0 1 calc((100% - 44px) / 3);
  max-width: calc((100% - 44px) / 3);
}
```

อธิบาย:

- `.range-grid` gap เดิมคือ 22px
- 3 columns มีช่องว่าง 2 ช่อง = 44px
- ความกว้าง card = `(100% - 44px) / 3`
- `flex: 0 1 ...` หมายถึงไม่ grow, shrink ได้, basis ตามสูตร
- `max-width` กัน card ขยายเกินสูตร

### Catalog card width

```css
.catalog-card {
  flex: 0 1 calc((100% - 66px) / 4);
  max-width: calc((100% - 66px) / 4);
  height: auto;
}
```

อธิบาย:

- 4 columns มีช่องว่าง 3 ช่อง = 66px
- card กว้าง `(100% - 66px) / 4`
- ทำให้ desktop เป็น 4 columns
- ถ้ามี 8 ชิ้นจะออกมา 4x2
- ถ้ามี 6 หรือ 10 ชิ้น แถวสุดท้ายจะอยู่กลาง

### Hidden card

```css
.catalog-card.is-hidden {
  display: none;
}
```

ใช้กับ search/filter ในหน้าสินค้า ถ้า JS ซ่อน card ต้องหายจาก flex layout จริง เพื่อให้ card ที่เหลือจัดกลางใหม่

### Tablet layout

```css
@media (max-width: 991.98px) {
  .range-card,
  .catalog-card {
    flex-basis: calc((100% - 22px) / 2);
    max-width: calc((100% - 22px) / 2);
  }
}
```

บน tablet ใช้ 2 columns เพราะพื้นที่แคบลง

### Mobile layout

```css
@media (max-width: 767.98px) {
  .range-card,
  .catalog-card {
    flex-basis: 100%;
    max-width: 100%;
  }
}
```

บน mobile ใช้ 1 column เพื่อไม่ให้ข้อความและปุ่มแน่นเกิน

## Product image sizing

ตำแหน่ง: `wwwroot/css/site.css:8326-8357`

### `.catalog-thumb`

```css
.catalog-thumb {
  display: grid;
  place-items: center;
  height: 240px;
  min-height: 0;
  padding: 12px;
  box-sizing: border-box;
}
```

หน้าที่:

- ทำกรอบรูปสินค้าให้ขนาดคงที่
- จัดรูปอยู่กลาง
- padding ทำให้รูปไม่ชิดขอบ
- `box-sizing` กัน padding ดันขนาดเกิน

### `.catalog-image`

```css
.catalog-image {
  width: 100%;
  height: 100%;
  max-width: none;
  max-height: none;
  box-sizing: border-box;
  padding: 0;
  border-radius: 16px;
  object-fit: contain;
}
```

หน้าที่:

- รูปกินพื้นที่เต็มกรอบ
- `object-fit: contain` เห็นรูปครบ ไม่ crop
- `border-radius: 16px` ทำให้รูปมน
- เหมาะกับรูปสินค้าที่สัดส่วนไม่เท่ากัน

## หน้าสินค้าได้รับผลด้วยอย่างไร

`Views/Home/Shop.cshtml:46` ใช้:

```html
<div class="catalog-grid" data-product-list>
```

ดังนั้น CSS `.catalog-grid` ชุดเดียวกันส่งผลกับ:

- section "สินค้าขายดี" ในหน้าแรก
- product list ในหน้าสินค้า

ผลลัพธ์:

- desktop เป็น 4 columns
- tablet เป็น 2 columns
- mobile เป็น 1 column
- แถวสุดท้ายจัดกลางเอง
- ถ้า filter แล้วเหลือ 1 หรือ 2 card จะไม่ชิดซ้าย

## Checklist เวลาแก้ layout รอบต่อไป

- ถ้าเปลี่ยนจำนวน card ต่อแถว ต้องแก้สูตร gap ด้วย เช่น 4 columns = gap 3 ช่อง
- ถ้าเปลี่ยน `gap` ของ `.catalog-grid` ต้องแก้ `66px` ให้สัมพันธ์กัน
- ถ้าอยากให้ best seller เป็นจำนวนอื่น แก้ `BuildBestSellingProducts(..., N)` ใน `Index()`
- ถ้ารูป hero mobile สูงผิด ให้เช็ก `--hero-media-height` ก่อน
- ถ้ารูป hero ยังสูงเกิน ให้เช็กว่า image มี `min-height` hard-coded หลัง media query หรือไม่

