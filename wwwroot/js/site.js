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
