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
