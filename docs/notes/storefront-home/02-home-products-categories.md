# 02 Home, Products, Categories

เอกสารนี้อธิบาย flow หน้าแรก, สินค้าขายดี, หมวดสินค้า, new arrivals, และ view model ที่เกี่ยวข้อง

## ไฟล์ที่เกี่ยวข้อง

| ไฟล์ | หน้าที่ |
| --- | --- |
| `Controllers/HomeController.cs` | action `Index()` และ `Shop()` |
| `Controllers/Home/CartCatalog.cs` | โหลดสินค้า, โหลดสินค้าใหม่, สร้าง sales lookup, จัดการ cart |
| `Controllers/Home/ProductsOrders.cs` | map product, build category cards, build best selling products |
| `ViewModel/StorefrontViewModels.cs` | shape ของข้อมูลที่ view ใช้ |
| `Views/Home/Index.cshtml` | markup หน้าแรก |
| `Views/Home/Shop.cshtml` | markup หน้าสินค้า |
| `wwwroot/css/site.css` | layout hero, range cards, catalog cards |

## Flow หน้าแรกละเอียด

```text
GET /
  -> HomeController.Index()
    -> GetProducts()
      -> SELECT active products
      -> Include Category
      -> OrderBy Name
      -> MapProduct()
    -> BuildProductSalesLookup()
      -> SELECT order items ที่มี ProductId
      -> ดึง Qty, LineTotal, OrderStatus
      -> ตัด refunded/cancelled
      -> group by ProductId
    -> BuildCategoryCards(products, salesLookup)
      -> group product ตาม category
      -> ในแต่ละหมวด เลือกสินค้าที่ขายดีที่สุด
    -> BuildBestSellingProducts(products, salesLookup, 8)
      -> จัดอันดับยอดขายรวมทั้งร้าน
      -> เอา 8 ชิ้น
    -> GetNewArrivalProducts()
      -> เอาสินค้า active ล่าสุด 3 ชิ้น
    -> return HomeIndexViewModel
  -> Views/Home/Index.cshtml
```

## `HomeIndexViewModel`

ตำแหน่ง: `ViewModel/StorefrontViewModels.cs:5-12`

| บรรทัด | property | ใช้ที่ไหน | ความหมาย |
| --- | --- | --- | --- |
| 7 | `Categories` | `Index.cshtml:98-118` | หมวดสินค้า พร้อมสินค้าขายดีในหมวด |
| 9 | `Products` | `Index.cshtml:129-164` | สินค้าขายดีรวมทั้งร้าน 8 ชิ้น |
| 11 | `NewArrivals` | `Index.cshtml:5-72` | สินค้าใหม่ล่าสุดใน hero carousel |

## `ProductCardViewModel`

ตำแหน่ง: `ViewModel/StorefrontViewModels.cs:220-241`

| บรรทัด | property | ความหมาย |
| --- | --- | --- |
| 222 | `ProductId` | id ของสินค้า ใช้ส่งเข้า form add-to-cart |
| 224 | `Name` | ชื่อสินค้า |
| 226 | `Category` | ชื่อหมวดสินค้า |
| 228 | `Description` | คำอธิบายที่ผ่านการ normalize แล้ว |
| 230 | `Price` | ราคาปัจจุบัน |
| 232 | `OriginalPrice` | ราคาเดิม ถ้ามี |
| 234 | `Badge` | ป้าย เช่น `หมดแล้ว`, `ใกล้หมด` |
| 236 | `ThemeKey` | ชุด theme สี/ภาพของ card |
| 238 | `ImagePath` | path รูปที่ view ใช้ |
| 240 | `IsSoldOut` | สินค้าหมดหรือไม่ |

## `CategoryCardViewModel`

ตำแหน่ง: `ViewModel/StorefrontViewModels.cs:197-218`

| บรรทัด | property | ความหมาย |
| --- | --- | --- |
| 199 | `Title` | ชื่อหมวด เช่น Cake, Bakery |
| 201 | `Subtitle` | ข้อความนับจำนวนเมนูในหมวด |
| 203 | `ThemeKey` | theme ของ card ใช้จากสินค้าขายดีในหมวด |
| 205 | `ImagePath` | รูปของสินค้าขายดีในหมวด |
| 207 | `ItemCount` | จำนวนสินค้าในหมวด |
| 209 | `FeaturedProductName` | ชื่อสินค้าขายดีในหมวด |
| 211 | `FeaturedProductDescription` | คำอธิบายของสินค้าขายดี |
| 213 | `FeaturedProductPriceLabel` | ราคาแบบพร้อมแสดงผล เช่น `135 ฿` |
| 215 | `FeaturedProductSalesLabel` | ข้อความยอดขาย เช่น `ขายแล้ว 4 ชิ้น` |
| 217 | `FeaturedProductBadge` | badge เช่น `ขายดี` หรือ `แนะนำ` |

## `GetProducts()`

ตำแหน่ง: `Controllers/Home/CartCatalog.cs:15-25`

