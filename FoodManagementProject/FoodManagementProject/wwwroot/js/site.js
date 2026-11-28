(function () {
    // finding the shared menu, background overlay and desktop/mobile add button
    const addMenu = document.getElementById("add-menu");
    const addOverlay = document.getElementById("add-overlay");
    const addTriggers = document.querySelectorAll(".add-trigger");

    // pages without the menu (admin, moderator, login) do nothing 
    if (!addMenu || !addOverlay) {
        return;
    }

    function setAddMenu(open) {
        // showing or hiding the menu and overlay together
        addMenu.hidden = !open;
        addOverlay.hidden = !open;

        for (const trigger of addTriggers) {
            trigger.setAttribute("aria-expanded", open);

            // changing the mobile button from a plus to a close icon
            if (trigger.classList.contains("tabbar__fab")) {
                trigger.querySelector("i").className = open ? "ti ti-x" : "ti ti-plus";
            }
        }
    }

    for (const trigger of addTriggers) {
        trigger.addEventListener("click", function () {
            // opening the menu if hidden, or closing it if already visible
            setAddMenu(addMenu.hidden);
        });
    }

    // closing the menu when the user clicks outside of it
    addOverlay.addEventListener("click", function () {
        setAddMenu(false);
    });

    // allowing the user to close the menu by pressing the Escape key
    document.addEventListener("keydown", function (event) {
        if (event.key === "Escape") {
            setAddMenu(false);
        }
    });


})();

// adding a hide/show button to every password field
document.querySelectorAll('input[type="password"]').forEach(function (input) {
    const wrapper = document.createElement("div");
    wrapper.className = "password-field";
    input.parentNode.insertBefore(wrapper, input);
    wrapper.appendChild(input);

    const button = document.createElement("button");
    button.type = "button";
    button.className = "password-toggle";
    button.innerHTML = '<i class="ti ti-eye" aria-hidden=true></i>'; // adding an eye icon for password show/hide
    button.setAttribute("aria-label", "Show password");
    button.setAttribute("aria-pressed", "false");

    button.addEventListener("click", function () {
        const show = input.type === "password";
        input.type = show ? "text" : "password";
        input.spellcheck = false;
        button.querySelector("i").className = show ? "ti ti-eye-off" : "ti ti-eye"; // adding an eye icon for password show/hide
        button.setAttribute("aria-label", show ? "Hide password" : "Show password");
        button.setAttribute("aria-pressed", String(show));
    });

    wrapper.appendChild(button);
});

document.querySelectorAll(".password-rules").forEach(function (list) {
    const input = document.getElementById(list.dataset.for);
    if (!input) {
        return;
    }

    const checks = {
        length: function (value) { return value.length >= 8; },
        letter: function (value) { return /[A-Za-z]/.test(value); },
        number: function (value) { return /\d/.test(value); },
        special: function (value) { return /[^A-Za-z\d]/.test(value); }
    };

    function updateRules() {
        for (const item of list.querySelectorAll("li")) {
            const met = checks[item.dataset.rule](input.value);
            item.classList.toggle("is-met", met);
            item.querySelector("i").className = met ? "ti ti-circle-check" : "ti ti-circle-x";
        }
    }

    input.addEventListener("input", updateRules);
    updateRules(); // so the sicks are rigth when the page reloads with an error

});
