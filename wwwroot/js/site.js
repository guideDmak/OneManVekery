const searchRoot = document.querySelector("[data-product-search]");

if (searchRoot) {
  const searchInput = searchRoot.querySelector("[data-search-input]");
  const categoryInput = searchRoot.querySelector("[data-category-input]");
  const cards = [...searchRoot.querySelectorAll("[data-product-card]")];
  const emptyState = searchRoot.querySelector("[data-empty-state]");

  const filterProducts = () => {
    const searchTerm = (searchInput?.value || "").trim().toLowerCase();
    const selectedCategory = categoryInput?.value || "all";
    let visibleCount = 0;

    cards.forEach((card) => {
      const name = card.dataset.name || "";
      const category = card.dataset.category || "";
      const matchesSearch = !searchTerm || name.includes(searchTerm) || category.toLowerCase().includes(searchTerm);
      const matchesCategory = selectedCategory === "all" || category === selectedCategory;
      const isVisible = matchesSearch && matchesCategory;

      card.classList.toggle("is-hidden", !isVisible);

      if (isVisible) {
        visibleCount += 1;
      }
    });

    emptyState?.classList.toggle("d-none", visibleCount > 0);
  };

  searchInput?.addEventListener("input", filterProducts);
  categoryInput?.addEventListener("change", filterProducts);
}

const adminItemsRoot = document.querySelector("[data-admin-items]");