หน้าที่: โหลดสินค้าที่เปิดขายทั้งหมด

อธิบายทีละบรรทัด:

| บรรทัด | อธิบาย |
| --- | --- |
| 15 | ประกาศ method คืนค่า list ของ `ProductCardViewModel` |
| 17 | เริ่ม query จาก table products |
| 18 | `AsNoTracking()` เพราะอ่านอย่างเดียว ไม่ต้อง track entity |
| 19 | `Include(product => product.Category)` โหลด category มาด้วย |
| 20 | เอาเฉพาะสินค้าที่ active |
| 21 | เรียงตามชื่อสินค้า |
| 22 | execute query เป็น list entity |
| 23 | map entity เป็น view model ด้วย `MapProduct` |
| 24 | materialize เป็น list |

## `GetNewArrivalProducts()`

ตำแหน่ง: `Controllers/Home/CartCatalog.cs:27-39`

หน้าที่: โหลดสินค้า active ล่าสุด 3 ชิ้นให้ hero carousel

หลักการ:

- query จาก product active
- include category
- sort by `CreatedAt DESC`
- ถ้าเวลาสร้างเท่ากัน sort by `Id DESC`
- take 3
- map เป็น `ProductCardViewModel`

## `BuildProductSalesLookup()`

ตำแหน่ง: `Controllers/Home/CartCatalog.cs:41-62`

หน้าที่: สร้าง dictionary ยอดขายรวมต่อสินค้า

โครงสร้าง output:

```text
ProductId string -> ProductSalesSummary(UnitsSold, Revenue)
```

อธิบายทีละบรรทัด:

| บรรทัด | อธิบาย |
| --- | --- |
| 41 | method คืนค่า `IReadOnlyDictionary<string, ProductSalesSummary>` |
| 43 | query จาก `OrderItems` |
| 44 | อ่านอย่างเดียว ไม่ track entity |
| 45 | เอาเฉพาะ order item ที่มี product id |
| 46-52 | select เฉพาะ field ที่ต้องใช้: product id, qty, line total, order status |
| 53 | execute query ก่อน เพราะถัดไปมี normalize status ใน C# |
| 54 | ตัด order ที่ status เป็น refunded/cancelled |
| 55 | group ตาม `ProductId` |
| 56-61 | แปลงเป็น dictionary key คือ product id string |
| 59 | sum จำนวนชิ้นที่ขาย |
| 60 | sum รายได้รวมของ product นั้น |
| 61 | ใช้ comparer แบบ ignore case |

เหตุผลที่ไม่เอา refunded/cancelled:

- ไม่ควรนับสินค้าที่คืนเงินแล้ว
- ไม่ควรนับ order ที่ยกเลิก

## `MapProduct(Product product)`

ตำแหน่ง: `Controllers/Home/ProductsOrders.cs:143-166`

หน้าที่: แปลง entity จาก database ให้เป็น card ที่ view ใช้ง่าย

ลำดับ:

1. อ่าน metadata จาก `Description`
2. resolve category ถ้าไม่มีให้ fallback เป็น `Bakery`
3. resolve theme key จาก category/name/image path
4. เช็กว่าสินค้าหมดหรือไม่
5. สร้าง `ProductCardViewModel`

จุดสำคัญ:

- `Badge` เป็น `หมดแล้ว` ถ้า stock <= 0
- ถ้าไม่หมดแต่ stock <= reorder level จะเป็น `ใกล้หมด`
- `ImagePath` ผ่าน `NormalizeProductImagePath` เพื่อ fallback เป็น theme image ถ้าไม่มีรูป

## `BuildCategoryCards()`

ตำแหน่ง: `Controllers/Home/ProductsOrders.cs:394-443`

หน้าที่: สร้าง card หมวดและเลือกสินค้าขายดีประจำหมวด

Flow:

```text
products
  -> remove empty category
  -> group by category
  -> order category by item count desc, then category name
  -> for each group:
       map product + sales summary
       sort by units sold desc
       sort by revenue desc
       sort by sold-out status
       sort by name
       pick first as featured product
       create CategoryCardViewModel
```

อธิบายบรรทัดสำคัญ:

| บรรทัด | อธิบาย |
| --- | --- |
| 394-396 | method รับ product list และ sales lookup |
| 399 | ตัดสินค้าที่ category ว่าง |
| 400 | group ตาม category แบบ ignore case |
| 401 | หมวดที่มีสินค้ามากขึ้นก่อน |
| 402 | ถ้าจำนวนเท่ากัน เรียงตามชื่อหมวด |
| 405-416 | ในแต่ละหมวด เอา product ไปจับคู่กับยอดขาย |
| 408-410 | ถ้าไม่มียอดขาย ให้ใช้ `ProductSalesSummary(0, 0)` |
| 418 | เรียงตามจำนวนขายมากสุด |
| 419 | ถ้าจำนวนขายเท่ากัน เรียงตาม revenue มากสุด |
| 420 | ให้สินค้าที่ยังไม่หมดมาก่อนสินค้าหมด |
| 421 | ถ้ายังเท่ากัน เรียงชื่อ |
| 423 | ตัวแรกหลัง sort คือ featured product |
| 424 | นับจำนวนสินค้าในหมวด |
| 428-441 | map เป็น `CategoryCardViewModel` |
| 437-439 | ถ้ามียอดขาย แสดง `ขายแล้ว N ชิ้น` ถ้าไม่มีใช้ `เมนูเด่นของหมวดนี้` |
| 440 | badge เป็น `ขายดี` ถ้ามีขายแล้ว ไม่งั้น `แนะนำ` |

