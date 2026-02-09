let extractedParameters = [];

// Toast notification functions
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

// Extract parameters from SQL query
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

// Display parameters for configuration
function displayParameters(parameters) {
    const parametersList = document.getElementById('parametersList');
    const parametersSection = document.getElementById('parametersSection');
    
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
                            
                            <div class="mb-2">
                                <label class="form-label fw-semibold">Description</label>
                                <input type="text" class="form-control param-description" data-index="${index}" placeholder="Parameter description">
                            </div>
                            
                            <div class="mb-2">
                                <label class="form-label fw-semibold">Type <span class="text-danger">*</span></label>
                                <select class="form-control param-type" data-index="${index}" required>
                                    <option value="Varchar2">Varchar2</option>
                                    <option value="Decimal">Number</option>
                                    <option value="Date">Date</option>
                                    <option value="TimeStamp">TimeStamp</option>
                                    <option value="Clob">Clob</option>
                                </select>
                            </div>
                            
                            <div class="mb-2">
                                <label class="form-label fw-semibold">View Control <span class="text-danger">*</span></label>
                                <select class="form-control param-viewcontrol" data-index="${index}" required>
                                    <option value="TextBox">TextBox</option>
                                    <option value="Select">Select</option>
                                    <option value="CheckBox">CheckBox</option>
                                    <option value="Date">Date</option>
                                </select>
                            </div>
                            
                            <div class="mb-2">
                                <label class="form-label fw-semibold">Default Value</label>
                                <input type="text" class="form-control param-default" data-index="${index}" placeholder="Default value">
                            </div>
                            
                            <div class="form-check">
                                <input class="form-check-input param-required" type="checkbox" data-index="${index}" checked>
                                <label class="form-check-label">Required</label>
                            </div>
                            
                            <input type="hidden" class="param-position" data-index="${index}" value="${param.position}">
                        </div>
                    </div>
                </div>
            `;
        });
        html += '</div>';
        parametersList.innerHTML = html;
    }
    
    parametersSection.style.display = 'block';
}

// Load parameters button click
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

// Form submission
document.getElementById('createReportForm').addEventListener('submit', async function(e) {
    e.preventDefault();
    
    // Validate that parameters are loaded
    if (extractedParameters.length > 0 && document.getElementById('parametersSection').style.display === 'none') {
        showErrorToast('Please load parameters first');
        return;
    }
    
    // Collect parameter configurations
    const parameters = [];
    extractedParameters.forEach((param, index) => {
        const type = document.querySelector(`.param-type[data-index="${index}"]`).value;
        const viewControl = document.querySelector(`.param-viewcontrol[data-index="${index}"]`).value;
        const description = document.querySelector(`.param-description[data-index="${index}"]`).value;
        const defaultValue = document.querySelector(`.param-default[data-index="${index}"]`).value;
        const isRequired = document.querySelector(`.param-required[data-index="${index}"]`).checked;
        const position = parseInt(document.querySelector(`.param-position[data-index="${index}"]`).value);
        
        parameters.push({
            Name: param.name,
            Description: description || null,
            Type: type,
            ViewControl: viewControl,
            DefaultValue: defaultValue || null,
            IsRequired: isRequired,
            Position: position
        });
    });
    
    const formData = {
        Name: document.getElementById('Name').value,
        Description: document.getElementById('Description').value,
        ReportQuery: document.getElementById('ReportQuery').value,
        SchemaId: document.getElementById('SchemaId').value,
        IsActive: document.getElementById('IsActive').checked,
        CategoryId: document.getElementById('CategoryId').value,
        Parameters: parameters
    };
    
    try {
        const response = await fetch('/Report/Create', {
            method: 'POST',
            headers: {
                'Content-Type': 'application/json',
            },
            body: JSON.stringify(formData)
        });
        
        if (response.ok) {
            showSuccessToast('Report created successfully');
            setTimeout(() => {
                window.location.href = '/Home/Index';
            }, 1500);
        } else {
            const error = await response.text();
            showErrorToast('Error: ' + error);
        }
    } catch (error) {
        showErrorToast('Error: ' + error.message);
    }
});
