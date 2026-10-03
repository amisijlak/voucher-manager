function escapeAttr(value) {
    return String(value ?? '')
        .replaceAll('&', '&amp;')
        .replaceAll('"', '&quot;')
        .replaceAll('<', '&lt;')
        .replaceAll('>', '&gt;');
}

function formatAmount(value, currency) {
    if (value == null || String(value).trim() === '') return '';
    const number = Number(String(value).replaceAll(',', ''));
    if (!Number.isFinite(number)) return '';
    const usd = String(currency || 'UGX').toUpperCase() === 'USD';
    return number.toLocaleString('en-US', {
        minimumFractionDigits: usd ? 2 : 0,
        maximumFractionDigits: usd ? 2 : 0
    });
}

function requisitionLineHtml(line) {
    const currency = (line && line.currency) || 'UGX';
    const budgetLineId = line && line.budgetLineId ? line.budgetLineId : '';
    const quantity = line && line.quantity != null ? line.quantity : 1;
    return `<tr>
        <td class="line-no"></td>
        <td><input class="form-control" name="Input.Lines[0].Description" value="${escapeAttr(line && line.description)}" /></td>
        <td><input class="form-control text-end" name="Input.Lines[0].Quantity" value="${escapeAttr(quantity)}" type="number" min="0" step="0.01" /></td>
        <td><input class="form-control text-end amount-input" name="Input.Lines[0].UnitAmount" value="${escapeAttr(formatAmount(line && line.unitAmount, currency))}" inputmode="decimal" /></td>
        <td>
            <select class="form-select" name="Input.Lines[0].Currency">
                <option value="UGX" ${currency === 'UGX' ? 'selected' : ''}>UGX</option>
                <option value="USD" ${currency === 'USD' ? 'selected' : ''}>USD</option>
            </select>
        </td>
        <td class="text-end">
            <input type="hidden" name="Input.Lines[0].BudgetLineId" value="${escapeAttr(budgetLineId)}" />
            <button class="btn btn-danger btn-sm" type="button" onclick="removeRequisitionLine(this)" title="Remove item"><i class="bi bi-trash"></i></button>
        </td>
    </tr>`;
}

function renumberRequisitionLines() {
    const rows = document.querySelectorAll('#lineTable tbody tr');
    rows.forEach((row, index) => {
        const number = row.querySelector('.line-no');
        if (number) number.textContent = String(index + 1);
        row.querySelectorAll('[name]').forEach(input => {
            input.name = input.name.replace(/Lines\[\d+\]/, 'Lines[' + index + ']');
        });
    });
}

function addRequisitionLine(line) {
    const body = document.querySelector('#lineTable tbody');
    body.insertAdjacentHTML('beforeend', requisitionLineHtml(line || {}));
    renumberRequisitionLines();
}

function removeRequisitionLine(button) {
    const body = document.querySelector('#lineTable tbody');
    button.closest('tr').remove();
    if (body.rows.length === 0) {
        body.insertAdjacentHTML('beforeend', requisitionLineHtml({}));
    }
    renumberRequisitionLines();
}

function fillRequisitionFromBudget() {
    const periodSelect = document.getElementById('budgetPeriod');
    const body = document.querySelector('#lineTable tbody');
    if (!periodSelect || !body) return;

    const catalog = window.requisitionBudgetCatalog || { periods: [], lines: [] };
    const periodId = periodSelect.value;
    const period = catalog.periods.find(item => String(item.id) === String(periodId));
    if (period) {
        const start = document.getElementById('periodStart');
        const end = document.getElementById('periodEnd');
        if (start) start.value = period.start;
        if (end) end.value = period.end;
    }

    const lines = periodId
        ? catalog.lines.filter(item => String(item.periodId) === String(periodId))
        : [];
    body.innerHTML = '';
    if (lines.length === 0) {
        body.insertAdjacentHTML('beforeend', requisitionLineHtml({}));
    } else {
        lines.forEach(line => body.insertAdjacentHTML('beforeend', requisitionLineHtml({
            description: line.description,
            quantity: 1,
            unitAmount: line.amount,
            currency: line.currency,
            budgetLineId: line.id
        })));
    }
    renumberRequisitionLines();
}

function initRequisitionItems() {
    const table = document.getElementById('lineTable');
    if (!table) return;
    renumberRequisitionLines();
    table.querySelectorAll('.amount-input').forEach(input => {
        const currency = input.closest('tr')?.querySelector('select[name*="Currency"]')?.value;
        input.value = formatAmount(input.value, currency);
    });
    table.addEventListener('focusout', event => {
        if (!event.target.classList.contains('amount-input')) return;
        const currency = event.target.closest('tr')?.querySelector('select[name*="Currency"]')?.value;
        event.target.value = formatAmount(event.target.value, currency);
    });
    table.addEventListener('change', event => {
        if (!event.target.matches('select[name*="Currency"]')) return;
        const amount = event.target.closest('tr')?.querySelector('.amount-input');
        if (amount) amount.value = formatAmount(amount.value, event.target.value);
    });
    document.getElementById('budgetPeriod')?.addEventListener('change', fillRequisitionFromBudget);
}

document.addEventListener('submit', event => {
    event.target.querySelectorAll?.('.amount-input')?.forEach(input => {
        input.value = String(input.value).replaceAll(',', '');
    });
});
