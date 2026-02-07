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
            },
            error: function () {
                $('#ReportContainer').html('<div class="alert alert-danger">Failed to load report.</div>');
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
