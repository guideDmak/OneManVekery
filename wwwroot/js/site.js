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
  const adminMainWrap = document.querySelector(".admin-main-wrap");

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
}

const adminAccountsRoot = document.querySelector("[data-admin-accounts]");

if (adminAccountsRoot && window.bootstrap) {
  const addAccountModalElement = document.getElementById("addAccountModal");
  const editAccountModalElement = document.getElementById("editAccountModal");
  const canChangeRoles = adminAccountsRoot.dataset.canChangeRoles === "true";
  const searchForm = adminAccountsRoot.querySelector("[data-account-search-form]");
  const searchInput = adminAccountsRoot.querySelector("[data-account-search-input]");
  const searchResetButton = adminAccountsRoot.querySelector("[data-account-search-reset]");
  const searchRows = [...adminAccountsRoot.querySelectorAll("[data-account-row]")];
  const searchEmptyState = adminAccountsRoot.querySelector("[data-account-search-empty]");

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

  const filterAccountRows = () => {
    const keyword = (searchInput?.value || "").trim().toLowerCase();
    let visibleCount = 0;

    searchRows.forEach((row) => {
      const haystack = (row.dataset.accountSearch || "").toLowerCase();
      const isVisible = !keyword || haystack.includes(keyword);

      row.classList.toggle("d-none", !isVisible);
      if (isVisible) {
        visibleCount += 1;
      }
    });

    searchEmptyState?.classList.toggle("d-none", visibleCount > 0);
  };

  searchForm?.addEventListener("submit", (event) => {
    event.preventDefault();
    filterAccountRows();
  });

  searchInput?.addEventListener("input", filterAccountRows);

  searchResetButton?.addEventListener("click", () => {
    if (searchInput instanceof HTMLInputElement) {
      searchInput.value = "";
      searchInput.focus();
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
}

const adminOrdersRoot = document.querySelector("[data-admin-orders]");

if (adminOrdersRoot && window.bootstrap) {
  const addOrderModalElement = document.getElementById("addOrderModal");
  const editOrderModalElement = document.getElementById("editOrderModal");
  const addOrderForm = adminOrdersRoot.querySelector("[data-add-order-form]");
  const customerCombobox = addOrderForm?.querySelector("[data-order-customer-combobox]");
  const customerIdInput = addOrderForm?.querySelector("[data-order-customer-id]");
  const customerSearchInput = addOrderForm?.querySelector("[data-order-customer-search]");
  const customerSuggestions = addOrderForm?.querySelector("[data-order-customer-suggestions]");
  const customerSelectedCopy = addOrderForm?.querySelector("[data-order-customer-selected]");
  const customerOptionButtons = [...(addOrderForm?.querySelectorAll("[data-order-customer-option]") || [])];
  const customerPhoneInput = addOrderForm?.querySelector("[data-order-customer-phone]");
  const orderLinesRoot = addOrderForm?.querySelector("[data-order-lines]");
  const addOrderLineButton = addOrderForm?.querySelector("[data-add-order-line]");
  const orderLineTemplate = document.getElementById("orderLineTemplate");

  const setOrderFieldValue = (id, value) => {
    const field = document.getElementById(id);
    if (!field) {
      return;
    }

    field.value = value ?? "";
  };

  const updateOrderLineMeta = (lineRow) => {
    if (!(lineRow instanceof HTMLElement)) {
      return;
    }

    const productSelect = lineRow.querySelector("[data-order-product-select]");
    const metaLabel = lineRow.querySelector("[data-order-line-meta]");

    if (!(productSelect instanceof HTMLSelectElement) || !(metaLabel instanceof HTMLElement)) {
      return;
    }

    const selectedOption = productSelect.selectedOptions[0];
    if (!selectedOption || !selectedOption.value) {
      metaLabel.textContent = "";
      return;
    }

    metaLabel.textContent = `Available stock: ${selectedOption.dataset.stock || 0}`;
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

  document.addEventListener("click", (event) => {
    if (!(customerCombobox instanceof HTMLElement)) {
      return;
    }

    const target = event.target;
    if (target instanceof Node && !customerCombobox.contains(target)) {
      hideCustomerSuggestions();
    }
  });

  const bindOrderLine = (lineRow) => {
    if (!(lineRow instanceof HTMLElement)) {
      return;
    }

    if (lineRow.dataset.orderLineBound === "true") {
      updateOrderLineMeta(lineRow);
      return;
    }

    const removeButton = lineRow.querySelector("[data-remove-order-line]");
    const productSelect = lineRow.querySelector("[data-order-product-select]");

    removeButton?.addEventListener("click", () => {
      if (!(orderLinesRoot instanceof HTMLElement)) {
        return;
      }

      const allLines = orderLinesRoot.querySelectorAll("[data-order-line-row]");
      if (allLines.length <= 1) {
        return;
      }

      lineRow.remove();
      reindexOrderLines();
    });

    productSelect?.addEventListener("change", () => {
      updateOrderLineMeta(lineRow);
    });

    updateOrderLineMeta(lineRow);
    lineRow.dataset.orderLineBound = "true";
  };

  const reindexOrderLines = () => {
    if (!(orderLinesRoot instanceof HTMLElement)) {
      return;
    }

    [...orderLinesRoot.querySelectorAll("[data-order-line-row]")].forEach((lineRow, index) => {
      const productSelect = lineRow.querySelector("[data-order-product-select]");
      const quantityInput = lineRow.querySelector('input[type="number"]');

      if (productSelect instanceof HTMLSelectElement) {
        productSelect.name = `AddForm.Items[${index}].ProductId`;
      }

      if (quantityInput instanceof HTMLInputElement) {
        quantityInput.name = `AddForm.Items[${index}].Quantity`;
      }

      bindOrderLine(lineRow);
    });
  };

  addOrderLineButton?.addEventListener("click", () => {
    if (!(orderLinesRoot instanceof HTMLElement) || !(orderLineTemplate instanceof HTMLTemplateElement)) {
      return;
    }

    const fragment = orderLineTemplate.content.cloneNode(true);
    orderLinesRoot.append(fragment);
    reindexOrderLines();
  });

  reindexOrderLines();

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
