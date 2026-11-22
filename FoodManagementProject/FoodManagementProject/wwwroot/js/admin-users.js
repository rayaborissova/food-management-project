(function () {
    const rows = Array.from(document.querySelectorAll(".user-row"));

    // when the page has no users there is nothing to do
    if (rows.length === 0) {
        return;
    }

    const searchInput = document.getElementById("user-search");
    const shownLabel = document.getElementById("user-shown");
    const emptyMessage = document.getElementById("user-empty");

    // all the filters in the toolbar
    const state = { role: "all", status: "all", search: "" };

    function render() {
        let visible = 0;

        for (const row of rows) {
            const show = (state.role === "all" || row.dataset.role === state.role) && (state.status === "all" || row.dataset.status === state.status) && row.dataset.search.includes(state.search);

            row.hidden = !show;
            if (show) {
                visible++;
            }
        }

        emptyMessage.hidden = visible !== 0;
        shownLabel.textContent = "Showing " + visible + " of " + rows.length;
    }

    // one handler for each group (role tabs and status chips)
    function bindButtons(selector, key, dataName) {
        const buttons = Array.from(document.querySelectorAll(selector));

        for (const button of buttons) {
            button.addEventListener("click", function () {
                state[key] = button.dataset[dataName];

                for (const other of buttons) {
                    const active = other === button;
                    other.classList.toggle("is-active", active);
                    other.setAttribute("aria-pressed", String(active));
                }

                render();
            });
        }
    }

    bindButtons(".inv-tab", "role", "role");
    bindButtons(".chip-filter", "status", "status");

    searchInput.addEventListener("input", function () {
        state.search = searchInput.value.trim().toLowerCase();
        render();
    });

    render();
})();