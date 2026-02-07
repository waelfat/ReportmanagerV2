let procedureParameters = [];

// Email validation function
function validateEmails(emailString) {
    if (!emailString || emailString.trim() === '') {
        return { valid: true, emails: [] };
    }
    
    const emailRegex = /^[^\s@]+@[^\s@]+\.[^\s@]+$/;
    const emails = emailString.split(',').map(e => e.trim()).filter(e => e !== '');
    const invalidEmails = emails.filter(email => !emailRegex.test(email));
    
    return {
        valid: invalidEmails.length === 0,
        invalidEmails: invalidEmails,
        emails: emails
    };
}

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

// Load procedure parameters
document.getElementById('loadProcedureBtn').addEventListener('click', async function() {
    const schemaId = document.getElementById('SchemaId').value.trim();
    const procedureName = document.getElementById('ProcedureName').value.trim();
    
    if (!schemaId) {
        showErrorToast('Please select a schema first');
        return;
    }
    
    if (!procedureName) {
        showErrorToast('Please enter a procedure name');
        return;
    }
    
    try {
        const response = await fetch('/ScheduledJob/GetProcedureParameters', {
            method: 'POST',
            headers: {
                'Content-Type': 'application/json',
            },
            body: JSON.stringify({ 
                procedureName: procedureName,
                schemaId: schemaId 
            })
        });
        
        if (response.ok) {
            const parameters = await response.json();
            procedureParameters = parameters;
            displayParameters(parameters);
            showSuccessToast('Parameters loaded successfully');
        } else {
            const error = await response.text();
            showErrorToast('Error loading parameters: ' + error);
        }
    } catch (error) {
        showErrorToast('Error: ' + error.message);
    }
});

// Display parameters
function displayParameters(parameters) {
    const parametersList = document.getElementById('parametersList');
    const parametersSection = document.getElementById('parametersSection');
    
    if (parameters.length === 0) {
        parametersList.innerHTML = '<div class="alert alert-info"><i class="fas fa-info-circle me-2"></i>No parameters found for this procedure.</div>';
    } else {
        let html = '<div class="row g-3">';
        parameters.forEach((param, index) => {
            if (param.direction === 'Input') {
                html += `
                    <div class="col-md-6">
                        <div class="card border-primary border-opacity-25">
                            <div class="card-body p-3">
                                <label class="form-label fw-semibold text-primary">
                                    <i class="fas fa-arrow-right me-1"></i>${param.name}
                                </label>
                                <input type="text" class="form-control parameter-input" 
                                       data-index="${index}" 
                                       placeholder="Enter value for ${param.name}">
                                <small class="text-muted">${param.type} - ${param.direction}</small>
                            </div>
                        </div>
                    </div>
                `;
            } else {
                html += `
                    <div class="col-md-6">
                        <div class="card border-secondary border-opacity-25">
                            <div class="card-body p-3">
                                <label class="form-label fw-semibold text-secondary">
                                    <i class="fas fa-arrow-left me-1"></i>${param.name}
                                </label>
                                <div class="form-control-plaintext text-muted bg-light rounded p-2">
                                    <i class="fas fa-info-circle me-1"></i>Output parameter - no input required
                                </div>
                                <small class="text-muted">${param.type} - ${param.direction}</small>
                            </div>
                        </div>
                    </div>
                `;
            }
        });
        html += '</div>';
        parametersList.innerHTML = html;
    }
    
    parametersSection.style.display = 'block';
}

// Function to build cron expression from individual parts
function buildCronExpression() {
    const minute = document.getElementById('cronMinute').value || '*';
    const hour = document.getElementById('cronHour').value || '*';
    const day = document.getElementById('cronDay').value || '*';
    const month = document.getElementById('cronMonth').value || '*';
    const dayOfWeek = document.getElementById('cronDayOfWeek').value || '*';
    
    return `${minute} ${hour} ${day} ${month} ${dayOfWeek}`;
}

// Function to update cron expression and description
function updateCronExpression() {
    const cronExpression = buildCronExpression();
    document.getElementById('CronExpression').value = cronExpression;
    
    const cronDescription = document.getElementById('cronDescription');
    const cronText = document.getElementById('cronText');
    
    try {
        const humanReadable = cronstrue.toString(cronExpression, { verbose: true });
        cronText.textContent = humanReadable;
        cronDescription.style.display = 'block';
    } catch (e) {
        cronText.textContent = 'Invalid cron expression';
        cronDescription.style.display = 'block';
    }
}

// Add event listeners to all cron part inputs
document.querySelectorAll('.cron-part').forEach(input => {
    input.addEventListener('input', updateCronExpression);
});

// Initialize on page load
updateCronExpression();

document.getElementById('createJobForm').addEventListener('submit', async function(e) {
    e.preventDefault();
    
    // Validate emails
    const sendToEmails = document.getElementById('SendToEmails').value;
    const ccMails = document.getElementById('CCMails').value;
    
    const sendToValidation = validateEmails(sendToEmails);
    if (!sendToValidation.valid) {
        showErrorToast('Invalid email(s) in Send To: ' + sendToValidation.invalidEmails.join(', '));
        document.getElementById('SendToEmails').focus();
        return;
    }
    
    const ccValidation = validateEmails(ccMails);
    if (!ccValidation.valid) {
        showErrorToast('Invalid email(s) in CC: ' + ccValidation.invalidEmails.join(', '));
        document.getElementById('CCMails').focus();
        return;
    }
    
    // Collect parameter values
    // ensure paratemers are exists 
    if(procedureParameters == null ||    procedureParameters.length == 0 ){
        // showErrorToast('Please load procedure parameters first');
        showErrorToast('Please load procedure parameters first');
        document.getElementById('loadProcedureBtn').focus();
        document.getElementById('loadProcedureBtn').scrollIntoView({ behavior: 'smooth' });
        
        return;
    }

    const parameters = [];
    procedureParameters.forEach((param, index) => {
        if (param.direction === 'Input') {
            const input = document.querySelector(`[data-index="${index}"]`);
            parameters.push({
                Name: param.name,
                Value: input ? input.value : null,
                Type: param.type,
                Direction: param.direction
            });
        } else {
            // Include output parameters without values
            parameters.push({
                Name: param.name,
                Value: null,
                Type: param.type,
                Direction: param.direction
            });
        }
    });
    
    const formData = {
        Description: document.getElementById('Description').value,
        SchemaId: document.getElementById('SchemaId').value,
        ProcedureName: document.getElementById('ProcedureName').value,
        CronExpression: document.getElementById('CronExpression').value,
        IsActive: document.getElementById('IsActive').checked,
        MessageSubject: document.getElementById('MessageSubject').value,
        MessageBody: document.getElementById('MessageBody').value,
        SendToEmails: document.getElementById('SendToEmails').value,
        CCMails: document.getElementById('CCMails').value,
        Parameters: parameters
    };
    
    try {
        const response = await fetch('/ScheduledJob/Create', {
            method: 'POST',
            headers: {
                'Content-Type': 'application/json',
            },
            body: JSON.stringify(formData)
        });
        
        if (response.ok) {
            showSuccessToast('Job created successfully');
            setTimeout(() => {
                window.location.href = '/ScheduledJob';
            }, 1500);
        } else {
            const error = await response.text();
            showErrorToast('Error: ' + error);
        }
    } catch (error) {
        showErrorToast('Error: ' + error.message);
    }
});
