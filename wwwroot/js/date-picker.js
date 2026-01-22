// Date Picker Configuration for DD/MM/YYYY format
// Using Flatpickr for consistent date picking across all browsers

document.addEventListener('DOMContentLoaded', function() {
    // Initialize all date inputs with Flatpickr
    const dateInputs = document.querySelectorAll('input[type="date"], .date-picker');
    
    dateInputs.forEach(function(input) {
        // Convert existing value from YYYY-MM-DD to DD/MM/YYYY if present
        if (input.value && input.value.match(/^\d{4}-\d{2}-\d{2}$/)) {
            const parts = input.value.split('-');
            input.value = `${parts[2]}/${parts[1]}/${parts[0]}`;
        }
        
        // Change input type to text for custom formatting
        input.type = 'text';
        input.placeholder = 'DD/MM/YYYY';
        input.classList.add('date-picker-input');
        
        // Initialize Flatpickr
        flatpickr(input, {
            dateFormat: 'd/m/Y',
            altInput: true,
            altFormat: 'd/m/Y',
            allowInput: true,
            parseDate: function(datestr, format) {
                // Parse DD/MM/YYYY format
                if (datestr.match(/^\d{2}\/\d{2}\/\d{4}$/)) {
                    const parts = datestr.split('/');
                    return new Date(parts[2], parts[1] - 1, parts[0]);
                }
                // Parse YYYY-MM-DD format (from server)
                if (datestr.match(/^\d{4}-\d{2}-\d{2}$/)) {
                    return new Date(datestr);
                }
                return new Date(datestr);
            },
            onChange: function(selectedDates, dateStr, instance) {
                // Ensure the hidden input has the correct format for server
                if (selectedDates.length > 0) {
                    const date = selectedDates[0];
                    const year = date.getFullYear();
                    const month = String(date.getMonth() + 1).padStart(2, '0');
                    const day = String(date.getDate()).padStart(2, '0');
                    
                    // Store in DD/MM/YYYY format for display
                    input.value = `${day}/${month}/${year}`;
                    
                    // Set a hidden input with YYYY-MM-DD for server processing if needed
                    let hiddenInput = input.parentElement.querySelector('.date-hidden');
                    if (!hiddenInput && input.name) {
                        hiddenInput = document.createElement('input');
                        hiddenInput.type = 'hidden';
                        hiddenInput.name = input.name + '_ISO';
                        hiddenInput.className = 'date-hidden';
                        input.parentElement.appendChild(hiddenInput);
                    }
                    if (hiddenInput) {
                        hiddenInput.value = `${year}-${month}-${day}`;
                    }
                }
            }
        });
    });
});

// Helper function to format dates for display
function formatDateForDisplay(dateString) {
    if (!dateString) return '';
    
    // If already in DD/MM/YYYY format
    if (dateString.match(/^\d{2}\/\d{2}\/\d{4}$/)) {
        return dateString;
    }
    
    // If in YYYY-MM-DD format
    if (dateString.match(/^\d{4}-\d{2}-\d{2}/)) {
        const date = new Date(dateString);
        const day = String(date.getDate()).padStart(2, '0');
        const month = String(date.getMonth() + 1).padStart(2, '0');
        const year = date.getFullYear();
        return `${day}/${month}/${year}`;
    }
    
    return dateString;
}

// Helper function to convert DD/MM/YYYY to YYYY-MM-DD for server
function convertToISODate(ddmmyyyy) {
    if (!ddmmyyyy) return '';
    
    if (ddmmyyyy.match(/^\d{2}\/\d{2}\/\d{4}$/)) {
        const parts = ddmmyyyy.split('/');
        return `${parts[2]}-${parts[1]}-${parts[0]}`;
    }
    
    return ddmmyyyy;
}

