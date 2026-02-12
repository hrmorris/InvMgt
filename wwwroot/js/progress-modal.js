/**
 * Global Progress Modal Component
 * Provides a reusable progress modal with circular SVG progress indicator,
 * percentage display, stage messages, and activity log for all long-running
 * operations across the application.
 *
 * Usage:
 *   const progress = new ProgressModal({ title: 'Processing...', color: '#ffc107' });
 *   progress.show();
 *   progress.updateStage(25, 'Uploading file...', 'cloud-arrow-up');
 *   // After async work:
 *   progress.complete('Done!', '/redirect/url');
 *   // On error:
 *   progress.error('Something went wrong');
 *
 *   // Or use the auto-staged fetch helper:
 *   ProgressModal.runWithProgress({
 *       title: 'AI Analysis',
 *       url: '/AiImport/AnalyzeDocument',
 *       formData: fd,
 *       stages: [...],
 *       onSuccess: (result) => { ... },
 *       onError: (msg) => { ... }
 *   });
 */

class ProgressModal {
    constructor(options = {}) {
        this.title = options.title || 'Processing...';
        this.color = options.color || '#ffc107';
        this.colorClass = options.colorClass || 'warning';
        this.icon = options.icon || 'cpu';
        this.stageInterval = null;
        this._ensureModalExists();
    }

    _ensureModalExists() {
        if (document.getElementById('globalProgressModal')) return;

        const modalHtml = `
        <div class="modal fade" id="globalProgressModal" tabindex="-1" data-bs-backdrop="static" data-bs-keyboard="false">
            <div class="modal-dialog modal-dialog-centered modal-lg">
                <div class="modal-content">
                    <div class="modal-header" id="gpm-header">
                        <h5 class="modal-title" id="gpm-title"><i class="bi bi-cpu me-2"></i>Processing...</h5>
                    </div>
                    <div class="modal-body text-center py-4">
                        <div class="position-relative d-inline-block mb-4">
                            <svg width="160" height="160" viewBox="0 0 160 160" class="circular-progress">
                                <circle cx="80" cy="80" r="70" fill="none" stroke="#e9ecef" stroke-width="10"></circle>
                                <circle id="gpm-circle" cx="80" cy="80" r="70" fill="none" stroke="#ffc107" stroke-width="10"
                                        stroke-dasharray="439.82" stroke-dashoffset="439.82" stroke-linecap="round"
                                        transform="rotate(-90, 80, 80)" style="transition: stroke-dashoffset 0.5s ease;"></circle>
                            </svg>
                            <div class="position-absolute top-50 start-50 translate-middle text-center">
                                <span id="gpm-percent" class="display-6 fw-bold">0%</span>
                            </div>
                        </div>
                        <div id="gpm-status" class="mb-3">
                            <h6 class="text-muted"><i class="bi bi-hourglass-split me-2"></i>Preparing...</h6>
                        </div>
                        <div class="text-start border rounded p-3 bg-light" style="max-height: 200px; overflow-y: auto;" id="gpm-log-container">
                            <div class="small text-muted" id="gpm-log">
                                <div><i class="bi bi-hourglass-split text-warning me-1"></i> Waiting to start...</div>
                            </div>
                        </div>
                    </div>
                    <div class="modal-footer d-none" id="gpm-footer">
                        <button type="button" class="btn btn-secondary" data-bs-dismiss="modal">Close</button>
                        <button type="button" class="btn btn-warning" id="gpm-retry" onclick="location.reload()">Try Again</button>
                    </div>
                </div>
            </div>
        </div>
        <div class="modal fade" id="globalErrorModal" tabindex="-1">
            <div class="modal-dialog modal-dialog-centered">
                <div class="modal-content">
                    <div class="modal-header bg-danger text-white">
                        <h5 class="modal-title"><i class="bi bi-exclamation-triangle me-2"></i>Error</h5>
                        <button type="button" class="btn-close btn-close-white" data-bs-dismiss="modal"></button>
                    </div>
                    <div class="modal-body">
                        <p id="gpm-error-message" class="mb-0"></p>
                    </div>
                    <div class="modal-footer">
                        <button type="button" class="btn btn-secondary" data-bs-dismiss="modal">Close</button>
                        <button type="button" class="btn btn-primary" onclick="location.reload()">Try Again</button>
                    </div>
                </div>
            </div>
        </div>`;

        const container = document.createElement('div');
        container.innerHTML = modalHtml;
        document.body.appendChild(container);
    }

