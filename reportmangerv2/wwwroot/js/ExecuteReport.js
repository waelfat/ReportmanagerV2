$(function () {
    // Delegated handler for dynamically generated sidebar links
    $(document).on('click', '.load-report', function (e) {
        e.preventDefault();
        var $link = $(this);

        var id = $link.data('report-id');
        if (!id) return;

        // UI feedback: set active state and show spinner in container
        $('.load-report.active').removeClass('active');
        $link.addClass('active');
        $('#ReportContainer').html('<div class="text-center py-4"><div class="spinner-border" role="status" aria-hidden="true"></div></div>');

        $.ajax({
            url: 'home/ExecuteReport',
            type: 'GET',
            data: { id: id },
            success: function (result) {
                $('#ReportContainer').html(result);
                if (typeof IntializePartial === 'function') {
                    IntializePartial(id);
                } else {
                    $.getScript('/js/executerepotpartial.js')
                        .done(function () {
                            if (typeof IntializePartial === 'function') IntializePartial(id);
                        })
                        .fail(function () {
                            console.error('Failed to load executerepotpartial.js');
                        });
                }
            },
            error: function () {
                $('#ReportContainer').html('<div class="alert alert-danger">Failed to load report.</div>');
            }
        });
    });
    
    // Schedule datetime change handler
    $(document).on('change', '[id^="scheduleDateTime-"]', function() {
        const reportId = $(this).attr('id').replace('scheduleDateTime-', '');
        const dateTime = new Date($(this).val());
        
        if (!isNaN(dateTime.getTime())) {
            const minute = dateTime.getMinutes();
            const hour = dateTime.getHours();
            const day = dateTime.getDate();
            const month = dateTime.getMonth() + 1;
            const cronExpr = `${minute} ${hour} ${day} ${month} *`;
            
            $(`#cronExpression-${reportId}`).val(cronExpr);
            
            try {
                const humanReadable = cronstrue.toString(cronExpr);
                $(`#cronDescription-${reportId}`).text(humanReadable);
            } catch (e) {
                $(`#cronDescription-${reportId}`).text('Invalid cron expression');
            }
        }
    });
    
    // Schedule button handler
    $(document).on('click', '[id^="scheduleBtn-"]', function() {
        const reportId = $(this).attr('id').replace('scheduleBtn-', '');
        const cronExpression = $(`#cronExpression-${reportId}`).val();
        const selectedDate = $(`#scheduleDateTime-${reportId}`).val();
        console.log(selectedDate);
        //check if user enter date and time in scheduleDateTime-
        if(!selectedDate)
        {
            alert('Please select a date and time');
            return;
        }


        const $btn = $(this);
        
        if (!cronExpression) {
            alert('Please select a date and time first');
            return;
        }
        
        // Collect parameters from the form
        const parameters = [];
        $(`#executeReportForm-${reportId} input[name^="Parameters"]`).each(function() {
            const name = $(this).attr('name');
            if (name.includes('.Name')) {
                const index = name.match(/\[(\d+)\]/)[1];
                const paramName = $(this).val();
                const paramValue = $(`input[name="Parameters[${index}].Value"], select[name="Parameters[${index}].Value"]`).val();
                parameters.push({ Name: paramName, Value: paramValue || '' });
            }
        });
        
        $btn.prop('disabled', true).html('<i class="fas fa-spinner fa-spin me-2"></i>Scheduling...');
        
        $.ajax({
            url: '/Home/ScheduleReport',
            method: 'POST',
            contentType: 'application/json',
            data: JSON.stringify({
                reportId: reportId,
                cronExpression: cronExpression,
                selectedDate:selectedDate,
                parameters: parameters
            }),
            success: function(data) {
                alert('Report scheduled successfully!');
                $(`#scheduleSection-${reportId}`).collapse('hide');
                $btn.prop('disabled', false).html('<i class="fas fa-calendar-check me-2"></i>Schedule Report');
                debugger;
                addExecutionToTable(data.executionId,true)
            },
            error: function(xhr) {
                const errorMsg = xhr.responseJSON?.error || 'Failed to schedule report';
                alert(errorMsg);
                $btn.prop('disabled', false).html('<i class="fas fa-calendar-check me-2"></i>Schedule Report');
            }
        });
    });
    
    // Delete execution handler
    $(document).on('click', '.delete-execution', function() {
        var executionId = $(this).data('execution-id');
        var $btn = $(this);
        
        if (confirm('Are you sure you want to delete this execution? This action cannot be undone.')) {
            $btn.prop('disabled', true).html('<i class="fas fa-spinner fa-spin me-1"></i>Deleting...');
            
            $.ajax({
                url: '/Home/DeleteExecution',
                method: 'POST',
                data: { executionId: executionId },
                success: function() {
                    $(`tr[data-execution-id="${executionId}"]`).fadeOut(function() {
                        $(this).remove();
                    });
                },
                error: function() {
                    alert('Failed to delete execution');
                    $btn.prop('disabled', false).html('<i class="fas fa-trash me-1"></i>Delete');
                }
            });
        }
    });
});
