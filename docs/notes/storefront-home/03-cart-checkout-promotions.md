# 03 Cart, Checkout, Pricing, Promotions

เอกสารนี้อธิบาย flow ตะกร้า checkout ราคา promotion และ order creation

## ไฟล์ที่เกี่ยวข้อง

| ไฟล์ | หน้าที่ |
| --- | --- |
| `Controllers/HomeController.cs` | actions: Cart, AddToCart, ChangeCartQuantity, RemoveFromCart, Checkout, ApplyPromoCode |
| `Controllers/Home/CartCatalog.cs` | session cart, product lookup, create order |
| `Controllers/Home/PageModels.cs` | build cart page model, order status model, my orders model |
| `Controllers/Home/CheckoutPricing.cs` | คำนวณราคาและ benefit |
| `Controllers/Home/Promotions.cs` | ตรวจ promotion/promo code/loyalty |
| `Controllers/Home/InternalTypes.cs` | internal records ที่ใช้ส่งข้อมูลระหว่าง helper |

## Flow เพิ่มสินค้าเข้าตะกร้า

```text
POST /Home/AddToCart
  -> HomeController.AddToCart(productId)
  -> IsStorefrontUserSignedIn()
  -> AddItemToCart(productId)
     -> GetActiveProductEntity(productId)
     -> ReadCartItems()
     -> เพิ่ม quantity โดยไม่เกิน stock และไม่เกิน 99
     -> WriteCartItems()
  -> RedirectToAction(Cart)
```

## `AddItemToCart()`

ตำแหน่ง: `Controllers/Home/CartCatalog.cs:104-137`

| ช่วงบรรทัด | อธิบาย |
| --- | --- |
| 104 | method รับ product id และ quantity ค่า default คือ 1 |
| 106-110 | โหลดสินค้า active ถ้าไม่เจอ, stock หมด, หรือ quantity <= 0 ให้ fail |
| 112 | อ่าน cart จาก session |
| 113 | หา index ของสินค้านี้ใน cart |
| 114 | ถ้าเจออยู่แล้ว อ่านจำนวนเดิม |
| 115 | จำนวนใหม่คือจำนวนเดิม + quantity แต่ไม่เกิน stock |
| 117-120 | ถ้าเพิ่มแล้วไม่มากกว่าเดิม แปลว่าเพิ่มไม่ได้ เช่น stock เต็มแล้ว |
| 122-125 | ถ้ามี item เดิม update quantity และ cap ที่ 99 |
| 127-134 | ถ้ายังไม่มี item เพิ่ม `CartSessionItem` ใหม่ |
| 136 | เขียน cart กลับ session |
| 137 | return true |

## Flow เปิดหน้าตะกร้า

```text
GET /Home/Cart
  -> login guard
  -> BuildCartPageModel()
     -> GetCartItems()
     -> ApplySignedInCheckoutDefaults()
     -> BuildPricingSummary()
     -> map CartLineRecord เป็น CartLineViewModel
     -> map pricing เป็น labels/summary
  -> Views/Home/Cart.cshtml
```

## `BuildCartPageModel()`

ตำแหน่ง: `Controllers/Home/PageModels.cs:78-130`

หน้าที่: รวมข้อมูลที่หน้า cart ต้องแสดงทั้งหมดไว้ใน object เดียว

ข้อมูลที่รวม:

- รายการสินค้าในตะกร้า
- payment options
- promotion/benefit ที่ใช้แล้ว
- subtotal/delivery/discount
- promo message
- point balance
- point discount limit
- reward item state

จุดสำคัญ:

- ถ้าไม่ได้ส่ง checkout model มา จะสร้างใหม่โดย default payment เป็น `promptpay`
- ถ้ามาจาก GET ปกติจะ include promo code ที่ persist ไว้ใน session
- ถ้ามาจาก POST ที่ validate fail จะ preserve checkout ที่ user กรอกมา

## Flow submit checkout

