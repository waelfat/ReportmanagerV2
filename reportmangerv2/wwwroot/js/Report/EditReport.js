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

function updateDependsOnOptions() {
    const allParams = document.querySelectorAll('.param-name');
    document.querySelectorAll('.param-depends-on').forEach(select => {
        const currentParam = select.closest('.card').querySelector('.param-name').value;
        const savedValue = select.dataset.savedValue || '';
        select.innerHTML = '<option value="">None</option>';
        allParams.forEach(p => {
            if (p.value !== currentParam) {
                const selected = p.value === savedValue ? 'selected' : '';
                select.innerHTML += `<option value="${p.value}" ${selected}>${p.value}</option>`;
            }
        });
    });
}

document.addEventListener('DOMContentLoaded', function() {
    document.querySelectorAll('.param-viewcontrol').forEach(select => {
        const card = select.closest('.card');
        
        if (select.value === 'Select') {
            card.querySelector('.param-datasource').style.display = 'block';
            card.querySelector('.param-depends').style.display = 'block';
        }
        
        select.addEventListener('change', function() {
            if (this.value === 'Select') {
                card.querySelector('.param-datasource').style.display = 'block';
                card.querySelector('.param-depends').style.display = 'block';
                updateDependsOnOptions();
            } else {
                card.querySelector('.param-datasource').style.display = 'none';
                card.querySelector('.param-depends').style.display = 'none';
            }
        });
    });
    
    updateDependsOnOptions();
});

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
            const existing = param.existing || {};
            html += `
                <div class="col-md-6">
                    <div class="card border-primary border-opacity-25">
                        <div class="card-body">
                            <h6 class="text-primary"><i class="fas fa-tag me-2"></i>${param.name}</h6>
                            <input type="hidden" class="param-id" value="${existing.id || ''}">
                            <input type="hidden" class="param-name" value="${param.name}">
                            
                            <div class="mb-2">
                                <label class="form-label fw-semibold">Description</label>
                                <input type="text" class="form-control param-description" value="${existing.description || ''}" placeholder="Parameter description">
                            </div>
                            
                            <div class="mb-2">
                                <label class="form-label fw-semibold">Type <span class="text-danger">*</span></label>
                                <select class="form-control param-type" required>
                                    <option value="Varchar2" ${existing.type === 'Varchar2' ? 'selected' : ''}>Varchar2</option>
                                    <option value="Decimal" ${existing.type === 'Decimal' ? 'selected' : ''}>Number</option>
                                    <option value="Date" ${existing.type === 'Date' ? 'selected' : ''}>Date</option>
                                    <option value="TimeStamp" ${existing.type === 'TimeStamp' ? 'selected' : ''}>TimeStamp</option>
                                    <option value="Clob" ${existing.type === 'Clob' ? 'selected' : ''}>Clob</option>
                                </select>
                            </div>
                            
                            <div class="mb-2">
                                <label class="form-label fw-semibold">View Control <span class="text-danger">*</span></label>
                                <select class="form-control param-viewcontrol" required>
                                    <option value="TextBox" ${existing.viewControl === 'TextBox' ? 'selected' : ''}>TextBox</option>
                                    <option value="Select" ${existing.viewControl === 'Select' ? 'selected' : ''}>Select</option>
                                    <option value="CheckBox" ${existing.viewControl === 'CheckBox' ? 'selected' : ''}>CheckBox</option>
                                    <option value="Date" ${existing.viewControl === 'Date' ? 'selected' : ''}>Date</option>
                                </select>
                            </div>
                            
                            <div class="mb-2">
                                <label class="form-label fw-semibold">Default Value</label>
                                <input type="text" class="form-control param-default" value="${existing.defaultValue || ''}" placeholder="Default value">
                            </div>
                            
                            <div class="mb-2">
                                <label class="form-label fw-semibold">Position <span class="text-danger">*</span></label>
                                <input type="number" class="form-control param-position" value="${param.position}" min="1" required>
                            </div>
                            
                            <div class="mb-2 param-datasource" style="display:${existing.viewControl === 'Select' ? 'block' : 'none'};">
                                <label class="form-label fw-semibold">Data Source Query</label>
                                <textarea class="form-control param-datasource-query" rows="2" placeholder="SELECT id, name, parent_id FROM table">${existing.dependencyQuery || ''}</textarea>
                            </div>
                            
                            <div class="mb-2 param-depends" style="display:${existing.viewControl === 'Select' ? 'block' : 'none'};">
                                <label class="form-label fw-semibold">Depends On</label>
                                <select class="form-control param-depends-on" data-saved-value="${existing.dependsOn || ''}">
                                    <option value="">None</option>
                                </select>
                            </div>
                            
                            <div class="form-check">
                                <input class="form-check-input param-required" type="checkbox" ${existing.isRequired !== false ? 'checked' : ''}>
                                <label class="form-check-label">Required</label>
                            </div>
                        </div>
                    </div>
                </div>
            `;
        });
        html += '</div>';
        parametersList.innerHTML = html;
        
        document.querySelectorAll('.param-viewcontrol').forEach(select => {
            const card = select.closest('.card');
            select.addEventListener('change', function() {
                if (this.value === 'Select') {
                    card.querySelector('.param-datasource').style.display = 'block';
                    card.querySelector('.param-depends').style.display = 'block';
                    updateDependsOnOptions();
                } else {
                    card.querySelector('.param-datasource').style.display = 'none';
                    card.querySelector('.param-depends').style.display = 'none';
                }
            });
        });
        
        updateDependsOnOptions();
    }
}

document.getElementById('loadParametersBtn').addEventListener('click', function() {
    const query = document.getElementById('ReportQuery').value.trim();
    
    if (!query) {
        showErrorToast('Please enter a SQL query first');
        return;
    }
    
    const existingParams = {};
    document.querySelectorAll('#parametersList .card').forEach(card => {
        const name = card.querySelector('.param-name')?.value;
        if (name) {
            existingParams[name] = {
                id: card.querySelector('.param-id')?.value || null,
                description: card.querySelector('.param-description')?.value || '',
                type: card.querySelector('.param-type')?.value || 'Varchar2',
                viewControl: card.querySelector('.param-viewcontrol')?.value || 'TextBox',
                defaultValue: card.querySelector('.param-default')?.value || '',
                isRequired: card.querySelector('.param-required')?.checked ?? true,
                position: parseInt(card.querySelector('.param-position')?.value) || 0,
                dependsOn: card.querySelector('.param-depends-on')?.value || '',
                dependencyQuery: card.querySelector('.param-datasource-query')?.value || ''
            };
        }
    });
    
    extractedParameters = extractParametersFromQuery(query);
    
    if (extractedParameters.length === 0) {
        showErrorToast('No parameters found in the query. Use :parameterName syntax.');
        return;
    }
    
    extractedParameters.forEach((param, index) => {
        if (existingParams[param.name]) {
            param.existing = existingParams[param.name];
        }
        param.position = index + 1;
    });
    
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
        const dependsOn = card.querySelector('.param-depends-on')?.value || null;
        const dependencyQuery = card.querySelector('.param-datasource-query')?.value || null;
        
        parameters.push({
            Id: id,
            Name: name,
            Description: description || null,
            Type: type,
            ViewControl: viewControl,
            DefaultValue: defaultValue || null,
            IsRequired: isRequired,
            Position: position,
            DependsOn: dependsOn || null,
            DependencyQuery: dependencyQuery || null
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