    show() {
        const header = document.getElementById('gpm-header');
        const title = document.getElementById('gpm-title');
        const circle = document.getElementById('gpm-circle');
        const footer = document.getElementById('gpm-footer');
        const log = document.getElementById('gpm-log');

        header.className = `modal-header bg-${this.colorClass} ${['warning', 'light'].includes(this.colorClass) ? 'text-dark' : 'text-white'}`;
        title.innerHTML = `<i class="bi bi-${this.icon} me-2"></i>${this.title}`;
        circle.setAttribute('stroke', this.color);
        footer.classList.add('d-none');
        log.innerHTML = '<div><i class="bi bi-hourglass-split text-warning me-1"></i> Waiting to start...</div>';

        this._setProgress(0);
        this._setStatus('Preparing...', 'hourglass-split');

        this._modal = new bootstrap.Modal(document.getElementById('globalProgressModal'));
        this._modal.show();
    }

    hide() {
        this._stopStages();
        if (this._modal) {
            this._modal.hide();
        }
    }

    updateStage(percent, message, icon) {
        this._setProgress(percent);
        this._setStatus(message, icon || 'arrow-right');
        this._addLog(message, icon || 'arrow-right', percent >= 100 ? 'text-success' : 'text-primary');
    }

    complete(message, redirectUrl, delay) {
        this._stopStages();
        this._setProgress(100);
        this._setStatus(message, 'trophy');
        this._addLog(message, 'check-circle-fill', 'text-success');

        if (redirectUrl) {
            setTimeout(() => {
                this.hide();
                window.location.href = redirectUrl;
            }, delay || 1500);
        }
    }

    error(message) {
        this._stopStages();
        this.hide();
        document.getElementById('gpm-error-message').textContent = message;
        const errorModal = new bootstrap.Modal(document.getElementById('globalErrorModal'));
        errorModal.show();
    }

    showFooter() {
        document.getElementById('gpm-footer').classList.remove('d-none');
    }

    _setProgress(percent) {
        const circle = document.getElementById('gpm-circle');
        const percentEl = document.getElementById('gpm-percent');
        const circumference = 2 * Math.PI * 70; // 439.82
        const offset = circumference - (percent / 100) * circumference;
        circle.style.strokeDashoffset = offset;
        percentEl.textContent = Math.round(percent) + '%';
    }

    _setStatus(message, icon) {
        document.getElementById('gpm-status').innerHTML =
            `<h6 class="text-muted"><i class="bi bi-${icon} me-2"></i>${message}</h6>`;
    }

    _addLog(message, icon, colorClass) {
        const log = document.getElementById('gpm-log');
        const entry = document.createElement('div');
        entry.className = 'mb-1';
        const time = new Date().toLocaleTimeString('en-GB', { hour: '2-digit', minute: '2-digit', second: '2-digit' });
        entry.innerHTML = `<span class="text-muted me-1">[${time}]</span><i class="bi bi-${icon} ${colorClass || 'text-primary'} me-1"></i> ${message}`;
        log.appendChild(entry);
        const container = document.getElementById('gpm-log-container');
        container.scrollTop = container.scrollHeight;
    }

    startAutoStages(stages, intervalMs) {
        let index = 0;
        const ms = intervalMs || 3500;
        this.stageInterval = setInterval(() => {
            if (index < stages.length) {
                this.updateStage(stages[index].percent, stages[index].msg, stages[index].icon);
                index++;
            }
        }, ms);
    }

    _stopStages() {
        if (this.stageInterval) {
            clearInterval(this.stageInterval);
            this.stageInterval = null;
        }
    }

