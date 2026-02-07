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

// Initialize function to be called on page load
function initializeEditJob(cronExpression, existingParameters) {
    // Initialize cron expression from model
    const cronParts = cronExpression.split(' ');
    if (cronParts.length === 5) {
        document.getElementById('cronMinute').value = cronParts[0];
        document.getElementById('cronHour').value = cronParts[1];
        document.getElementById('cronDay').value = cronParts[2];
        document.getElementById('cronMonth').value = cronParts[3];
        document.getElementById('cronDayOfWeek').value = cronParts[4];
    }
    
    // Load existing parameters
    procedureParameters = existingParameters;
    
    // Initialize cron expression display
    updateCronExpression();
}

// Load procedure parameters
document.getElementById('loadProcedureBtn').addEventListener('click', async function() {
    const procedureName = document.getElementById('ProcedureName').value.trim();
    const schemaId = document.getElementById('SchemaId').value.trim();
    
    if (!schemaId) {
        showErrorToast('Please choose a schema name');
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
            showSuccessToast('Parameters reloaded successfully');
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
    
    if (parameters.length === 0) {
        parametersList.innerHTML = '<div class="alert alert-info"><i class="fas fa-info-circle me-2"></i>No parameters found for this procedure.</div>';
    } else {
        let html = '<div class="row g-3">';
        parameters.forEach((param) => {
            if (param.direction === 'Input') {
                html += `
                    <div class="col-md-6">
                        <div class="card border-primary border-opacity-25">
                            <div class="card-body p-3">
                                <label class="form-label fw-semibold text-primary">
                                    <i class="fas fa-arrow-right me-1"></i>${param.name}
                                </label>
                                <input type="text" class="form-control parameter-input" 
                                       data-name="${param.name}" 
                                       data-type="${param.type}"
                                       data-direction="${param.direction}"
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
                                <input type="hidden" class="parameter-output" 
                                       data-name="${param.name}" 
                                       data-type="${param.type}"
                                       data-direction="${param.direction}">
                            </div>
                        </div>
                    </div>
                `;
            }
        });
        html += '</div>';
        parametersList.innerHTML = html;
    }
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

// Form submission
document.getElementById('editJobForm').addEventListener('submit', async function(e) {
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
    const parameters = [];
    
    try {
        // Input parameters
        const parInputs = document.querySelectorAll('.parameter-input');
        parInputs.forEach(input => {
            if (!input.value || !input.value.trim()) {
                throw new Error("Value is required for Input parameters");
            }
            debugger;
            // ensure the value is valid for the parameter type
            var paramType=input.getAttribute('data-type').toLowerCase();
            if(input.getAttribute('data-type').toLowerCase().includes('date') && isNaN(Date.parse(input.value))){
                input.focus();

                throw new Error("Invalid date format for parameter " + input.getAttribute('data-name'));
                

            }
            //check for varchar2
            if(input.getAttribute('data-type').toLowerCase().includes('varchar') && input.value.length>200){
                input.focus();
                throw new Error("Value too long for parameter " + input.getAttribute('data-name'));
            }
            //chech for Number
            if(input.getAttribute('data-type').toLowerCase().includes('number') && isNaN(input.value)){
                input.focus();
                throw new Error("Invalid number for parameter " + input.getAttribute('data-name'));
            }
            //check for decimal
            if(input.getAttribute('data-type').toLowerCase().includes('decimal') && isNaN(input.value)){
                input.focus();
                throw new Error("Invalid decimal for parameter " + input.getAttribute('data-name'));
            }

            parameters.push({
                Name: input.getAttribute('data-name'),
                Value: input.value,
                Type: input.getAttribute('data-type'),
                Direction: input.getAttribute('data-direction')
            });
        });
    } catch (error) {
        showErrorToast(error.message);
        return;
    }
    
    // Output parameters
    document.querySelectorAll('.parameter-output').forEach(input => {
        parameters.push({
            Name: input.getAttribute('data-name'),
            Value: null,
            Type: input.getAttribute('data-type'),
            Direction: input.getAttribute('data-direction')
        });
    });
    
    const formData = {
        Id: document.getElementById('JobId').value,
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
        const response = await fetch('/ScheduledJob/Edit', {
            method: 'POST',
            headers: {
                'Content-Type': 'application/json',
            },
            body: JSON.stringify(formData)
        });
        
        if (response.ok) {
            showSuccessToast('Job updated successfully');
            setTimeout(() => {
                window.location.href = '/ScheduledJob/';
            }, 1500);
        } else {
            const error = await response.text();
            showErrorToast('Error: ' + error);
        }
    } catch (error) {
        showErrorToast('Error: ' + error.message);
    }
});
