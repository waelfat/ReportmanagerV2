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
});
