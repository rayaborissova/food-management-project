// adding a hide/show button to every password field
document.querySelectorAll('input[type="password"]').forEach(function (input) {
    const wrapper = document.createElement("div");
    wrapper.className = "password-field";
    input.parentNode.insertBefore(wrapper, input);
    wrapper.appendChild(input);

    const button = document.createElement("button");
    button.type = "button";
    button.className = "btn btn--outline btn--sm password--toggle";
    button.textContent = "Show";
    button.setAttribute("aria-label", "Show password");
    button.setAttribute("aria-pressed", "false");

    button.addEventListener("click", function () {
        const show = input.type === "password";
        input.type = show ? "text" : "password";
        input.spellcheck = false;
        button.textContent = show ? "Hide" : "Show";
        button.setAttribute("aria-label", show ? "Hide password" : "Show password");
        button.setAttribute("aria-pressed", String(show));
    });

    wrapper.appendChild(button);
});