    /**
     * Static helper to run a fetch with automatic progress stages.
     * @param {Object} opts
     * @param {string} opts.title - Modal title
     * @param {string} opts.url - POST endpoint URL
     * @param {FormData} opts.formData - Form data to send
     * @param {Array} opts.stages - [{percent, msg, icon}, ...]
     * @param {Function} opts.onSuccess - Callback on success: (result) => {}
     * @param {Function} opts.onError - Optional callback on error: (message) => {}
     * @param {string} opts.color - Progress circle color (default: #ffc107)
     * @param {string} opts.colorClass - BS color class (default: warning)
     * @param {string} opts.icon - Header icon (default: cpu)
     * @param {number} opts.stageInterval - ms between stages (default: 3500)
     * @param {boolean} opts.confirm - Show confirm dialog first (default: false)
     * @param {string} opts.confirmMessage - Confirm dialog message
     */
    static async runWithProgress(opts) {
        if (opts.confirm) {
            if (!window.confirm(opts.confirmMessage || 'Start this operation?')) return;
        }

        const progress = new ProgressModal({
            title: opts.title || 'Processing...',
            color: opts.color || '#ffc107',
            colorClass: opts.colorClass || 'warning',
            icon: opts.icon || 'cpu'
        });

        progress.show();

        const stages = opts.stages || ProgressModal.defaultStages();
        progress.startAutoStages(stages, opts.stageInterval || 3500);

        try {
            const fetchOpts = Object.assign({
                method: 'POST',
                body: opts.formData,
                credentials: 'same-origin'
            }, opts.fetchOptions || {});

            const response = await fetch(opts.url, fetchOpts);

            progress._stopStages();

            // Check for auth redirect
            if (response.redirected || response.url.includes('/Account/Login')) {
                progress.error('Session expired. Please refresh the page and log in again.');
                return;
            }

            // Validate JSON response
            const contentType = response.headers.get('content-type');
            if (!contentType || !contentType.includes('application/json')) {
                throw new Error('Server returned non-JSON response. You may need to log in again.');
            }

            const result = await response.json();

            if (result.success) {
                if (opts.onSuccess) {
                    opts.onSuccess(result, progress);
                } else {
                    // Default success behavior
                    const redirectUrl = result.redirectUrl || result.redirect;
                    progress.complete(result.message || 'Completed successfully!', redirectUrl);
                }
            } else {
                progress.error(result.message || 'An unknown error occurred.');
                if (opts.onError) opts.onError(result.message);
            }
        } catch (err) {
            progress._stopStages();
            progress.error(err.message || 'A network error occurred. Please try again.');
            if (opts.onError) opts.onError(err.message);
        }
    }

    // Pre-built stage sets for common operations
    static aiAnalysisStages() {
        return [
            { percent: 5, msg: 'Preparing document for analysis...', icon: 'file-earmark-text' },
            { percent: 12, msg: 'Uploading to AI engine...', icon: 'cloud-arrow-up' },
            { percent: 22, msg: 'Running OCR / text extraction...', icon: 'eye' },
            { percent: 35, msg: 'Detecting invoice fields...', icon: 'search' },
            { percent: 48, msg: 'Extracting invoice number & dates...', icon: 'calendar-date' },
            { percent: 58, msg: 'Extracting amounts & line items...', icon: 'calculator' },
            { percent: 68, msg: 'Identifying supplier/customer...', icon: 'building' },
            { percent: 78, msg: 'Matching against database records...', icon: 'diagram-3' },
            { percent: 86, msg: 'Validating extracted data...', icon: 'shield-check' },
            { percent: 93, msg: 'Finalizing results...', icon: 'check2-all' }
        ];
    }

    static multiPageStages() {
        return [
            { percent: 3, msg: 'Uploading PDF document...', icon: 'cloud-arrow-up' },
            { percent: 8, msg: 'Document uploaded successfully', icon: 'check-circle' },
            { percent: 12, msg: 'Analyzing document structure...', icon: 'file-earmark-text' },
            { percent: 18, msg: 'Detecting invoice page boundaries...', icon: 'scissors' },
            { percent: 28, msg: 'Identifying individual invoices...', icon: 'search' },
            { percent: 40, msg: 'Extracting invoice data (batch 1)...', icon: 'cpu' },
            { percent: 55, msg: 'Extracting invoice data (batch 2)...', icon: 'cpu' },
            { percent: 68, msg: 'Parsing line items and amounts...', icon: 'calculator' },
            { percent: 78, msg: 'Matching suppliers and customers...', icon: 'diagram-3' },
            { percent: 85, msg: 'Validating and deduplicating...', icon: 'shield-check' },
            { percent: 92, msg: 'Finalizing extraction results...', icon: 'check2-all' }
        ];
    }

