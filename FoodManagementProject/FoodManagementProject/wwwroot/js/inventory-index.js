(function () {
    const list = document.getElementById("inv-list");

    // nothing to do when inventory is empty
    if (!list) {
        return;
    }

    const rows = Array.from(list.querySelectorAll(".inv-item"));
    const emptyMessage = document.getElementById("inv-empty");
    const shownLabel = document.getElementById("inv-shown");
    const searchInput = document.getElementById("inv-search");
    const sortSelect = document.getElementById("inv-sort");

    // everything the user can change in the toolbar (filters, sort orders, display mode)
    const state = { category: "all", status: "all", search: "", sort: "expiry", view: "list" };

    // comparers reads the data-* attributes written in the view
    const byName = (a, b) => a.dataset.name.localeCompare(b.dataset.name);
    const byExpiry = (a, b) => Number(a.dataset.days) - Number(b.dataset.days) || byName(a, b);
    const categoryOrder = (a, b) => a.dataset.categoryName.localeCompare(b.dataset.categoryName);
    const byCategory = (a, b) => categoryOrder(a, b) || byName(a, b);

    function compare(a, b) {
        if (state.view === "category") {
            // grouped view - categories first, then the chosen order inside each category
            return categoryOrder(a, b) || (state.sort === "name" ? byName(a, b) : byExpiry(a, b));
        }
        if (state.sort === "name") return byName(a, b);
        if (state.sort === "category") return byCategory(a, b);

        return byExpiry(a, b);
    }

    function matches(row) {
        // a row is visible only if it pases all three filters
        return (state.category === "all" || row.dataset.category === state.category) && (state.status === "all" || row.dataset.status === state.status) && row.dataset.search.includes(state.search);
    }

    function render() {
        // removing the category headings of the previous render
        list.querySelectorAll(".inv-heading").forEach(heading => heading.remove());

        let visible = 0;
        let lastCategory = null;

        for (const row of rows.slice().sort(compare)) {
            const show = matches(row);
            row.hidden = !show;
            list.appendChild(row); // appending moves the row, which applies the new order

            if (show) {
                visible++;

                // a heading before the first row of every category in the grouped view
                if (state.view === "category" && row.dataset.categoryName !== lastCategory) {
                    lastCategory = row.dataset.categoryName;
                    const heading = document.createElement("li");
                    heading.className = "inv-heading";
                    heading.textContent = lastCategory;
                    list.insertBefore(heading, row);
                }
            }
        }

        // updating the empty state and the count after applying all the filters
        emptyMessage.hidden = visible !== 0;
        shownLabel.textContent = "Showing " + visible + " of " + rows.length;
    }

    function bindButtons(selector, key, dataName) {
        const buttons = Array.from(document.querySelectorAll(selector));

        for (const button of buttons) {
            button.addEventListener("click", function () {
                // reading the clicked button's value and updating the correspodning setting
                state[key] = button.dataset[dataName];

                // updating both the visual styling and teh accessible pressed state
                for (const other of buttons) {
                    const active = other === button;
                    other.classList.toggle("is-active", active);
                    other.setAttribute("aria-pressed", String(active));
                }

                render();
            });
        }
    }

    bindButtons(".inv-tab", "category", "category");
    bindButtons(".chip-filter", "status", "status");
    bindButtons(".inv-toggle button", "view", "view");

    // filter as the user types, the view should write data-search in lowercase
    searchInput.addEventListener("input", function () {
        state.search = searchInput.value.trim().toLowerCase();
        render();
    });

    sortSelect.addEventListener("change", function () {
        state.sort = sortSelect.value;
        render();
    });

    // applying the default filters and expiry sort when the page first loads
    render();

    const selectToggle = document.getElementById("inv-select-toggle");
    const bulkBar = document.getElementById("inv-bulk-bar");

    if (selectToggle && bulkBar) {
        const bulkCount = document.getElementById("inv-bulk-count");
        const selectAllButton = document.getElementById("inv-select-all");
        const needsSelection = Array.from(bulkBar.querySelectorAll("[data-needs-selection]"));

        // only rows that are currently shown count, a filter can't act on hidden rows
        function selectedIds() {
            return rows.filter(row => !row.hidden).map(row => row.querySelector(".inv-select")).filter(box => box.checked).map(box => box.value);
        }

        function updateBulk() {
            const count = selectedIds().length;
            bulkCount.textContent = count + " selected";
            needsSelection.forEach(button => { button.disabled = count === 0; });
        }

        selectToggle.addEventListener("click", function () {
            const entering = !list.classList.contains("inv-list--selecting");

            list.classList.toggle("inv-list--selecting", entering);
            bulkBar.hidden = !entering;
            selectToggle.textContent = entering ? "Cancel selection" : "Select";

            if (!entering) {
                list.querySelectorAll(".inv-select").forEach(box => { box.checked = false; });
            }

            updateBulk();
        });

        list.addEventListener("change", function (event) {
            if (event.target.matches(".inv-select")) {
                updateBulk();
            }
        });

        selectAllButton.addEventListener("click", function () {
            rows.filter(row => !row.hidden).forEach(row => { row.querySelector(".inv-select").checked = true; });
            updateBulk();
        });

        // filters change what is shown so the count has to follow
        document.querySelectorAll(".inv-tab, .chip-filter, .inv-toggle button").forEach(button => button.addEventListener("click", updateBulk));
        searchInput.addEventListener("input", updateBulk);

        // both bulk forms send the selected ingredient ids, the remove form asks for confirmation first
        for (const form of bulkBar.querySelectorAll("form")) {
            form.addEventListener("submit", function (event) {
                const ids = selectedIds();

                if (ids.length === 0) {
                    event.preventDefault();
                    return;
                }

                if (form.dataset.confirm && !confirm(form.dataset.confirm)) {
                    event.preventDefault();
                    return;
                }

                form.querySelectorAll("input[name='ingredientIds']").forEach(input => input.remove());

                for (const id of ids) {
                    const input = document.createElement("input");
                    input.type = "hidden";
                    input.name = "ingredientIds";
                    input.value = id;
                    form.appendChild(input);

                }
            });
        }

    }
})();