if (adminItemsRoot && window.bootstrap) {
  const addModalElement = document.getElementById("addItemModal");
  const editModalElement = document.getElementById("editItemModal");
  const categoryModalElement = document.getElementById("addCategoryModal");
  const categoryListElement = document.getElementById("adminItemCategories");
  const adminMainWrap = document.querySelector(".admin-main-wrap");
  const itemsGridElement = adminItemsRoot.querySelector("[data-items-grid]");
  const itemCards = [...adminItemsRoot.querySelectorAll("[data-item-card]")];
  const itemsSearchInput = adminItemsRoot.querySelector("[data-items-search]");
  const itemsCategoryFilter = adminItemsRoot.querySelector("[data-items-category-filter]");
  const itemsSortSelect = adminItemsRoot.querySelector("[data-items-sort]");
  const itemsEmptyState = adminItemsRoot.querySelector("[data-items-empty-state]");
  const itemsResultsLabel = adminItemsRoot.querySelector("[data-items-results-label]");
  const totalItemCount = itemCards.length;
  let pendingReturnModalId = "";
  let shouldRestoreReturnModal = false;
  let applyItemFiltersAndSort = () => {};

  const setFieldValue = (id, value) => {
    const field = document.getElementById(id);
    if (!field) {
      return;
    }

    field.value = value ?? "";
  };

  const setCheckboxValue = (id, isChecked) => {
    const field = document.getElementById(id);
    if (!field) {
      return;
    }

    field.checked = isChecked;
  };

  const getOrCreateAdminNotice = () => {
    let notice = document.querySelector(".admin-site-notice");
    if (notice) {
      return notice;
    }

    notice = document.createElement("div");
    notice.className = "site-notice admin-site-notice";
    notice.setAttribute("role", "status");

    const topbar = adminMainWrap?.querySelector(".admin-topbar");
    if (adminMainWrap && topbar) {
      adminMainWrap.insertBefore(notice, topbar);
      return notice;
    }

    adminItemsRoot.prepend(notice);
    return notice;
  };

  const showAdminNotice = (message, isError = false) => {
    if (!message) {
      return;
    }

    const notice = getOrCreateAdminNotice();
    notice.textContent = message;
    notice.classList.toggle("is-error", isError);
  };

  const setCategoryValidation = (message = "") => {
    const validationElement = categoryModalElement?.querySelector("[data-category-validation]");
    if (!validationElement) {
      return;
    }

    validationElement.textContent = message;
  };

  const appendCategoryOption = (categoryName) => {
    if (!categoryListElement || !categoryName) {
      return;
    }

    const hasExistingOption = [...categoryListElement.querySelectorAll("option")]
      .some((option) => (option.value || "").trim().toLowerCase() === categoryName.trim().toLowerCase());

    if (hasExistingOption) {
      return;
    }

    const option = document.createElement("option");
    option.value = categoryName;
    categoryListElement.append(option);

    if (itemsCategoryFilter instanceof HTMLSelectElement) {
      const hasExistingSelectOption = [...itemsCategoryFilter.options]
        .some((selectOption) => (selectOption.value || "").trim().toLowerCase() === categoryName.trim().toLowerCase());

      if (!hasExistingSelectOption) {
        const selectOption = document.createElement("option");
        selectOption.value = categoryName;
        selectOption.textContent = categoryName;
        itemsCategoryFilter.append(selectOption);
      }
    }
  };

  const restorePreviousModal = () => {
    if (!shouldRestoreReturnModal || !pendingReturnModalId) {
      pendingReturnModalId = "";
      shouldRestoreReturnModal = false;
      return;
    }

    const returnModalElement = document.getElementById(pendingReturnModalId);
    pendingReturnModalId = "";
    shouldRestoreReturnModal = false;

    if (returnModalElement) {
      window.bootstrap.Modal.getOrCreateInstance(returnModalElement).show();
    }
  };

  const openCategoryModal = (trigger) => {
    if (!(categoryModalElement instanceof HTMLElement)) {
      return;
    }

    const targetFieldId = trigger?.dataset.categoryTarget || "";
    const returnModalId = trigger?.dataset.returnModalId || "";
    const targetField = targetFieldId ? document.getElementById(targetFieldId) : null;

    setFieldValue("CategoryForm_TargetFieldId", targetFieldId);
    setFieldValue("CategoryForm_ReturnModalId", returnModalId);
    setFieldValue("CategoryForm_Name", targetField instanceof HTMLInputElement ? targetField.value : "");
    setCategoryValidation("");

    pendingReturnModalId = returnModalId;
    shouldRestoreReturnModal = !!returnModalId;

    if (returnModalId) {
      const returnModalElement = document.getElementById(returnModalId);
      if (returnModalElement) {
        const showCategoryAfterHide = () => {
          returnModalElement.removeEventListener("hidden.bs.modal", showCategoryAfterHide);
          window.bootstrap.Modal.getOrCreateInstance(categoryModalElement).show();
        };

        returnModalElement.addEventListener("hidden.bs.modal", showCategoryAfterHide);
        window.bootstrap.Modal.getOrCreateInstance(returnModalElement).hide();
        return;
      }
    }

    window.bootstrap.Modal.getOrCreateInstance(categoryModalElement).show();
  };

  const updateStockCard = (card, item) => {
    if (!(card instanceof HTMLElement) || !item) {
      return;
    }

    const stockCount = card.querySelector("[data-stock-count]");
    const statusBadge = card.querySelector("[data-stock-status-badge]");
    const updatedAt = card.querySelector("[data-stock-updated]");
    const minusButton = card.querySelector('[data-stock-direction="-1"]');
    const editButton = card.querySelector(".admin-edit-item-button");

    if (stockCount) {
      stockCount.textContent = item.stockQuantity ?? 0;
    }

    card.dataset.stock = String(item.stockQuantity ?? 0);
    card.dataset.updatedAt = String(Date.now());

    if (statusBadge) {
      statusBadge.textContent = item.statusLabel ?? "";
      statusBadge.className = `admin-status-badge is-${item.statusKey || "in-stock"}`;
    }

    if (updatedAt) {
      updatedAt.textContent = `Updated ${item.updatedAtLabel || ""}`;
    }

    if (minusButton instanceof HTMLButtonElement) {
      minusButton.disabled = (item.stockQuantity ?? 0) === 0;
    }

    if (editButton instanceof HTMLElement) {
      editButton.dataset.stockQuantity = String(item.stockQuantity ?? 0);
    }
  };

  const getSortableNumber = (value) => {
    const parsedValue = Number.parseFloat(value || "0");
    return Number.isFinite(parsedValue) ? parsedValue : 0;
  };

  const compareText = (leftValue, rightValue) => leftValue.localeCompare(rightValue, "th", { sensitivity: "base" });

  applyItemFiltersAndSort = () => {
    if (!itemsGridElement) {
      return;
    }

    const searchTerm = (itemsSearchInput?.value || "").trim().toLowerCase();
    const selectedCategory = (itemsCategoryFilter?.value || "all").trim().toLowerCase();
    const sortKey = itemsSortSelect?.value || "name-asc";

    const visibleCards = [];
    const hiddenCards = [];

    itemCards.forEach((card) => {
      const name = (card.dataset.name || "").trim();
      const category = (card.dataset.category || "").trim();
      const sku = (card.dataset.sku || "").trim();
      const searchableText = `${name} ${category} ${sku}`.toLowerCase();
      const matchesSearch = !searchTerm || searchableText.includes(searchTerm);
      const matchesCategory = selectedCategory === "all" || category.toLowerCase() === selectedCategory;
      const isVisible = matchesSearch && matchesCategory;

      card.classList.toggle("is-hidden", !isVisible);

      if (isVisible) {
        visibleCards.push(card);
      } else {
        hiddenCards.push(card);
      }
    });

    visibleCards.sort((leftCard, rightCard) => {
      switch (sortKey) {
        case "name-desc":
          return compareText(rightCard.dataset.name || "", leftCard.dataset.name || "");
        case "price-desc":
          return getSortableNumber(rightCard.dataset.price) - getSortableNumber(leftCard.dataset.price);
        case "price-asc":
          return getSortableNumber(leftCard.dataset.price) - getSortableNumber(rightCard.dataset.price);
        case "stock-desc":
          return getSortableNumber(rightCard.dataset.stock) - getSortableNumber(leftCard.dataset.stock);
        case "stock-asc":
          return getSortableNumber(leftCard.dataset.stock) - getSortableNumber(rightCard.dataset.stock);
        case "updated-desc":
          return getSortableNumber(rightCard.dataset.updatedAt) - getSortableNumber(leftCard.dataset.updatedAt);
        default:
          return compareText(leftCard.dataset.name || "", rightCard.dataset.name || "");
      }
    });

    [...visibleCards, ...hiddenCards].forEach((card) => {
      itemsGridElement.append(card);
    });

    if (itemsEmptyState) {
      itemsEmptyState.classList.toggle("d-none", visibleCards.length > 0);
    }

    if (itemsResultsLabel) {
      itemsResultsLabel.textContent = visibleCards.length === totalItemCount
        ? `${totalItemCount} รายการ`
        : `แสดง ${visibleCards.length} จาก ${totalItemCount} รายการ`;
    }
  };

  itemsSearchInput?.addEventListener("input", applyItemFiltersAndSort);
  itemsCategoryFilter?.addEventListener("change", applyItemFiltersAndSort);
  itemsSortSelect?.addEventListener("change", applyItemFiltersAndSort);

  const submitStockAdjustment = async (formElement, submitter) => {
    const quantityInput = formElement.querySelector("[data-stock-amount]");
    const directionInput = formElement.querySelector("[data-stock-direction-input]");
    if (!(quantityInput instanceof HTMLInputElement) || !(directionInput instanceof HTMLInputElement)) {
      return;
    }

    const quantityAmount = Number.parseInt(quantityInput.value || "0", 10);
    if (!Number.isInteger(quantityAmount) || quantityAmount <= 0) {
      showAdminNotice("กรุณากรอกจำนวนที่ต้องการอย่างน้อย 1", true);
      quantityInput.focus();
      return;
    }

    const isDecrease = submitter.dataset.stockDirection === "-1";
    directionInput.value = isDecrease ? "-1" : "1";

    const currentStockElement = formElement.closest("[data-item-card]")?.querySelector("[data-stock-count]");
    const currentStock = Number.parseInt(currentStockElement?.textContent || "0", 10);
    if (isDecrease && Number.isInteger(currentStock) && quantityAmount > currentStock) {
      showAdminNotice("จำนวนที่ลดต้องไม่เกินสต็อกคงเหลือ", true);
      quantityInput.focus();
      return;
    }

    const formData = new FormData(formElement);

    const controls = [...formElement.querySelectorAll("[data-stock-direction], [data-stock-amount]")];
    controls.forEach((control) => {
      if (!(control instanceof HTMLButtonElement) && !(control instanceof HTMLInputElement)) {
        return;
      }

      control.dataset.wasDisabled = String(control.disabled);
      control.disabled = true;
    });

    try {
      const response = await fetch(formElement.action, {
        method: formElement.method || "post",
        headers: {
          "X-Requested-With": "XMLHttpRequest",
          Accept: "application/json"
        },
        body: formData
      });

      const payload = await response.json().catch(() => null);

      controls.forEach((control) => {
        if (!(control instanceof HTMLButtonElement) && !(control instanceof HTMLInputElement)) {
          return;
        }

        control.disabled = control.dataset.wasDisabled === "true";
        delete control.dataset.wasDisabled;
      });

      if (!response.ok || !payload?.success) {
        showAdminNotice(payload?.message || "ไม่สามารถปรับสต็อกได้", true);
        return;
      }

      updateStockCard(formElement.closest("[data-item-card]"), payload.item);
      applyItemFiltersAndSort();
      showAdminNotice(payload.message || "อัปเดตสต็อกแล้ว");
    } catch {
      controls.forEach((control) => {
        if (!(control instanceof HTMLButtonElement) && !(control instanceof HTMLInputElement)) {
          return;
        }

        control.disabled = control.dataset.wasDisabled === "true";
        delete control.dataset.wasDisabled;
      });

      showAdminNotice("อัปเดตสต็อกไม่สำเร็จ ลองใหม่อีกครั้ง", true);
    }
  };

  adminItemsRoot.querySelectorAll("[data-stock-adjust-form]").forEach((formElement) => {
    if (!(formElement instanceof HTMLFormElement)) {
      return;
    }

    formElement.addEventListener("submit", (event) => {
      event.preventDefault();
    });

    formElement.querySelectorAll("[data-stock-direction]").forEach((buttonElement) => {
      if (!(buttonElement instanceof HTMLButtonElement)) {
        return;
      }

      buttonElement.addEventListener("click", () => {
        void submitStockAdjustment(formElement, buttonElement);
      });
    });
  });

  document.querySelectorAll("[data-open-category-modal]").forEach((buttonElement) => {
    if (!(buttonElement instanceof HTMLButtonElement)) {
      return;
    }

    buttonElement.addEventListener("click", () => {
      openCategoryModal(buttonElement);
    });
  });

  const categoryFormElement = categoryModalElement?.querySelector("[data-add-category-form]");
  categoryFormElement?.addEventListener("submit", async (event) => {
    event.preventDefault();

    if (!(categoryFormElement instanceof HTMLFormElement)) {
      return;
    }

    const submitButton = categoryFormElement.querySelector("[data-category-submit]");
    if (!(submitButton instanceof HTMLButtonElement)) {
      return;
    }

    setCategoryValidation("");

    const formData = new FormData(categoryFormElement);
    submitButton.disabled = true;

    try {
      const response = await fetch(categoryFormElement.action, {
        method: categoryFormElement.method || "post",
        headers: {
          "X-Requested-With": "XMLHttpRequest",
          Accept: "application/json"
        },
        body: formData
      });

      const payload = await response.json().catch(() => null);
      submitButton.disabled = false;

      if (!response.ok || !payload?.success) {
        const message = payload?.message || "ไม่สามารถเพิ่มหมวดสินค้าได้";
        setCategoryValidation(message);
        showAdminNotice(message, true);
        return;
      }

      appendCategoryOption(payload.categoryName);

      const targetFieldId = (categoryFormElement.querySelector("#CategoryForm_TargetFieldId")?.value || "").trim();
      const targetField = targetFieldId ? document.getElementById(targetFieldId) : null;
      if (targetField instanceof HTMLInputElement && payload.categoryName) {
        targetField.value = payload.categoryName;
        targetField.dispatchEvent(new Event("input", { bubbles: true }));
      }

      showAdminNotice(payload.message || "เพิ่มหมวดสินค้าแล้ว");
      window.bootstrap.Modal.getOrCreateInstance(categoryModalElement).hide();
    } catch {
      submitButton.disabled = false;
      setCategoryValidation("เพิ่มหมวดสินค้าไม่สำเร็จ ลองใหม่อีกครั้ง");
      showAdminNotice("เพิ่มหมวดสินค้าไม่สำเร็จ ลองใหม่อีกครั้ง", true);
    }
  });

  categoryModalElement?.addEventListener("hidden.bs.modal", () => {
    setCategoryValidation("");
    setFieldValue("CategoryForm_Name", "");
    setFieldValue("CategoryForm_TargetFieldId", "");
    setFieldValue("CategoryForm_ReturnModalId", "");
    restorePreviousModal();
  });

  editModalElement?.addEventListener("show.bs.modal", (event) => {
    const trigger = event.relatedTarget;
    if (!(trigger instanceof HTMLElement) || !trigger.dataset.itemId) {
      return;
    }

    setFieldValue("EditForm_ItemId", trigger.dataset.itemId);
    setFieldValue("EditForm_ItemCode", trigger.dataset.itemCode);
    setFieldValue("EditForm_Name", trigger.dataset.name);
    setFieldValue("EditForm_Category", trigger.dataset.category);
    setFieldValue("EditForm_Sku", trigger.dataset.sku);
    setFieldValue("EditForm_Price", trigger.dataset.price);
    setFieldValue("EditForm_StockQuantity", trigger.dataset.stockQuantity);
    setFieldValue("EditForm_ReorderLevel", trigger.dataset.reorderLevel);
    setFieldValue("EditForm_Tagline", trigger.dataset.tagline);
    setFieldValue("EditForm_Notes", trigger.dataset.notes);
    setFieldValue("EditForm_ImagePath", trigger.dataset.imagePath);
    setFieldValue("EditStockDisplay", `${trigger.dataset.stockQuantity || 0} units`);
    setCheckboxValue("EditForm_IsPublished", trigger.dataset.isPublished === "true");
  });

  const activeModal = adminItemsRoot.dataset.activeModal;
  if (activeModal === "add" && addModalElement) {
    window.bootstrap.Modal.getOrCreateInstance(addModalElement).show();
  }

  if (activeModal === "edit" && editModalElement) {
    window.bootstrap.Modal.getOrCreateInstance(editModalElement).show();
  }

  if (activeModal === "category" && categoryModalElement) {
    window.bootstrap.Modal.getOrCreateInstance(categoryModalElement).show();
  }

  applyItemFiltersAndSort();
}

