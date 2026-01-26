// ============================================================
// FILE: wwwroot/js/remittance.js
// PURPOSE: JavaScript for the Remittance System UI
// ============================================================

// ============================================================
// UTILITY FUNCTIONS
// ============================================================

/**
 * Format currency in Philippine Peso
 */
function formatCurrency(amount) {
    return '₱' + parseFloat(amount).toLocaleString('en-PH', {
        minimumFractionDigits: 2,
        maximumFractionDigits: 2
    });
}

/**
 * Show toast notification
 */
function showToast(message, type = 'success') {
    const toastContainer = document.getElementById('toast-container') || createToastContainer();
    
    const toast = document.createElement('div');
    toast.className = `toast-notification ${type}`;
    toast.innerHTML = `
        <i class="bi ${type === 'success' ? 'bi-check-circle-fill' : type === 'error' ? 'bi-x-circle-fill' : 'bi-info-circle-fill'}"></i>
        <span>${message}</span>
    `;
    
    toastContainer.appendChild(toast);
    
    // Animate in
    setTimeout(() => toast.classList.add('show'), 10);
    
    // Remove after 4 seconds
    setTimeout(() => {
        toast.classList.remove('show');
        setTimeout(() => toast.remove(), 300);
    }, 4000);
}

function createToastContainer() {
    const container = document.createElement('div');
    container.id = 'toast-container';
    container.style.cssText = 'position: fixed; top: 20px; right: 20px; z-index: 9999;';
    document.body.appendChild(container);
    return container;
}

// ============================================================
// CLASS TREASURER - PAYMENT COLLECTION
// ============================================================

/**
 * Collect fee payment (AJAX)
 */
async function collectFeePayment(feeId, paymentMethod = 'Cash') {
    if (!confirm('Mark this fee as PAID? The student will be notified.')) {
        return;
    }
    
    try {
        const token = document.querySelector('input[name="__RequestVerificationToken"]')?.value;
        
        const formData = new FormData();
        formData.append('feeId', feeId);
        formData.append('paymentMethod', paymentMethod);
        formData.append('__RequestVerificationToken', token);
        
        const response = await fetch('/Officer/CollectFeePayment', {
            method: 'POST',
            body: formData
        });
        
        const result = await response.json();
        
        if (result.success) {
            showToast(result.message, 'success');
            // Update UI - change button and status
            updateFeeRowUI(feeId, 'collected');
            // Refresh stats if function exists
            if (typeof refreshStats === 'function') {
                refreshStats();
            }
        } else {
            showToast(result.message, 'error');
        }
    } catch (error) {
        console.error('Error collecting payment:', error);
        showToast('An error occurred. Please try again.', 'error');
    }
}

/**
 * Revoke fee collection (AJAX)
 */
async function revokeFeeCollection(feeId) {
    if (!confirm('Revoke this payment? This will mark the fee as UNPAID again.')) {
        return;
    }
    
    try {
        const token = document.querySelector('input[name="__RequestVerificationToken"]')?.value;
        
        const formData = new FormData();
        formData.append('feeId', feeId);
        formData.append('__RequestVerificationToken', token);
        
        const response = await fetch('/Officer/RevokeFeesCollection', {
            method: 'POST',
            body: formData
        });
        
        const result = await response.json();
        
        if (result.success) {
            showToast(result.message, 'success');
            updateFeeRowUI(feeId, 'not-collected');
            if (typeof refreshStats === 'function') {
                refreshStats();
            }
        } else {
            showToast(result.message, 'error');
        }
    } catch (error) {
        console.error('Error revoking payment:', error);
        showToast('An error occurred. Please try again.', 'error');
    }
}

/**
 * Update fee row UI after action
 */
