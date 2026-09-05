// Dynamic add/remove rows for the intake form's history lists (allergies/conditions/
// medications/surgeries) — plain JS, no server round-trip needed just to add a text input.
document.addEventListener("click", (event) => {
  const addButton = event.target.closest("[data-list-add]");
  if (addButton) {
    const container = addButton.closest("[data-list]");
    const rows = container.querySelectorAll(".dynamic-list__row");
    const lastRow = rows[rows.length - 1];
    const newRow = lastRow.cloneNode(true);
    newRow.querySelectorAll("input").forEach((input) => (input.value = ""));
    container.insertBefore(newRow, addButton);
    return;
  }

  const removeButton = event.target.closest(".dynamic-list__remove");
  if (removeButton) {
    const container = removeButton.closest("[data-list]");
    const rows = container.querySelectorAll(".dynamic-list__row");
    if (rows.length > 1) {
      removeButton.closest(".dynamic-list__row").remove();
    } else {
      removeButton.closest(".dynamic-list__row").querySelectorAll("input").forEach((input) => (input.value = ""));
    }
  }
});

// Formats phone inputs as (XXX) XXX-XXXX while typing. Storage/validation is always done
// server-side against the canonical E.164 form — this is presentation only.
function formatPhoneInput(input) {
  const digits = input.value.replace(/\D/g, "").slice(0, 10);
  let formatted = digits;
  if (digits.length > 6) {
    formatted = `(${digits.slice(0, 3)}) ${digits.slice(3, 6)}-${digits.slice(6)}`;
  } else if (digits.length > 3) {
    formatted = `(${digits.slice(0, 3)}) ${digits.slice(3)}`;
  } else if (digits.length > 0) {
    formatted = `(${digits}`;
  }
  input.value = formatted;
}

document.addEventListener("input", (event) => {
  if (event.target.matches("[data-phone-mask]")) {
    formatPhoneInput(event.target);
  }
});