const adminProductsRoot = document.querySelector("[data-admin-products]");

if (adminProductsRoot) {
  const adminMainWrap = document.querySelector(".admin-main-wrap");
  const hideReasonModalElement = document.getElementById("productHideReasonModal");
  const hideReasonFormElement = hideReasonModalElement?.querySelector("[data-product-hide-reason-form]");
  const hideReasonInput = hideReasonModalElement?.querySelector("[data-product-hide-reason-input]");
  const hideReasonValidation = hideReasonModalElement?.querySelector("[data-product-hide-validation]");
  const hideReasonTarget = hideReasonModalElement?.querySelector("[data-product-hide-target]");
  let pendingHideForm = null;

  const getOrCreateProductsNotice = () => {
    let notice = document.querySelector(".admin-site-notice");
    if (notice) {
      return notice;
    }

    notice = document.createElement("div");
    notice.className = "site-notice admin-site-notice";
    notice.setAttribute("role", "status");

    const topbar = adminMainWrap?.querySelector(".admin-topbar");
    if (adminMainWrap && topbar) {
      adminMainWrap.insertBefore(notice, topbar);
      return notice;
    }

    adminProductsRoot.prepend(notice);
    return notice;
  };

  const showProductsNotice = (message, isError = false) => {
    if (!message) {
      return;
    }

    const notice = getOrCreateProductsNotice();
    notice.textContent = message;
    notice.classList.toggle("is-error", isError);
  };

  const setHideReasonValidation = (message = "") => {
    if (hideReasonValidation) {
      hideReasonValidation.textContent = message;
    }
  };

  const submitVisibilityForm = async (formElement) => {
    const submitButton = formElement.querySelector("[data-visibility-submit]");
    if (!(submitButton instanceof HTMLButtonElement)) {
      return;
    }

    const originalDisabled = submitButton.disabled;
    const formData = new FormData(formElement);
    submitButton.disabled = true;

    try {
      const response = await fetch(formElement.action, {
        method: formElement.method || "post",
        headers: {
          "X-Requested-With": "XMLHttpRequest",
          Accept: "application/json"
        },
        body: formData
      });

      const payload = await response.json().catch(() => null);
      submitButton.disabled = originalDisabled;

      if (!response.ok || !payload?.success) {
        return {
          success: false,
          message: payload?.message || "อัปเดตสถานะสินค้าไม่สำเร็จ"
        };
      }

      updateProductCard(formElement.closest("[data-product-card]"), payload.product);
      return {
        success: true,
        message: payload.message || "อัปเดตสถานะสินค้าแล้ว"
      };
    } catch {
      submitButton.disabled = originalDisabled;
      return {
        success: false,
        message: "อัปเดตสถานะสินค้าไม่สำเร็จ ลองใหม่อีกครั้ง"
      };
    }
  };

  const updateProductCard = (card, product) => {
    if (!(card instanceof HTMLElement) || !product) {
      return;
    }

    const badge = card.querySelector("[data-product-visibility-badge]");
    const publishedCopy = card.querySelector("[data-product-published-copy]");
    const actionInput = card.querySelector("[data-visibility-action-input]");
    const noteInput = card.querySelector("[data-visibility-note-input]");
    const submitButton = card.querySelector("[data-visibility-submit]");

    if (badge) {
      badge.textContent = product.visibilityLabel ?? "";
      badge.className = `admin-status-badge is-${product.visibilityKey || "gold"}`;
    }

    if (publishedCopy) {
      publishedCopy.textContent = product.publishedCopy ?? "";
    }

    if (actionInput instanceof HTMLInputElement) {
      actionInput.value = product.nextAction ?? "publish";
    }

    if (noteInput instanceof HTMLInputElement) {
      noteInput.value = product.notes ?? "";
    }

    card.dataset.productNotes = product.notes ?? "";

    if (submitButton instanceof HTMLButtonElement) {
      submitButton.textContent = product.buttonLabel ?? "Update Visibility";
      submitButton.classList.toggle("admin-primary-button", !product.isPublished);
      submitButton.classList.toggle("admin-secondary-button", !!product.isPublished);
    }
  };

  adminProductsRoot.querySelectorAll("[data-product-visibility-form]").forEach((formElement) => {
    if (!(formElement instanceof HTMLFormElement)) {
      return;
    }

    formElement.addEventListener("submit", async (event) => {
      event.preventDefault();

      const actionInput = formElement.querySelector("[data-visibility-action-input]");
      const nextAction = actionInput instanceof HTMLInputElement ? (actionInput.value || "").trim().toLowerCase() : "";

      if (nextAction === "hide" && hideReasonModalElement && hideReasonInput instanceof HTMLTextAreaElement) {
        pendingHideForm = formElement;
        const productCard = formElement.closest("[data-product-card]");
        const productName = productCard?.dataset.productName || "สินค้านี้";
        const existingNote = productCard?.dataset.productNotes || "";
        hideReasonInput.value = existingNote;
        setHideReasonValidation("");

        if (hideReasonTarget) {
          hideReasonTarget.innerHTML = `ระบุเหตุผลก่อนซ่อน <strong>${productName}</strong> จากหน้าร้าน`;
        }

        window.bootstrap.Modal.getOrCreateInstance(hideReasonModalElement).show();
        return;
      }

      const result = await submitVisibilityForm(formElement);
      showProductsNotice(result.message, !result.success);
    });
  });

  hideReasonFormElement?.addEventListener("submit", async (event) => {
    event.preventDefault();

    if (!(pendingHideForm instanceof HTMLFormElement) || !(hideReasonInput instanceof HTMLTextAreaElement) || !hideReasonModalElement) {
      return;
    }

    const reason = (hideReasonInput.value || "").trim();
    if (!reason) {
      setHideReasonValidation("กรุณาระบุเหตุผลก่อนซ่อนสินค้าจากหน้าร้าน");
      hideReasonInput.focus();
      return;
    }

    const noteInput = pendingHideForm.querySelector("[data-visibility-note-input]");
    if (noteInput instanceof HTMLInputElement) {
      noteInput.value = reason;
    }

    setHideReasonValidation("");
    const result = await submitVisibilityForm(pendingHideForm);

    if (!result.success) {
      setHideReasonValidation(result.message);
      showProductsNotice(result.message, true);
      return;
    }

    showProductsNotice(result.message);
    window.bootstrap.Modal.getOrCreateInstance(hideReasonModalElement).hide();
  });

  hideReasonModalElement?.addEventListener("hidden.bs.modal", () => {
    pendingHideForm = null;
    if (hideReasonInput instanceof HTMLTextAreaElement) {
      hideReasonInput.value = "";
    }

    setHideReasonValidation("");

    if (hideReasonTarget) {
      hideReasonTarget.textContent = "ระบุเหตุผลก่อนซ่อนสินค้านี้จากหน้าร้าน";
    }
  });
}