function updateFeeRowUI(feeId, status) {
    const row = document.querySelector(`[data-fee-id="${feeId}"]`);
    if (!row) return;
    
    const statusCell = row.querySelector('.status-cell');
    const actionCell = row.querySelector('.action-cell');
    
    if (status === 'collected') {
        if (statusCell) {
            statusCell.innerHTML = '<span class="collection-status collected"><i class="bi bi-check-circle me-1"></i>Collected</span>';
        }
        if (actionCell) {
            actionCell.innerHTML = `
                <button class="btn btn-revoke btn-sm" onclick="revokeFeeCollection(${feeId})">
                    <i class="bi bi-x-circle me-1"></i>Revoke
                </button>
            `;
        }
        row.classList.add('collected');
        row.classList.remove('not-collected');
    } else if (status === 'not-collected') {
        if (statusCell) {
            statusCell.innerHTML = '<span class="collection-status not-collected"><i class="bi bi-clock me-1"></i>Unpaid</span>';
        }
        if (actionCell) {
            actionCell.innerHTML = `
                <button class="btn btn-collect btn-sm" onclick="collectFeePayment(${feeId})">
                    <i class="bi bi-check-circle me-1"></i>Collect
                </button>
            `;
        }
        row.classList.remove('collected');
        row.classList.add('not-collected');
    } else if (status === 'locked') {
        if (statusCell) {
            statusCell.innerHTML = '<span class="collection-status locked"><i class="bi bi-lock me-1"></i>Remitted</span>';
        }
        if (actionCell) {
            actionCell.innerHTML = '<span class="lock-indicator"><i class="bi bi-lock-fill"></i>Locked</span>';
        }
        row.classList.add('locked');
    }
}

// ============================================================
// REMITTANCE SELECTION
// ============================================================

/**
 * Select all fees for remittance
 */
function selectAllForRemittance(feeName) {
    const checkboxes = document.querySelectorAll(`.remittance-checkbox[data-fee-name="${feeName}"]`);
    const selectAllCheckbox = document.querySelector(`#selectAll-${feeName.replace(/\s+/g, '-')}`);
    
    checkboxes.forEach(cb => {
        if (!cb.disabled) {
            cb.checked = selectAllCheckbox.checked;
        }
    });
    
    updateRemittanceSummary(feeName);
}

/**
 * Update remittance summary when selections change
 */
function updateRemittanceSummary(feeName) {
    const checkboxes = document.querySelectorAll(`.remittance-checkbox[data-fee-name="${feeName}"]:checked`);
    let totalAmount = 0;
    let totalCount = checkboxes.length;
    
    checkboxes.forEach(cb => {
        totalAmount += parseFloat(cb.dataset.amount || 0);
    });
    
    const summaryElement = document.querySelector(`#remittance-summary-${feeName.replace(/\s+/g, '-')}`);
    if (summaryElement) {
        summaryElement.innerHTML = `
            <strong>${totalCount}</strong> students selected • 
            <strong>${formatCurrency(totalAmount)}</strong> total
        `;
    }
    
    // Enable/disable remit button
    const remitButton = document.querySelector(`#remit-btn-${feeName.replace(/\s+/g, '-')}`);
    if (remitButton) {
        remitButton.disabled = totalCount === 0;
    }
}

// ============================================================
// ORG TREASURER - VALIDATION
// ============================================================

/**
 * Quick validate remittance (AJAX)
 */
async function quickValidateRemittance(remittanceId) {
    if (!confirm('Validate this remittance? The official payment date will be set to NOW for all students in this batch.')) {
        return;
    }
    
    try {
        const token = document.querySelector('input[name="__RequestVerificationToken"]')?.value;
        
        const formData = new FormData();
        formData.append('remittanceId', remittanceId);
        formData.append('__RequestVerificationToken', token);
        
        const response = await fetch('/Officer/ValidateRemittance', {
            method: 'POST',
            body: formData
        });
        
        if (response.redirected) {
            window.location.href = response.url;
        } else {
            const result = await response.json();
            if (result.success) {
                showToast(result.message, 'success');
                // Remove the remittance card from UI
                const card = document.querySelector(`[data-remittance-id="${remittanceId}"]`);
                if (card) {
                    card.style.transition = 'opacity 0.3s, transform 0.3s';
                    card.style.opacity = '0';
                    card.style.transform = 'translateX(20px)';
                    setTimeout(() => card.remove(), 300);
                }
                updatePendingCount(-1);
            } else {
                showToast(result.message, 'error');
            }
        }
    } catch (error) {
        console.error('Error validating remittance:', error);
        showToast('An error occurred. Please try again.', 'error');
    }
}

/**
 * Update pending remittance count in UI
 */
function updatePendingCount(change) {
    const countBadge = document.querySelector('.pending-count-badge');
    if (countBadge) {
        let count = parseInt(countBadge.textContent) || 0;
        count += change;
        countBadge.textContent = count;
        
        if (count <= 0) {
            countBadge.style.display = 'none';
        }
    }
}

