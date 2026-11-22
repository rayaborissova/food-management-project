(function () {

    const purchaseStorageKey = "shoppingListCheckedItemIds";

    const purchaseCheckboxes = document.querySelectorAll('input[name="purchasedItemIds"]');
    const purchaseForm = document.getElementById("confirm-purchase-form");
    const purchaseInputs = document.getElementById("checked-purchase-inputs");

    if (!purchaseForm || !purchaseInputs) {
        return;
    }

    let checkedItemIds = JSON.parse(sessionStorage.getItem(purchaseStorageKey) || "[]");

    //keeping only the IDs that still appear as active
    const activeItemIds = Array.from(purchaseCheckboxes, checkbox => checkbox.value);

    checkedItemIds = checkedItemIds.filter(id => activeItemIds.includes(id));

    function savePurchaseChecks() {
        sessionStorage.setItem(purchaseStorageKey, JSON.stringify(checkedItemIds));
    }

    for (const checkbox of purchaseCheckboxes) {
        //restoring the checkmark
        checkbox.checked = checkedItemIds.includes(checkbox.value);

        checkbox.addEventListener("change", function () {
            if (checkbox.checked) {
                if (!checkedItemIds.includes(checkbox.value)) {
                    checkedItemIds.push(checkbox.value);
                }
            } else {
                checkedItemIds = checkedItemIds.filter(id => id !== checkbox.value);
            }

            savePurchaseChecks();
        });
    }
    // removing the outdated IDs
    savePurchaseChecks();

    purchaseForm.addEventListener("submit", function (event) {
        if (checkedItemIds.length === 0) {
            event.preventDefault();
            alert("Check at least one purchased item.");
            return;
        }

        purchaseInputs.replaceChildren();

        for (const id of checkedItemIds) {
            const input = document.createElement("input");

            input.type = "hidden";
            input.name = "purchasedItemIds";
            input.value = id;

            purchaseInputs.appendChild(input);
        }
    });
})();