```text
POST /Home/Checkout
  -> login guard
  -> GetCartItems()
  -> ถ้าตะกร้าว่าง redirect Cart
  -> ApplySignedInCheckoutDefaults(checkout)
  -> BuildPricingSummary(cartItems, checkout)
  -> validate promo code
  -> validate point reward
  -> validate point discount limit
  -> TryValidateCartInventory()
  -> ถ้า ModelState invalid กลับ Cart
  -> PersistPromoCode()
  -> TryCreateOrder()
  -> ClearCart()
  -> redirect OrderStatus
```

## `BuildPricingSummary()`

ตำแหน่ง: `Controllers/Home/CheckoutPricing.cs:15-248`

หน้าที่: คำนวณราคาและสิทธิประโยชน์ทั้งหมดก่อนสร้าง order หรือ preview cart

สิ่งที่คำนวณ:

- subtotal
- delivery fee
- automatic promotions
- promo code
- point discount
- free reward item
- total discount
- points earned
- projected point balance
- applied benefits list

เหตุผลที่แยกเป็น `PricingSummary`:

- action ไม่ต้องรู้รายละเอียดสูตรคำนวณทุกอย่าง
- checkout จริงและ apply promo preview ใช้ logic ราคาเดียวกัน
- ลดโอกาสราคา preview กับราคาตอนสั่งซื้อไม่ตรงกัน

## `PricingSummary`

ตำแหน่ง: `Controllers/Home/InternalTypes.cs:15-32`

record นี้เป็นผลลัพธ์รวมจากการคำนวณราคา

| field | ความหมาย |
| --- | --- |
| `Subtotal` | ราคาสินค้ารวมก่อนส่วนลด |
| `DeliveryFee` | ค่าส่ง |
| `DiscountAmount` | ส่วนลดสินค้า |
| `ShippingDiscountAmount` | ส่วนลดค่าส่ง |
| `CurrentPoints` | point ปัจจุบันของ user |
| `PointsEarned` | point ที่จะได้จาก order นี้ |
| `PointsRedeemed` | point ที่ใช้ |
| `PointsDiscountAmount` | เงินที่ลดจาก point |
| `MaxPointDiscountRedeem` | point สูงสุดที่ใช้ลดได้ใน order นี้ |
| `ProjectedPointsBalance` | point คงเหลือหลัง checkout |
| `RewardPointCost` | point ที่ต้องใช้แลก reward |
| `RewardQty` | จำนวน reward |
| `RewardProductName` | ชื่อ reward |
| `CanRedeemFreeItem` | user แลก reward ได้หรือไม่ |
| `LoyaltyPromotionId` | promotion ที่เกี่ยวกับ loyalty |
| `AppliedBenefits` | รายการ benefit ที่ใช้แสดงในหน้า cart/order |
| `Promo` | ผลลัพธ์ promo code |

## `PromoResolution`

ตำแหน่ง: `Controllers/Home/InternalTypes.cs:49-66`

record นี้อธิบายสถานะของ promo code

| field | ความหมาย |
| --- | --- |
| `IsValid` | code ถูกต้องตามเงื่อนไขหรือไม่ |
| `IsApplied` | code ถูกใช้จริงหรือไม่ |
| `Code` | code ที่ normalize แล้ว |
| `Title` | ชื่อ promo |
| `Description` | คำอธิบาย |
| `DiscountAmount` | ส่วนลดสินค้า |
| `ShippingDiscountAmount` | ส่วนลดค่าส่ง |
| `Message` | ข้อความที่แสดงให้ user |
| `MessageState` | state ของข้อความ เช่น success/error |
| `PromoCodeId` | id ของ promo code |
| `PromotionId` | id ของ promotion ที่ผูกอยู่ |

## `ApplyPromoCode()`

ตำแหน่ง: `Controllers/HomeController.cs:243-290`

หน้าที่: preview promo หรือ point discount โดยไม่สร้าง order

จุดที่ต้องเข้าใจ:

- method นี้เป็น POST แต่ render view `"Cart"` กลับมาเลย
- ใช้ `ClearCheckoutValidationForPromoPreview()` เพื่อลบ error ของ field checkout อื่น
- ถ้า user กด apply โดยไม่กรอก code จะขึ้น error เฉพาะกรณี action เป็น promo
- ถ้า code valid จะ `PersistPromoCode(pricing.Promo.Code)`
- ถ้า code invalid จะ clear persisted promo code

