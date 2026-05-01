let newRowIdx = 0;
const btnConfirm = document.getElementById('btnConfirm');
const hintMessage = document.getElementById('hintMessage');
const hintText = document.getElementById('hintText');
const inputSupplier = document.getElementById('SupplierName');
const inputDocNumber = document.getElementById('DocumentNumber');
const inputIssueDate = document.getElementById('IssueDate');
const inputDueDate = document.getElementById('DueDate');
function addRow() {
    const noItemsRow = document.getElementById('noItemsRow');
    if (noItemsRow) noItemsRow.remove();

    const tbody = document.getElementById('itemsBody');
    const tr = document.createElement('tr');
    tr.className = "table-info new-item-row";
    tr.innerHTML = `
                <td><input name="NewItems[${newRowIdx}].Description" class="form-control form-control-sm desc-input" placeholder="Obavezan opis..." required oninput="validateForm()" /></td>
                <td><input name="NewItems[${newRowIdx}].Quantity" class="form-control form-control-sm text-center qty-input" type="number" value="1" oninput="calculateRow(this)" /></td>
                <td><input name="NewItems[${newRowIdx}].UnitPrice" class="form-control form-control-sm text-end price-input" type="number" step="0.01" value="0" oninput="calculateRow(this)" /></td>
                <td class="text-end fw-bold total-cell">0.00</td>
                <input type="hidden" name="NewItems[${newRowIdx}].LineTotal" class="total-hidden" value="0" />
            `;
    tbody.appendChild(tr);
    newRowIdx++;
    validateForm();
}
function calculateRow(input) {
    const tr = input.closest('tr');
    const qty = parseFloat(tr.querySelector('.qty-input').value) || 0;
    const price = parseFloat(tr.querySelector('.price-input').value) || 0;
    const total = (qty * price).toFixed(2);
    tr.querySelector('.total-cell').innerText = total;
    tr.querySelector('.total-hidden').value = total;
    validateForm();
}
function validateForm() {
    const supplierVal = inputSupplier.value.trim();
    const isSupplierOk = supplierVal !== "" && !supplierVal.toLowerCase().includes("unknown");
    inputSupplier.classList.toggle('is-invalid', !isSupplierOk);
    const docNumVal = inputDocNumber.value.trim();
    const isDocNumOk = docNumVal !== "" && docNumVal.toLowerCase() !== "unknown";
    inputDocNumber.classList.toggle('is-invalid', !isDocNumOk);
    let datesOk = true;
    if (inputIssueDate.value && inputDueDate.value) {
        const iDate = new Date(inputIssueDate.value);
        const dDate = new Date(inputDueDate.value);
        if (dDate < iDate) {
            datesOk = false;
            inputDueDate.classList.add('is-invalid');
        } else {
            inputDueDate.classList.remove('is-invalid');
        }
    }
    const newItemDescriptions = document.querySelectorAll('.desc-input');
    const existingRows = document.querySelectorAll('.existing-item');
    const hasExistingItems = existingRows.length > 0;
    const hasNewItems = newItemDescriptions.length > 0;

    let allNewItemsFilled = true;
    newItemDescriptions.forEach(input => {
        const val = input.value.trim();
        input.classList.toggle('is-invalid', val === "");
        if (val === "") allNewItemsFilled = false;
    });
    const itemsOk = hasExistingItems || (hasNewItems && allNewItemsFilled);
    if (isSupplierOk && isDocNumOk && itemsOk && datesOk) {
        btnConfirm.disabled = false;
        hintMessage.classList.add('d-none');
    } else {
        btnConfirm.disabled = true;
        hintMessage.classList.remove('d-none');

        if (!isSupplierOk) hintText.innerText = " Please fix the supplier name.";
        else if (!isDocNumOk) hintText.innerText = " Enter a valid document number.";
        else if (!datesOk) hintText.innerText = " Due date cannot be earlier than the issue date.";
        else if (!itemsOk) hintText.innerText = " You must add at least one item with a description.";
    }
}
document.addEventListener("DOMContentLoaded", validateForm);