const adminAccountsRoot = document.querySelector("[data-admin-accounts]");

if (adminAccountsRoot && window.bootstrap) {
  const addAccountModalElement = document.getElementById("addAccountModal");
  const editAccountModalElement = document.getElementById("editAccountModal");
  const canChangeRoles = adminAccountsRoot.dataset.canChangeRoles === "true";
  const searchForm = adminAccountsRoot.querySelector("[data-account-search-form]");
  const searchInput = adminAccountsRoot.querySelector("[data-account-search-input]");
  const roleFilter = adminAccountsRoot.querySelector("[data-account-role-filter]");
  const statusFilter = adminAccountsRoot.querySelector("[data-account-status-filter]");
  const sortSelect = adminAccountsRoot.querySelector("[data-account-sort]");
  const searchResetButton = adminAccountsRoot.querySelector("[data-account-search-reset]");
  const searchRows = [...adminAccountsRoot.querySelectorAll("[data-account-row]")];
  const tableBody = adminAccountsRoot.querySelector("[data-account-table-body]");
  const searchEmptyState = adminAccountsRoot.querySelector("[data-account-search-empty]");
  const resultsLabel = adminAccountsRoot.querySelector("[data-account-results-label]");
  const totalAccountCount = searchRows.length;

  const setAccountFieldValue = (id, value) => {
    const field = document.getElementById(id);
    if (!field) {
      return;
    }

    field.value = value ?? "";
  };

  const setAccountRoleValue = (value) => {
    const field = document.getElementById("EditForm_Role");
    if (!field) {
      return;
    }

    if (canChangeRoles && field instanceof HTMLSelectElement) {
      [...field.options]
        .filter((option) => option.dataset.dynamicRole === "true")
        .forEach((option) => option.remove());

      if (value) {
        const hasOption = [...field.options].some((option) => option.value === value);
        if (!hasOption) {
          const dynamicOption = new Option(value, value, true, true);
          dynamicOption.dataset.dynamicRole = "true";
          field.add(dynamicOption, 0);
        }
      }

      field.value = value ?? "";
      return;
    }

      field.value = value ?? "";
  };

  const compareAccountText = (leftValue, rightValue) =>
    leftValue.localeCompare(rightValue, "th", { sensitivity: "base" });

  const getAccountNumber = (value) => {
    const parsedValue = Number.parseInt(value || "0", 10);
    return Number.isFinite(parsedValue) ? parsedValue : 0;
  };

  const filterAccountRows = () => {
    const keyword = (searchInput?.value || "").trim().toLowerCase();
    const selectedRole = (roleFilter?.value || "all").trim().toLowerCase();
    const selectedStatus = (statusFilter?.value || "all").trim().toLowerCase();
    const sortKey = sortSelect?.value || "last-active-desc";
    const visibleRows = [];
    const hiddenRows = [];

    searchRows.forEach((row) => {
      const haystack = (row.dataset.accountSearch || "").toLowerCase();
      const rowRole = (row.dataset.accountRole || "").trim().toLowerCase();
      const rowStatus = (row.dataset.accountStatus || "").trim().toLowerCase();
      const matchesKeyword = !keyword || haystack.includes(keyword);
      const matchesRole = selectedRole === "all" || rowRole === selectedRole;
      const matchesStatus = selectedStatus === "all" || rowStatus === selectedStatus;
      const isVisible = matchesKeyword && matchesRole && matchesStatus;

      row.classList.toggle("d-none", !isVisible);
      if (isVisible) {
        visibleRows.push(row);
      } else {
        hiddenRows.push(row);
      }
    });

    visibleRows.sort((leftRow, rightRow) => {
      switch (sortKey) {
        case "name-asc":
          return compareAccountText(leftRow.dataset.accountName || "", rightRow.dataset.accountName || "");
        case "name-desc":
          return compareAccountText(rightRow.dataset.accountName || "", leftRow.dataset.accountName || "");
        case "role-asc":
          return compareAccountText(leftRow.dataset.accountRole || "", rightRow.dataset.accountRole || "");
        case "status-asc":
          return compareAccountText(leftRow.dataset.accountStatus || "", rightRow.dataset.accountStatus || "");
        case "last-active-desc":
        default:
          return getAccountNumber(rightRow.dataset.accountLastActive) - getAccountNumber(leftRow.dataset.accountLastActive);
      }
    });

    if (tableBody) {
      [...visibleRows, ...hiddenRows].forEach((row) => {
        tableBody.append(row);
      });
    }

    searchEmptyState?.classList.toggle("d-none", visibleRows.length > 0);

    if (resultsLabel) {
      resultsLabel.textContent = visibleRows.length === totalAccountCount
        ? `${totalAccountCount} รายการ`
        : `แสดง ${visibleRows.length} จาก ${totalAccountCount} รายการ`;
    }
  };

  searchForm?.addEventListener("submit", (event) => {
    event.preventDefault();
    filterAccountRows();
  });

  searchInput?.addEventListener("input", filterAccountRows);
  roleFilter?.addEventListener("change", filterAccountRows);
  statusFilter?.addEventListener("change", filterAccountRows);
  sortSelect?.addEventListener("change", filterAccountRows);

  searchResetButton?.addEventListener("click", () => {
    if (searchInput instanceof HTMLInputElement) {
      searchInput.value = "";
      searchInput.focus();
    }

    if (roleFilter instanceof HTMLSelectElement) {
      roleFilter.value = "all";
    }

    if (statusFilter instanceof HTMLSelectElement) {
      statusFilter.value = "all";
    }

    if (sortSelect instanceof HTMLSelectElement) {
      sortSelect.value = "last-active-desc";
    }

    filterAccountRows();
  });

  editAccountModalElement?.addEventListener("show.bs.modal", (event) => {
    const trigger = event.relatedTarget;
    if (!(trigger instanceof HTMLElement) || !trigger.dataset.accountId) {
      return;
    }

    setAccountFieldValue("EditForm_AccountId", trigger.dataset.accountId);
    setAccountFieldValue("EditForm_AccountCode", trigger.dataset.accountCode);
    setAccountFieldValue("EditForm_FullName", trigger.dataset.fullName);
    setAccountFieldValue("EditForm_Email", trigger.dataset.email);
    setAccountFieldValue("EditForm_PhoneNumber", trigger.dataset.phoneNumber);
    setAccountRoleValue(trigger.dataset.role);
    setAccountFieldValue("EditForm_Status", trigger.dataset.status);
    setAccountFieldValue("EditForm_LastActiveDisplay", trigger.dataset.lastActive);
    setAccountFieldValue("EditForm_Notes", trigger.dataset.notes);
    setAccountFieldValue("EditForm_Password", "");
  });

  const activeModal = adminAccountsRoot.dataset.activeModal;
  if (activeModal === "account-add" && addAccountModalElement) {
    window.bootstrap.Modal.getOrCreateInstance(addAccountModalElement).show();
  }

  if (activeModal === "account-edit" && editAccountModalElement) {
    window.bootstrap.Modal.getOrCreateInstance(editAccountModalElement).show();
  }

  filterAccountRows();
}

