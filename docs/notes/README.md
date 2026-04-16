# Project Notes Index

สารบัญโน้ตอธิบายระบบ One Man Vekery

## หมวดที่มีตอนนี้

| หมวด | โฟลเดอร์ | อ่านเมื่อ |
| --- | --- | --- |
| Storefront Home | `docs/notes/storefront-home/` | ต้องการเข้าใจหน้าแรก สินค้า หมวดสินค้า ตะกร้า checkout และหน้า storefront |
| Account/Auth | `docs/notes/account-auth/` | ต้องการเข้าใจ login/register/profile/address/session |
| Admin Workspace | `docs/notes/admin-workspace/` | ต้องการเข้าใจ dashboard, orders, products, items, codes, accounts, staff |
| ViewModels | `docs/notes/viewmodels/` | ต้องการเข้าใจข้อมูลที่ controller ส่งให้ view, form binding และ validation |
| Views | `docs/notes/views/` | ต้องการเข้าใจไฟล์ `.cshtml`, layout, form action, Tag Helpers และ `data-*` hooks |
| Shared Layouts | `docs/notes/shared-layouts/` | ต้องการเข้าใจ layout กลาง, navbar, footer, admin layout, modal, JavaScript glue |

## ลำดับอ่านแนะนำ

1. `storefront-home/README.md`
2. `storefront-home/01-controller-actions.md`
3. `storefront-home/02-home-products-categories.md`
4. `storefront-home/03-cart-checkout-promotions.md`
5. `storefront-home/04-views-css-layout.md`
6. `storefront-home/05-other-storefront-pages.md`
7. `account-auth/README.md`
8. `admin-workspace/README.md`
9. `viewmodels/README.md`
10. `viewmodels/01-storefront-viewmodels.md`
11. `viewmodels/02-auth-viewmodels.md`
12. `viewmodels/03-admin-page-viewmodels.md`
13. `viewmodels/04-admin-record-form-viewmodels.md`
14. `viewmodels/05-binding-validation-flow.md`
15. `views/README.md`
16. `views/01-shared-layouts-and-razor-basics.md`
17. `views/02-home-storefront-views.md`
18. `views/03-account-views.md`
19. `views/04-admin-dashboard-products-items.md`
20. `views/05-admin-orders-codes-accounts.md`
21. `views/06-view-js-form-hooks.md`
22. `shared-layouts/README.md`

## วิธีใช้โน้ตชุดนี้

- ถ้าจะแก้หน้า view ให้เริ่มจากไฟล์หมวด view/layout ก่อน แล้วค่อยตามไป controller
- ถ้าจะแก้ process เช่น checkout หรือ order ให้เริ่มจาก controller flow ก่อน
- ถ้าจะแก้ field ใหม่ ให้ไล่ครบ 4 ชั้น: ViewModel, Controller, View, JavaScript/CSS
- ถ้าจะแก้ admin AJAX ให้ดูทั้ง `Views/Admin/*.cshtml`, `AdminController`, และ `wwwroot/js/site.js`
