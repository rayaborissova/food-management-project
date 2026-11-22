(function () {
    const form = document.getElementById("recipe-form");

    if (!form) {
        return;
    }

    const lines = document.getElementById("recipe-lines");
    const emptyMessage = document.getElementById("recipe-lines-empty");
    const unitOptions = document.getElementById("unit-options");
    const searchInput = document.getElementById("ingredient-search");
    const results = document.getElementById("ingredient-results");
    const searchUrl = form.dataset.searchUrl;
    const initialLines = JSON.parse(document.getElementById("recipe-initial-lines").textContent || "[]");

    // the model binding needs the field names to count 0, 1, 2... without gaps
    function reindex() {
        Array.from(lines.rows).forEach(function (row, index) {
            row.querySelectorAll("[data-field]").forEach(function (element) {
                element.name = "Lines[" + index + "]." + element.dataset.field;
            });
        });

        emptyMessage.hidden = lines.rows.length > 0;
    }

    function field(tag, fieldName) {
        const element = document.createElement(tag);
        element.dataset.field = fieldName;
        return element;
    }

    function addLine(line) {
        // one line per ingredient
        const present = Array.from(lines.querySelectorAll("input[data-field='IngredientId']")).some(function (input) { return input.value === String(line.ingredientId); });
        if (present) {
            return;
        }

        const row = document.createElement("tr");

        const nameCell = document.createElement("td");
        nameCell.textContent = line.ingredientName; // textContext keeps names from being read as html
        const idInput = field("input", "IngredientId");
        idInput.type = "hidden";
        idInput.value = line.ingredientId;
        nameCell.appendChild(idInput);

        const quantityCell = document.createElement("td");
        const quantity = field("input", "Quantity");
        quantity.type = "number";
        quantity.min = "0.01";
        quantity.step = "any";
        quantity.className = "input";
        quantity.setAttribute("aria-label", "Quantity for " + line.ingredientName);
        quantity.value = line.quantity ?? "";
        quantityCell.appendChild(quantity);

        const unitCell = document.createElement("td");
        const unit = field("select", "UnitId");
        unit.className = "input";
        unit.setAttribute("aria-label", "Unit for " + line.ingredientName);
        unit.innerHTML = unitOptions.innerHTML;
        unit.value = line.unitId ?? "";
        unitCell.appendChild(unit);

        const noteCell = document.createElement("td");
        const note = field("input", "Note");
        note.type = "text";
        note.maxLength = 100;
        note.className = "input";
        note.setAttribute("aria-label", "Note for " + line.ingredientName);
        note.value = line.note ?? "";
        noteCell.appendChild(note);

        const removeCell = document.createElement("td");
        const remove = document.createElement("button");
        remove.type = "button";
        remove.className = "btn btn--danger btn--sm";
        remove.textContent = "Remove";
        remove.addEventListener("click", function () {
            row.remove();
            reindex();
        });
        removeCell.appendChild(remove);

        row.append(nameCell, quantityCell, unitCell, noteCell, removeCell);
        lines.appendChild(row);
        reindex();
    }

    initialLines.forEach(function (line) {
        addLine({
            ingredientId: line.ingredientId,
            ingredientName: line.ingredientName,
            quantity: line.quantity,
            unitId: line.unitId,
            note: line.note
        });
    });

    // searching the catalogue while typing
    let timer;
    searchInput.addEventListener("input", function () {
        clearTimeout(timer);
        const query = searchInput.value.trim();

        if (query.length < 2) {
            results.replaceChildren();
            return;
        }

        timer = setTimeout(async function () {
            const response = await fetch(searchUrl + "?q=" + encodeURIComponent(query));
            const items = await response.json();

            results.replaceChildren(...items.map(function (item) {
                const li = document.createElement("li");
                const button = document.createElement("button");
                button.type = "button";
                button.className = "btn btn--outline btn--sm";
                button.textContent = item.name + " (" + item.category + ")";
                button.addEventListener("click", function () {
                    addLine({ ingredientId: item.id, ingredientName: item.name });
                    searchInput.value = "";
                    results.replaceChildren();
                    searchInput.focus();
                });
                li.appendChild(button);
                return li;
            }));
        }, 250);
    });

    searchInput.addEventListener("keydown", function (event) {
        if (event.key === "Enter") {
            event.preventDefault();
        }
    });

    form.addEventListener("submit", reindex);
})();