## `TryCreateOrder()`

ตำแหน่ง: `Controllers/Home/CartCatalog.cs:169-318`

หน้าที่: สร้าง order จริงใน database

ลำดับหลัก:

1. แปลง product id ใน cart เป็น int
2. เปิด database transaction
3. โหลด products ที่เกี่ยวข้อง
4. ตรวจทุก item ว่ายัง active, stock ยังพอ
5. ถ้ามี promo code ที่ใช้จริง โหลด `PromoCode`
6. สร้าง `Order`
7. สร้าง `OrderItems`
8. ลด stock สินค้า
9. สร้าง `OrderPromotions` จาก applied benefits
10. บันทึก point ledger ถ้ามี earned/redeemed
11. update usage count ของ promo code
12. save changes
13. commit transaction

เหตุผลที่ต้องใช้ transaction:

- การสร้าง order, ลด stock, บันทึก promotion และ point ต้องสำเร็จพร้อมกัน
- ถ้า error กลางทาง ไม่ควรเกิด order ครึ่งเดียวหรือ stock ลดโดยไม่มี order

## Promotion flow

ไฟล์หลัก: `Controllers/Home/Promotions.cs`

```text
BuildPricingSummary()
  -> GetStoreNow()
  -> GetActiveAutomaticPromotions(storeNow)
  -> IsPromotionActiveNow()
  -> PromotionMeetsThresholds()
  -> CalculatePromotionDiscountAmount()
  -> ResolvePromoCode()
  -> CalculatePromoDiscountAmount()
  -> CalculateShippingDiscountAmount()
```

## Method map ใน `Promotions.cs`

| method | บรรทัด | หน้าที่ |
| --- | --- | --- |
| `GetCurrentPointsBalance` | 15 | อ่าน point balance ปัจจุบัน |
| `ApplyCheckoutLoyalty` | 30 | เขียน point ledger หลัง checkout |
| `GetActiveAutomaticPromotions` | 92 | ดึง promotion auto ที่ยัง active |
| `IsPromotionActiveNow` | 105 | ตรวจช่วงวันที่ เวลา วันในสัปดาห์ |
| `GetWeekdayMask` | 143 | แปลงวันเป็น bit mask |
| `PromotionMeetsThresholds` | 148 | ตรวจ minimum subtotal/item count |
| `CalculateBuyGetPromotionDiscount` | 163 | คำนวณ buy/get style promotion |
| `CalculatePromotionDiscountAmount` | 197 | คำนวณส่วนลด promotion |
| `ResolveRewardPromotionDiscount` | 223 | คำนวณ reward item |
| `CalculateEarnedPoints` | 240 | คำนวณ point ที่ได้รับ |
| `BuildPromotionDescription` | 255 | สร้างข้อความ benefit |
| `BuildLoyaltyEarningDescription` | 275 | สร้างข้อความ point earning |
| `GetStoreNow` | 285 | คืนเวลาร้าน Asia/Bangkok |
| `ResolvePromoCode` | 306 | validate promo code |
| `CalculatePromoDiscountAmount` | 410 | คำนวณส่วนลดจาก promo code |
| `CalculateShippingDiscountAmount` | 445 | คำนวณส่วนลดค่าส่งจาก promo code |
| `NormalizeDiscountType` | 471 | normalize discount type |
| `IsRecordActive` | 483 | ตรวจ active status |
| `IsWithinUsageWindow` | 489 | ตรวจวันเริ่ม/หมดอายุ |

## จุดเสี่ยงที่ควรระวังเวลาแก้ checkout

- อย่าให้ preview promo ใช้สูตรคนละชุดกับ checkout จริง
- อย่าลด stock นอก transaction
- อย่าลืมตรวจ stock อีกครั้งก่อนสร้าง order
- อย่าเก็บ promo code invalid ใน session
- ถ้าเพิ่ม promotion type ใหม่ ต้องเพิ่มทั้ง description, calculation, และ order benefit mapping