// ============================================================
// CONFIRMATION DIALOGS
// ============================================================

/**
 * Show confirmation modal for remittance submission
 */
function showRemittanceConfirmation(feeName, totalAmount, totalStudents) {
    const modalHtml = `
        <div class="modal fade" id="remittanceConfirmModal" tabindex="-1">
            <div class="modal-dialog modal-dialog-centered">
                <div class="modal-content" style="background: #1a1a2e; border: 1px solid rgba(212, 175, 55, 0.3);">
                    <div class="modal-header border-0">
                        <h5 class="modal-title text-white">
                            <i class="bi bi-send-check me-2 text-warning"></i>Confirm Remittance
                        </h5>
                        <button type="button" class="btn-close btn-close-white" data-bs-dismiss="modal"></button>
                    </div>
                    <div class="modal-body text-center py-4">
                        <div style="width: 80px; height: 80px; background: rgba(245, 158, 11, 0.1); border-radius: 50%; display: flex; align-items: center; justify-content: center; margin: 0 auto 1rem;">
                            <i class="bi bi-send-fill fs-2 text-warning"></i>
                        </div>
                        <h5 class="text-white mb-2">Submit "${feeName}" to Org Treasurer?</h5>
                        <p class="text-muted mb-3">
                            <strong class="text-warning">${totalStudents}</strong> students • 
                            <strong class="text-warning">${formatCurrency(totalAmount)}</strong> total
                        </p>
                        <div class="alert alert-warning py-2 px-3" style="background: rgba(245, 158, 11, 0.1); border-color: rgba(245, 158, 11, 0.3);">
                            <small><i class="bi bi-exclamation-triangle me-1"></i>
                            You will <strong>NOT</strong> be able to edit these payments after submission.</small>
                        </div>
                    </div>
                    <div class="modal-footer border-0 justify-content-center">
                        <button type="button" class="btn btn-outline-secondary" data-bs-dismiss="modal">Cancel</button>
                        <button type="button" class="btn btn-remit" onclick="submitRemittance('${feeName}')">
                            <i class="bi bi-send-fill me-1"></i>Submit Remittance
                        </button>
                    </div>
                </div>
            </div>
        </div>
    `;
    
    // Remove existing modal if any
    const existingModal = document.getElementById('remittanceConfirmModal');
    if (existingModal) existingModal.remove();
    
    document.body.insertAdjacentHTML('beforeend', modalHtml);
    const modal = new bootstrap.Modal(document.getElementById('remittanceConfirmModal'));
    modal.show();
}

/**
 * Submit remittance form
 */
function submitRemittance(feeName) {
    const form = document.querySelector(`#remittance-form-${feeName.replace(/\s+/g, '-')}`);
    if (form) {
        form.submit();
    }
}

// ============================================================
// INITIALIZATION
// ============================================================

document.addEventListener('DOMContentLoaded', function() {
    // Add toast container styles
    const style = document.createElement('style');
    style.textContent = `
        .toast-notification {
            background: rgba(30, 30, 50, 0.95);
            border: 1px solid rgba(255, 255, 255, 0.1);
            border-radius: 10px;
            padding: 12px 20px;
            margin-bottom: 10px;
            display: flex;
            align-items: center;
            gap: 10px;
            color: white;
            font-size: 0.9rem;
            opacity: 0;
            transform: translateX(20px);
            transition: all 0.3s ease;
            box-shadow: 0 4px 20px rgba(0, 0, 0, 0.3);
        }
        .toast-notification.show {
            opacity: 1;
            transform: translateX(0);
        }
        .toast-notification.success {
            border-left: 4px solid #10b981;
        }
        .toast-notification.success i {
            color: #10b981;
        }
        .toast-notification.error {
            border-left: 4px solid #ef4444;
        }
        .toast-notification.error i {
            color: #ef4444;
        }
        .toast-notification.info {
            border-left: 4px solid #3b82f6;
        }
        .toast-notification.info i {
            color: #3b82f6;
        }
    `;
    document.head.appendChild(style);
    
    // Initialize any checkbox listeners
    document.querySelectorAll('.remittance-checkbox').forEach(cb => {
        cb.addEventListener('change', function() {
            updateRemittanceSummary(this.dataset.feeName);
        });
    });
    
    console.log('Remittance JS initialized');
});