const adminOrdersRoot = document.querySelector("[data-admin-orders]");

if (adminOrdersRoot && window.bootstrap) {
  const addOrderModalElement = document.getElementById("addOrderModal");
  const editOrderModalElement = document.getElementById("editOrderModal");
  const addOrderForm = document.querySelector("[data-add-order-form]");
  const customerCombobox = addOrderForm?.querySelector("[data-order-customer-combobox]");
  const customerIdInput = addOrderForm?.querySelector("[data-order-customer-id]");
  const customerSearchInput = addOrderForm?.querySelector("[data-order-customer-search]");
  const customerSuggestions = addOrderForm?.querySelector("[data-order-customer-suggestions]");
  const customerSelectedCopy = addOrderForm?.querySelector("[data-order-customer-selected]");
  const customerOptionButtons = [...(addOrderForm?.querySelectorAll("[data-order-customer-option]") || [])];
  const customerPhoneInput = addOrderForm?.querySelector("[data-order-customer-phone]");
  const deliveryFeeInput = addOrderForm?.querySelector("#AddForm_DeliveryFee");
  const productPickerCombobox = addOrderForm?.querySelector("[data-order-product-picker-combobox]");
  const productPickerIdInput = addOrderForm?.querySelector("[data-order-product-picker-id]");
  const productPickerSearchInput = addOrderForm?.querySelector("[data-order-product-picker-search]");
  const productPickerSuggestions = addOrderForm?.querySelector("[data-order-product-picker-suggestions]");
  const productPickerOptionButtons = [...(addOrderForm?.querySelectorAll("[data-order-product-picker-option]") || [])];
  const productPickerQuantityInput = addOrderForm?.querySelector("[data-order-product-picker-qty]");
  const productPickerMeta = addOrderForm?.querySelector("[data-order-product-picker-meta]");
  const productAddButton = addOrderForm?.querySelector("[data-order-product-add]");
  const orderLinesRoot = addOrderForm?.querySelector("[data-order-lines]");
  const orderLineTemplate = document.getElementById("orderLineRowTemplate");
  const orderSummaryEmpty = addOrderForm?.querySelector("[data-order-summary-empty]");
  const orderSubtotalElement = addOrderForm?.querySelector("[data-order-subtotal]");
  const orderDeliveryFeeElement = addOrderForm?.querySelector("[data-order-delivery-fee]");
  const orderGrandTotalElement = addOrderForm?.querySelector("[data-order-grand-total]");
  const defaultProductPickerMessage = "พิมพ์ชื่อสินค้าหรือ SKU เพื่อค้นหา";

  const setOrderFieldValue = (id, value) => {
    const field = document.getElementById(id);
    if (!field) {
      return;
    }

    field.value = value ?? "";
  };

  const hideCustomerSuggestions = () => {
    customerSuggestions?.classList.add("d-none");
  };

  const setCustomerCopy = (message) => {
    if (!(customerSelectedCopy instanceof HTMLElement)) {
      return;
    }

    customerSelectedCopy.textContent = message;
  };

  const applyCustomerSelection = (optionButton) => {
    if (!(optionButton instanceof HTMLButtonElement)) {
      return;
    }

    const customerName = optionButton.dataset.customerName || "";
    const customerEmail = optionButton.dataset.customerEmail || "";
    const customerPhone = optionButton.dataset.customerPhone || "";

    if (customerIdInput instanceof HTMLInputElement) {
      customerIdInput.value = optionButton.dataset.customerId || "";
    }

    if (customerSearchInput instanceof HTMLInputElement) {
      customerSearchInput.value = customerEmail ? `${customerName} (${customerEmail})` : customerName;
    }

    if (customerPhoneInput instanceof HTMLInputElement) {
      customerPhoneInput.value = customerPhone;
    }

    setCustomerCopy(
      customerEmail
        ? `เลือกแล้ว: ${customerName} • ${customerEmail}`
        : `เลือกแล้ว: ${customerName}`
    );

    hideCustomerSuggestions();
  };

  const filterCustomerOptions = () => {
    if (!(customerSearchInput instanceof HTMLInputElement)) {
      return;
    }

    const keyword = customerSearchInput.value.trim().toLowerCase();
    let visibleCount = 0;

    customerOptionButtons.forEach((optionButton) => {
      if (!(optionButton instanceof HTMLButtonElement)) {
        return;
      }

      const haystack = `${optionButton.dataset.customerName || ""} ${optionButton.dataset.customerEmail || ""} ${optionButton.dataset.customerPhone || ""}`.toLowerCase();
      const isVisible = !keyword || haystack.includes(keyword);

      optionButton.classList.toggle("d-none", !isVisible);
      if (isVisible) {
        visibleCount += 1;
      }
    });

    customerSuggestions?.classList.toggle("d-none", visibleCount === 0);
  };

  customerOptionButtons.forEach((optionButton) => {
    if (!(optionButton instanceof HTMLButtonElement)) {
      return;
    }

    optionButton.addEventListener("click", () => {
      applyCustomerSelection(optionButton);
    });
  });

  customerSearchInput?.addEventListener("focus", () => {
    filterCustomerOptions();
  });

  customerSearchInput?.addEventListener("input", () => {
    if (customerIdInput instanceof HTMLInputElement) {
      customerIdInput.value = "";
    }

    setCustomerCopy("พิมพ์ชื่อลูกค้าหรืออีเมลเพื่อค้นหา");
    filterCustomerOptions();
  });

  const formatNumber = (value) => Number(value || 0).toLocaleString("th-TH", {
    minimumFractionDigits: 0,
    maximumFractionDigits: 2
  });

  const formatMoney = (value) => `${formatNumber(value)} ฿`;

  const parseQuantity = (value) => Math.max(1, Number.parseInt(String(value || "1"), 10) || 1);

  const getProductData = (source) => {
    if (!(source instanceof HTMLElement) || !(source.dataset.productId || "").trim()) {
      return null;
    }

    return {
      id: source.dataset.productId || "",
      name: source.dataset.productName || "",
      meta: source.dataset.productMeta || "",
      stock: Math.max(0, Number.parseInt(source.dataset.productStock || "0", 10) || 0),
      price: Math.max(0, Number.parseFloat(source.dataset.productPrice || "0") || 0)
    };
  };

  const getProductDataById = (productId) => {
    if (!productId) {
      return null;
    }

    const optionButton = productPickerOptionButtons.find((button) =>
      button instanceof HTMLButtonElement && button.dataset.productId === productId
    );

    return optionButton instanceof HTMLElement ? getProductData(optionButton) : null;
  };

  const describeProduct = (product) => {
    if (!product) {
      return defaultProductPickerMessage;
    }

    return `ราคา ${formatMoney(product.price)} • คงเหลือ ${formatNumber(product.stock)} ชิ้น`;
  };

  const setProductPickerMessage = (message, isError = false) => {
    if (!(productPickerMeta instanceof HTMLElement)) {
      return;
    }

    productPickerMeta.textContent = message;
    productPickerMeta.classList.toggle("is-error", isError);
  };

  const hideProductSuggestions = () => {
    productPickerSuggestions?.classList.add("d-none");
  };

  const getOrderLineRows = () =>
    [...(orderLinesRoot?.querySelectorAll("[data-order-line-row]") || [])]
      .filter((row) => row instanceof HTMLElement);

  const createOrderLineRow = () => {
    if (!(orderLineTemplate instanceof HTMLTemplateElement)) {
      return null;
    }

    const fragment = orderLineTemplate.content.cloneNode(true);
    const wrapper = document.createElement("tbody");
    wrapper.append(fragment);
    return wrapper.querySelector("[data-order-line-row]");
  };

  const setOrderLineData = (lineRow, product) => {
    if (!(lineRow instanceof HTMLElement) || !product) {
      return;
    }

    lineRow.dataset.productId = product.id;
    lineRow.dataset.productName = product.name;
    lineRow.dataset.productMeta = product.meta;
    lineRow.dataset.productStock = String(product.stock);
    lineRow.dataset.productPrice = String(product.price);
  };

  const updateOrderLineRow = (lineRow) => {
    if (!(lineRow instanceof HTMLElement)) {
      return;
    }

    const product = getProductData(lineRow);
    const productIdInput = lineRow.querySelector("[data-order-line-product-id]");
    const quantityInput = lineRow.querySelector("[data-order-line-qty]");
    const nameElement = lineRow.querySelector("[data-order-line-name]");
    const metaElement = lineRow.querySelector("[data-order-line-meta]");
    const unitPriceElement = lineRow.querySelector("[data-order-line-unit-price]");
    const lineTotalElement = lineRow.querySelector("[data-order-line-total]");

    if (!(productIdInput instanceof HTMLInputElement) ||
      !(quantityInput instanceof HTMLInputElement) ||
      !(nameElement instanceof HTMLElement) ||
      !(metaElement instanceof HTMLElement) ||
      !(unitPriceElement instanceof HTMLElement) ||
      !(lineTotalElement instanceof HTMLElement) ||
      !product) {
      return;
    }

    const maxQuantity = product.stock > 0 ? product.stock : null;
    let quantity = parseQuantity(quantityInput.value);
    if (typeof maxQuantity === "number") {
      quantity = Math.min(quantity, maxQuantity);
    }

    quantityInput.value = String(quantity);
    productIdInput.value = product.id;
    nameElement.textContent = product.name;
    metaElement.textContent = product.meta || describeProduct(product);
    unitPriceElement.textContent = formatMoney(product.price);
    lineTotalElement.textContent = formatMoney(product.price * quantity);
  };

  const syncOrderLineNames = () => {
    getOrderLineRows().forEach((lineRow, index) => {
      const productIdInput = lineRow.querySelector("[data-order-line-product-id]");
      const quantityInput = lineRow.querySelector("[data-order-line-qty]");

      if (productIdInput instanceof HTMLInputElement) {
        productIdInput.name = `AddForm.Items[${index}].ProductId`;
      }

      if (quantityInput instanceof HTMLInputElement) {
        quantityInput.name = `AddForm.Items[${index}].Quantity`;
      }
    });
  };

  const syncOrderSummary = () => {
    const lineRows = getOrderLineRows();
    let subtotal = 0;

    lineRows.forEach((lineRow) => {
      updateOrderLineRow(lineRow);

      const quantityInput = lineRow.querySelector("[data-order-line-qty]");
      const product = getProductData(lineRow);
      if (!(quantityInput instanceof HTMLInputElement) || !product) {
        return;
      }

      subtotal += product.price * parseQuantity(quantityInput.value);
    });

    const deliveryFee = deliveryFeeInput instanceof HTMLInputElement
      ? Math.max(0, Number.parseFloat(deliveryFeeInput.value || "0") || 0)
      : 0;
    const grandTotal = subtotal + deliveryFee;

    orderSummaryEmpty?.classList.toggle("d-none", lineRows.length > 0);

    if (orderSubtotalElement instanceof HTMLElement) {
      orderSubtotalElement.textContent = formatMoney(subtotal);
    }

    if (orderDeliveryFeeElement instanceof HTMLElement) {
      orderDeliveryFeeElement.textContent = formatMoney(deliveryFee);
    }

    if (orderGrandTotalElement instanceof HTMLElement) {
      orderGrandTotalElement.textContent = formatMoney(grandTotal);
    }
  };

  const clearProductPicker = (focusInput = false) => {
    if (productPickerIdInput instanceof HTMLInputElement) {
      productPickerIdInput.value = "";
    }

    if (productPickerSearchInput instanceof HTMLInputElement) {
      productPickerSearchInput.value = "";
      if (focusInput) {
        productPickerSearchInput.focus();
      }
    }

    if (productPickerQuantityInput instanceof HTMLInputElement) {
      productPickerQuantityInput.value = "1";
    }

    setProductPickerMessage(defaultProductPickerMessage);
    hideProductSuggestions();
  };

  const applyProductSelection = (source) => {
    const product = getProductData(source);
    if (!product) {
      return;
    }

    if (productPickerIdInput instanceof HTMLInputElement) {
      productPickerIdInput.value = product.id;
    }

    if (productPickerSearchInput instanceof HTMLInputElement) {
      productPickerSearchInput.value = product.name;
    }

    if (productPickerQuantityInput instanceof HTMLInputElement) {
      const currentQuantity = parseQuantity(productPickerQuantityInput.value);
      const cappedQuantity = product.stock > 0 ? Math.min(currentQuantity, product.stock) : currentQuantity;
      productPickerQuantityInput.value = String(cappedQuantity);
    }

    setProductPickerMessage(describeProduct(product));
    hideProductSuggestions();
  };

  const filterProductOptions = () => {
    if (!(productPickerSearchInput instanceof HTMLInputElement)) {
      return;
    }

    const keyword = productPickerSearchInput.value.trim().toLowerCase();
    let visibleCount = 0;

    productPickerOptionButtons.forEach((optionButton) => {
      if (!(optionButton instanceof HTMLButtonElement)) {
        return;
      }

      const haystack = `${optionButton.dataset.productName || ""} ${optionButton.dataset.productMeta || ""}`.toLowerCase();
      const isVisible = !keyword || haystack.includes(keyword);

      optionButton.classList.toggle("d-none", !isVisible);
      if (isVisible) {
        visibleCount += 1;
      }
    });

    productPickerSuggestions?.classList.toggle("d-none", visibleCount === 0);
  };

  const addSelectedProductToOrder = () => {
    if (!(orderLinesRoot instanceof HTMLElement) ||
      !(productPickerIdInput instanceof HTMLInputElement) ||
      !(productPickerQuantityInput instanceof HTMLInputElement)) {
      return;
    }

    const product = getProductDataById(productPickerIdInput.value);
    if (!product) {
      setProductPickerMessage("เลือกสินค้าก่อนเพิ่มลงออเดอร์", true);
      return;
    }

    const requestedQuantity = parseQuantity(productPickerQuantityInput.value);
    if (product.stock > 0 && requestedQuantity > product.stock) {
      setProductPickerMessage(`สินค้า ${product.name} มีคงเหลือ ${formatNumber(product.stock)} ชิ้น`, true);
      return;
    }

    const existingRow = getOrderLineRows().find((lineRow) => lineRow.dataset.productId === product.id);
    if (existingRow instanceof HTMLElement) {
      const quantityInput = existingRow.querySelector("[data-order-line-qty]");
      if (!(quantityInput instanceof HTMLInputElement)) {
        return;
      }

      const nextQuantity = parseQuantity(quantityInput.value) + requestedQuantity;
      if (product.stock > 0 && nextQuantity > product.stock) {
        setProductPickerMessage(`สินค้า ${product.name} มีคงเหลือ ${formatNumber(product.stock)} ชิ้น`, true);
        return;
      }

      quantityInput.value = String(nextQuantity);
      updateOrderLineRow(existingRow);
    } else {
      const newRow = createOrderLineRow();
      if (!(newRow instanceof HTMLElement)) {
        return;
      }

      setOrderLineData(newRow, product);

      const quantityInput = newRow.querySelector("[data-order-line-qty]");
      if (quantityInput instanceof HTMLInputElement) {
        quantityInput.value = String(requestedQuantity);
      }

      orderLinesRoot.append(newRow);
      updateOrderLineRow(newRow);
    }

    syncOrderLineNames();
    syncOrderSummary();
    clearProductPicker(true);
  };

  deliveryFeeInput?.addEventListener("input", syncOrderSummary);

  productPickerOptionButtons.forEach((optionButton) => {
    if (!(optionButton instanceof HTMLButtonElement)) {
      return;
    }

    optionButton.addEventListener("click", () => {
      applyProductSelection(optionButton);
    });
  });

  productPickerSearchInput?.addEventListener("focus", () => {
    filterProductOptions();
  });

  productPickerSearchInput?.addEventListener("input", () => {
    if (productPickerIdInput instanceof HTMLInputElement) {
      productPickerIdInput.value = "";
    }

    setProductPickerMessage(defaultProductPickerMessage);
    filterProductOptions();
  });

  productPickerSearchInput?.addEventListener("keydown", (event) => {
    if (event.key !== "Enter") {
      return;
    }

    event.preventDefault();

    const firstVisibleOption = productPickerOptionButtons.find((optionButton) =>
      optionButton instanceof HTMLButtonElement && !optionButton.classList.contains("d-none")
    );

    if (productPickerIdInput instanceof HTMLInputElement && productPickerIdInput.value) {
      addSelectedProductToOrder();
      return;
    }

    if (firstVisibleOption instanceof HTMLButtonElement) {
      applyProductSelection(firstVisibleOption);
    }
  });

  productPickerQuantityInput?.addEventListener("input", () => {
    if (!(productPickerQuantityInput instanceof HTMLInputElement)) {
      return;
    }

    const selectedProductId = productPickerIdInput instanceof HTMLInputElement
      ? productPickerIdInput.value
      : "";
    const selectedProduct = getProductDataById(selectedProductId);
    const quantity = parseQuantity(productPickerQuantityInput.value);

    if (selectedProduct && selectedProduct.stock > 0) {
      productPickerQuantityInput.value = String(Math.min(quantity, selectedProduct.stock));
      return;
    }

    productPickerQuantityInput.value = String(quantity);
  });

  productAddButton?.addEventListener("click", addSelectedProductToOrder);

  orderLinesRoot?.addEventListener("click", (event) => {
    const target = event.target;
    if (!(target instanceof Element)) {
      return;
    }

    const removeButton = target.closest("[data-remove-order-line]");
    if (!(removeButton instanceof HTMLElement)) {
      return;
    }

    const lineRow = removeButton.closest("[data-order-line-row]");
    if (!(lineRow instanceof HTMLElement)) {
      return;
    }

    lineRow.remove();
    syncOrderLineNames();
    syncOrderSummary();
  });

  orderLinesRoot?.addEventListener("input", (event) => {
    const target = event.target;
    if (!(target instanceof HTMLInputElement) || !target.matches("[data-order-line-qty]")) {
      return;
    }

    const lineRow = target.closest("[data-order-line-row]");
    if (!(lineRow instanceof HTMLElement)) {
      return;
    }

    updateOrderLineRow(lineRow);
    syncOrderSummary();
  });

  document.addEventListener("click", (event) => {
    const target = event.target;

    if (customerCombobox instanceof HTMLElement &&
      target instanceof Node &&
      !customerCombobox.contains(target)) {
      hideCustomerSuggestions();
    }

    if (productPickerCombobox instanceof HTMLElement &&
      target instanceof Node &&
      !productPickerCombobox.contains(target)) {
      hideProductSuggestions();
    }
  });

  productPickerCombobox?.addEventListener("focusout", () => {
    window.setTimeout(hideProductSuggestions, 120);
  });

  getOrderLineRows().forEach((lineRow) => {
    updateOrderLineRow(lineRow);
  });
  syncOrderLineNames();
  syncOrderSummary();

  editOrderModalElement?.addEventListener("show.bs.modal", (event) => {
    const trigger = event.relatedTarget;
    if (!(trigger instanceof HTMLElement) || !trigger.dataset.orderId) {
      return;
    }

    setOrderFieldValue("EditForm_OrderId", trigger.dataset.orderId);
    setOrderFieldValue("EditForm_OrderNumber", trigger.dataset.orderNumber);
    setOrderFieldValue("EditForm_CustomerName", trigger.dataset.customerName);
    setOrderFieldValue("EditForm_CreatedAtLabel", trigger.dataset.createdAt);
    setOrderFieldValue("EditForm_ItemSummary", trigger.dataset.itemSummary);
    setOrderFieldValue("EditForm_TotalAmountLabel", trigger.dataset.totalAmount);
    setOrderFieldValue("EditForm_PaymentMethodLabel", trigger.dataset.paymentMethod);
    setOrderFieldValue("EditForm_OrderStatus", trigger.dataset.orderStatus);
    setOrderFieldValue("EditForm_PaymentStatus", trigger.dataset.paymentStatus);
    setOrderFieldValue("EditForm_Phone", trigger.dataset.phone);
    setOrderFieldValue("EditForm_Address", trigger.dataset.address);
    setOrderFieldValue("EditForm_Note", trigger.dataset.note);
  });

  const activeModal = adminOrdersRoot.dataset.activeModal;
  if (activeModal === "order-add" && addOrderModalElement) {
    window.bootstrap.Modal.getOrCreateInstance(addOrderModalElement).show();
  }

  if (activeModal === "order-edit" && editOrderModalElement) {
    window.bootstrap.Modal.getOrCreateInstance(editOrderModalElement).show();
  }
}
