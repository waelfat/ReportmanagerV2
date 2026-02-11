let extractedParameters = [];

function showErrorToast(message) {
    document.getElementById('errorMessage').textContent = message;
    const toast = new bootstrap.Toast(document.getElementById('errorToast'));
    toast.show();
}

function showSuccessToast(message) {
    document.getElementById('successMessage').textContent = message;
    const toast = new bootstrap.Toast(document.getElementById('successToast'));
    toast.show();
}

function extractParametersFromQuery(query) {
    const paramRegex = /:(\w+)/g;
    const params = [];
    const uniqueParams = new Set();
    
    let match;
    while ((match = paramRegex.exec(query)) !== null) {
        const paramName = match[1];
        if (!uniqueParams.has(paramName)) {
            uniqueParams.add(paramName);
            params.push({
                name: paramName,
                position: params.length + 1
            });
        }
    }
    
    return params;
}

function displayParameters(parameters) {
    const parametersList = document.getElementById('parametersList');
    
    if (parameters.length === 0) {
        parametersList.innerHTML = '<div class="alert alert-info"><i class="fas fa-info-circle me-2"></i>No parameters found in the SQL query.</div>';
    } else {
        let html = '<div class="row g-3">';
        parameters.forEach((param, index) => {
            html += `
                <div class="col-md-6">
                    <div class="card border-primary border-opacity-25">
                        <div class="card-body">
                            <h6 class="text-primary"><i class="fas fa-tag me-2"></i>${param.name}</h6>
                            <input type="hidden" class="param-name" value="${param.name}">
                            
                            <div class="mb-2">
                                <label class="form-label fw-semibold">Description</label>
                                <input type="text" class="form-control param-description" placeholder="Parameter description">
                            </div>
                            
                            <div class="mb-2">
                                <label class="form-label fw-semibold">Type <span class="text-danger">*</span></label>
                                <select class="form-control param-type" required>
                                    <option value="Varchar2">Varchar2</option>
                                    <option value="Decimal">Number</option>
                                    <option value="Date">Date</option>
                                    <option value="TimeStamp">TimeStamp</option>
                                    <option value="Clob">Clob</option>
                                </select>
                            </div>
                            
                            <div class="mb-2">
                                <label class="form-label fw-semibold">View Control <span class="text-danger">*</span></label>
                                <select class="form-control param-viewcontrol" required>
                                    <option value="TextBox">TextBox</option>
                                    <option value="Select">Select</option>
                                    <option value="CheckBox">CheckBox</option>
                                    <option value="Date">Date</option>
                                </select>
                            </div>
                            
                            <div class="mb-2">
                                <label class="form-label fw-semibold">Default Value</label>
                                <input type="text" class="form-control param-default" placeholder="Default value">
                            </div>
                            
                            <div class="form-check">
                                <input class="form-check-input param-required" type="checkbox" checked>
                                <label class="form-check-label">Required</label>
                            </div>
                            
                            <input type="hidden" class="param-position" value="${param.position}">
                        </div>
                    </div>
                </div>
            `;
        });
        html += '</div>';
        parametersList.innerHTML = html;
    }
}

document.getElementById('loadParametersBtn').addEventListener('click', function() {
    const query = document.getElementById('ReportQuery').value.trim();
    
    if (!query) {
        showErrorToast('Please enter a SQL query first');
        return;
    }
    
    extractedParameters = extractParametersFromQuery(query);
    
    if (extractedParameters.length === 0) {
        showErrorToast('No parameters found in the query. Use :parameterName syntax.');
        return;
    }
    
    displayParameters(extractedParameters);
    showSuccessToast(`Found ${extractedParameters.length} parameter(s)`);
});

document.getElementById('editReportForm').addEventListener('submit', async function(e) {
    e.preventDefault();
    
    const parameters = [];
    const paramCards = document.querySelectorAll('#parametersList .card');
    
    paramCards.forEach((card, index) => {
        const id = card.querySelector('.param-id')?.value || null;
        const name = card.querySelector('.param-name').value;
        const description = card.querySelector('.param-description').value;
        const type = card.querySelector('.param-type').value;
        const viewControl = card.querySelector('.param-viewcontrol').value;
        const defaultValue = card.querySelector('.param-default').value;
        const isRequired = card.querySelector('.param-required').checked;
        const position = parseInt(card.querySelector('.param-position').value);
        
        parameters.push({
            Id: id,
            Name: name,
            Description: description || null,
            Type: type,
            ViewControl: viewControl,
            DefaultValue: defaultValue || null,
            IsRequired: isRequired,
            Position: position
        });
    });
    
    const formData = {
        Id: document.getElementById('ReportId').value,
        Name: document.getElementById('Name').value,
        Description: document.getElementById('Description').value,
        ReportQuery: document.getElementById('ReportQuery').value,
        SchemaId: document.getElementById('SchemaId').value,
        CategoryId: document.getElementById('CategoryId').value,
        IsActive: document.getElementById('IsActive').checked,
        Parameters: parameters
    };
    
    try {
        const response = await fetch('/Report/Edit', {
            method: 'POST',
            headers: {
                'Content-Type': 'application/json',
            },
            body: JSON.stringify(formData)
        });
        
        if (response.ok) {
            showSuccessToast('Report updated successfully');
            setTimeout(() => {
                window.location.href = '/Report/Index';
            }, 1500);
        } else {
            const error = await response.text();
            showErrorToast('Error: ' + error);
        }
    } catch (error) {
        showErrorToast('Error: ' + error.message);
    }
});