    static reprocessStages() {
        return [
            { percent: 5, msg: 'Loading stored document...', icon: 'file-earmark-arrow-down' },
            { percent: 12, msg: 'Analyzing document structure...', icon: 'file-earmark-text' },
            { percent: 20, msg: 'Detecting invoice page boundaries...', icon: 'scissors' },
            { percent: 35, msg: 'Identifying individual invoices...', icon: 'search' },
            { percent: 50, msg: 'Extracting invoice data (batch 1)...', icon: 'cpu' },
            { percent: 65, msg: 'Extracting invoice data (batch 2)...', icon: 'cpu' },
            { percent: 78, msg: 'Parsing line items and amounts...', icon: 'calculator' },
            { percent: 88, msg: 'Matching suppliers and customers...', icon: 'diagram-3' },
            { percent: 94, msg: 'Finalizing extraction results...', icon: 'check2-all' }
        ];
    }

    static paymentStages() {
        return [
            { percent: 5, msg: 'Preparing document for analysis...', icon: 'file-earmark-text' },
            { percent: 15, msg: 'Uploading to AI engine...', icon: 'cloud-arrow-up' },
            { percent: 25, msg: 'Running OCR / text extraction...', icon: 'eye' },
            { percent: 40, msg: 'Detecting payment details...', icon: 'credit-card' },
            { percent: 55, msg: 'Extracting bank & account info...', icon: 'bank' },
            { percent: 68, msg: 'Extracting amounts & reference...', icon: 'calculator' },
            { percent: 80, msg: 'Matching against invoices...', icon: 'diagram-3' },
            { percent: 90, msg: 'Validating extracted data...', icon: 'shield-check' },
            { percent: 95, msg: 'Finalizing results...', icon: 'check2-all' }
        ];
    }

    static batchStages() {
        return [
            { percent: 5, msg: 'Preparing batch operation...', icon: 'collection' },
            { percent: 15, msg: 'Processing documents...', icon: 'file-earmark-text' },
            { percent: 30, msg: 'Running AI extraction...', icon: 'cpu' },
            { percent: 50, msg: 'Extracting invoice details...', icon: 'receipt' },
            { percent: 65, msg: 'Matching entities...', icon: 'diagram-3' },
            { percent: 80, msg: 'Validating results...', icon: 'shield-check' },
            { percent: 90, msg: 'Saving records...', icon: 'floppy' },
            { percent: 95, msg: 'Finalizing...', icon: 'check2-all' }
        ];
    }

    static defaultStages() {
        return [
            { percent: 10, msg: 'Starting operation...', icon: 'gear' },
            { percent: 30, msg: 'Processing...', icon: 'cpu' },
            { percent: 50, msg: 'Working...', icon: 'arrow-repeat' },
            { percent: 70, msg: 'Almost there...', icon: 'hourglass-split' },
            { percent: 85, msg: 'Finalizing...', icon: 'check2-all' }
        ];
    }

    /**
     * Global toast notification
     */
    static toast(message, type) {
        const existing = document.querySelector('.gpm-toast');
        if (existing) existing.remove();

        const toast = document.createElement('div');
        toast.className = 'gpm-toast position-fixed bottom-0 end-0 p-3';
        toast.style.zIndex = '1100';
        const iconMap = { success: 'check-circle', danger: 'exclamation-triangle', warning: 'exclamation-circle', info: 'info-circle' };
        toast.innerHTML = `
            <div class="toast show bg-${type || 'info'} text-white" role="alert" style="min-width: 300px;">
                <div class="toast-body">
                    <i class="bi bi-${iconMap[type] || 'info-circle'} me-2"></i>${message}
                </div>
            </div>`;
        document.body.appendChild(toast);
        setTimeout(() => toast.remove(), 4000);
    }
}