## `BuildBestSellingProducts()`

ตำแหน่ง: `Controllers/Home/ProductsOrders.cs:445-470`

หน้าที่: สร้าง list สินค้าขายดีรวมทั้งร้าน โดยไม่สนหมวด

ในหน้าแรกเรียกด้วย `take = 8` เพื่อทำ layout 4x2

อธิบายทีละบรรทัด:

| บรรทัด | อธิบาย |
| --- | --- |
| 445-448 | method รับ products, sales lookup, และจำนวนที่ต้องการ |
| 451 | loop ทุก product |
| 453-455 | หา sales summary ของ product นั้น ถ้าไม่มีคือขาย 0 |
| 457-461 | สร้าง anonymous object `{ Product, Sales }` เพื่อ sort จากยอดขายได้ |
| 464 | เรียงจำนวนขายมากสุดก่อน |
| 465 | ถ้าจำนวนขายเท่ากัน เรียงรายได้มากสุด |
| 466 | ถ้ายังเท่ากัน ให้สินค้าที่ยังไม่หมดมาก่อน |
| 467 | ถ้ายังเท่ากัน เรียงชื่อสินค้า |
| 468 | ตัดเหลือ `take` ชิ้น เช่น 8 |
| 469 | เอาเฉพาะ `Product` กลับไปให้ view |
| 470 | materialize เป็น list |

## หน้าแรกใน `Index.cshtml`

ตำแหน่ง: `Views/Home/Index.cshtml`

### บรรทัด 1-8: เตรียม model และ hero products

| บรรทัด | อธิบาย |
| --- | --- |
| 1 | view รับ `HomeIndexViewModel` |
| 4 | ตั้ง title |
| 5-7 | ถ้ามี `NewArrivals` ใช้เป็น hero carousel ถ้าไม่มีใช้สินค้าขายดี 3 ชิ้นแรกแทน |

### บรรทัด 10-88: Hero

- แสดงภาพสินค้าใหม่
- ถ้าไม่มีสินค้าเลย fallback เป็น `hero-bakery.svg`
- ใช้ Bootstrap carousel
- caption ดึงชื่อ หมวด ราคา จาก `heroProduct`

### บรรทัด 90-120: หมวดสินค้า

| บรรทัด | อธิบาย |
| --- | --- |
| 93-95 | heading ของ section |
| 98 | container `.range-grid` |
| 99 | loop `Model.Categories` |
| 101 | card ใช้ theme class ตามหมวด/สินค้าขายดี |
| 103 | รูปสินค้า featured ของหมวด |
| 104 | badge `ขายดี` หรือ `แนะนำ` |
| 107 | subtitle เช่น `4 เมนูในหมวดนี้` |
| 108 | ชื่อหมวด |
| 109 | คำอธิบายสินค้าขายดี |
| 111-115 | กล่องสรุปสินค้าขายดีในหมวด |

### บรรทัด 122-170: สินค้าขายดี

| บรรทัด | อธิบาย |
| --- | --- |
| 125 | heading `สินค้าขายดี` |
| 126 | copy บอกว่า 8 เมนู |
| 129 | container `.catalog-grid` |
| 130 | loop `Model.Products` ซึ่งถูก set เป็น best seller 8 ชิ้นจาก controller |
| 132 | card theme + sold-out class |
| 134 | รูปสินค้า |
| 135-138 | แสดง badge ถ้ามี |
| 142-144 | ชื่อ หมวด คำอธิบาย |
| 146-151 | ราคาและราคาเดิม ถ้ามี |
| 154-160 | form เพิ่มลงตะกร้า |
| 157-158 | disable button ถ้าสินค้าหมด |
| 167 | link ไปหน้าสินค้าทั้งหมด |

## หน้าสินค้าใน `Shop.cshtml`

ตำแหน่ง: `Views/Home/Shop.cshtml`

จุดสำคัญ:

- `Model.Products` คือสินค้าทั้งหมด ไม่ใช่เฉพาะ best seller
- `Model.Categories` ใช้สร้าง dropdown filter
- `.catalog-grid` ใช้ CSS เดียวกับหน้าแรก ดังนั้นแถวสุดท้ายที่ไม่เต็มจะอยู่กลางเหมือนกัน
- `data-product-card`, `data-name`, `data-category` ใช้กับ JavaScript filter/search

