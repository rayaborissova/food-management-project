const storageKey = "inventorySelectedIngredientsIds";

// reading the stored ingredient ids, and if nothing is selected, it returns empty
let selectedIds = JSON.parse(sessionStorage.getItem(storageKey) || "[]");

// finding all ingredient checkboxes displayed on the page
const checkboxes = document.querySelectorAll('input[name="selectedIngredientIds"]');

// finding the paragraph that displays the number selected
const selectionCount = document.getElementById("selection-count");

// finding the button that clears the selection
const clearButton = document.getElementById("clear-selection");

//undo button
const undoButton = document.getElementById("undo-selection");

//review button
const reviewButton = document.getElementById("review-selected");

const reviewForm = document.getElementById("review-selected-form");

const selectedInputs = document.getElementById("selected-ingredient-inputs");

if (selectionCount && clearButton instanceof HTMLButtonElement && undoButton instanceof HTMLButtonElement && reviewButton instanceof HTMLButtonElement && reviewForm instanceof HTMLFormElement && selectedInputs) {
    let selectionBeforeClear = null;

    // updating the displayed number of selected ingredients
    const updateSelectionDisplay = () => {
        const count = selectedIds.length;
        const label = count === 1 ? "ingredient" : "ingredients";

        selectionCount.textContent = selectionBeforeClear === null ? `${count} ${label} selected` : "Selection cleared.";

        clearButton.hidden = count === 0;
        undoButton.hidden = selectionBeforeClear === null;
        reviewButton.disabled = count === 0;

        for (const checkbox of checkboxes) {
            if (!(checkbox instanceof HTMLInputElement)) continue;
            checkbox.checked = selectedIds.includes(checkbox.value);
        }
    };

    // storing the current selections and updating their count
    function saveSelection() {
        sessionStorage.setItem(storageKey, JSON.stringify(selectedIds));

        updateSelectionDisplay();
    }

    for (const checkbox of checkboxes) {
        if (!(checkbox instanceof HTMLInputElement)) continue;

        // whenever the user ticks or unticks
        checkbox.addEventListener("change", function () {
            selectionBeforeClear = null;

            // adding the id if not in selection
            if (checkbox.checked && !selectedIds.includes(checkbox.value)) {
                selectedIds.push(checkbox.value);
            } else if (!checkbox.checked) {
                // keeping every id except the unticked one
                selectedIds = selectedIds.filter(id => id !== checkbox.value);
            }

            // remember and refresh the count
            saveSelection();
        });
    }

    // when the clear button ic clicked
    clearButton.addEventListener("click", function () {

        selectionBeforeClear = [...selectedIds];

        // the remembered selection list is emptied
        selectedIds = [];

        //storing the empty list and updating count to 0
        saveSelection();
        undoButton.focus();
    });

    undoButton.addEventListener("click", function () {
        if (selectionBeforeClear === null) {
            return;
        }

        selectedIds = selectionBeforeClear;
        selectionBeforeClear = null;

        saveSelection();
        clearButton.focus();
    });

    //displaying the count after loading
    updateSelectionDisplay();


    reviewForm.addEventListener("submit", function (event) {
        if (selectedIds.length === 0) {
            event.preventDefault();
            alert("Select at least one ingredient.");
            return;
        }

        // removing inputs from earlier submission attempts
        selectedInputs.replaceChildren();

        // sending the ids of every selected ingredient, those hidden from the search as well
        for (const id of selectedIds) {
            const input = document.createElement("input");
            input.type = "hidden";
            input.name = "selectedIngredientIds";
            input.value = id;

            selectedInputs.appendChild(input);
        }

    });
}
