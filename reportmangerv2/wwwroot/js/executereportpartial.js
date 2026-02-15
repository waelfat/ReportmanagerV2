//this is code of razor view we will split in its own js file

if (typeof signalR === "undefined") {
  console.error("SignalR not loaded");

}else{


var connection = new signalR.HubConnectionBuilder()
  .withUrl("/executionHub")
  .build();

connection
  .start()
  .then(function () {
    console.log("SignalR connected");
  })
  .catch(function (err) {
    console.error("SignalR connection failed: ", err);
  });
  connection.on("ExecutionStarted",
  function (executionId,reportName) {
    console.log(`Execution started: ${executionId}`);
    updateExecutionRow(executionId, "Running", 0, false);
    showNotification("Started", reportName);
    // You can add logic here to update the UI when an execution starts
  },)

connection.on(
  "ExecutionCompleted",
  function (executionId, status, duration, hasResult) {
    updateExecutionRow(executionId, status, duration, hasResult);
    showNotification(status, executionId);
  },
);



}

function IntializePartial(reportId) {
  var form = $(`#executeReportForm-${reportId}`);
  form.on("submit", function (e) {

    e.preventDefault();

    var $btn = $(`#executeBtn-${reportId}`);
    var $status = $(`#executeStatus-${reportId}`);
    var $result = $(`#executeResult-${reportId}`);
    var isValid = true;

    const req = form.find("[data-IsRequired='true']");
    req.each(function () {
      if (!$(this).val()) {
        $(this).addClass("is-invalid");
        isValid = false;
      } else {
        $(this).removeClass("is-invalid");
      }
    });
    if (!isValid) {
      $status.html(
        '<div class="text-danger"><i class="fas fa-exclamation-circle me-1"></i>Please fill all required parameters</div>',
      );
      return;
    }

    $status.html(
      '<div class="d-flex align-items-center text-primary"><div class="spinner-border spinner-border-sm me-2" role="status"></div>Executing...</div>',
    );
    $btn
      .prop("disabled", true)
      .html('<i class="fas fa-spinner fa-spin me-2"></i>Executing...');

    $.ajax({
      url: form.attr("action"),
      method: "POST",
      data: form.serialize(),
      success: function (resp) {
        $status.html(
          '<div class="text-success"><i class="fas fa-check-circle me-1"></i>Started Successfully</div>',
        );
        $result
          .show()
          .find(".alert")
          .attr("class", "alert alert-success")
          .html(
            '<i class="fas fa-info-circle me-2"></i><strong>Execution Started!</strong><br>Execution ID: ' +
              (resp.executionId || "n/a"),
          );

        addExecutionToTable(resp.executionId,false);
      },
      error: function (xhr) {
        var msg = "Failed to start execution";
        try {
          msg = xhr.responseJSON?.error || xhr.responseText || msg;
        } catch (e) {}
        $status.html(
          '<div class="text-danger"><i class="fas fa-exclamation-circle me-1"></i>Failed</div>',
        );
        $result
          .show()
          .find(".alert")
          .attr("class", "alert alert-danger")
          .html(
            '<i class="fas fa-exclamation-triangle me-2"></i><strong>Execution Failed!</strong><br>' +
              msg,
          );
      },
      complete: function () {
        $btn
          .prop("disabled", false)
          .html('<i class="fas fa-play me-2"></i>Execute Report');
      },
    });
  });

  $(document).on("click", "#btnConfirmCancel", function () {
    //var confirmcancelid = $(this).data('execution-id');
    console.log(confirmcancelid);
    $.ajax({ 
      url: "/Home/CancelExecution",
      method: "POST",
      data: { executionId: confirmcancelid },
      success: function () {
        showNotification("Cancelled", confirmcancelid);
        // Update the row appearance and remove cancel button for all types
        updateExecutionRow(confirmcancelid, "Cancelled", 0, false);
      },
      error: function () {
        alert("Failed to cancel execution ");
      },
    });
  });
  let confirmcancelid = null;
  var confirmModal = document.getElementById("confirmModal");
  confirmModal.addEventListener("show.bs.modal", function (event) {
    var button = event.relatedTarget;
    confirmcancelid = button.getAttribute("data-execution-id");
    console.log("confirmcancelid " + confirmcancelid);
  });
}

function addExecutionToTable(executionId,scheduled) {
  var $tbody = $(".executions-table tbody");
  $tbody.find('tr:has(td[colspan="4"])').remove();

  var now = new Date();
  var dateStr = now.toLocaleDateString("en-US", {
    month: "short",
    day: "2-digit",
    year: "numeric",
  });
  var timeStr = now.toLocaleTimeString("en-US", { hour12: false });
  var newRow;
if(scheduled){
  newRow = `
            <tr class="table-warning" data-execution-id="${executionId}">
                <td class="text-nowrap">
                    <div class="fw-semibold">${dateStr}</div>
                    <small class="text-muted">${timeStr}</small>
                </td>
                <td>
                    <span class="badge bg-warning text-dark">
                        <i class="fas fa-calendar-alt"></i>Scheduled
                    </span>
                </td>
                <td><span class="text-muted">—</span></td>
                <td class="text-end">
                    <div class="btn-group" role="group">
                     <button type="button" class="btn btn-danger btn-sm" data-bs-toggle="modal" data-bs-target="#confirmModal" data-execution-id="${executionId}">
                                          <i class="fas fa-stop me-1"></i>Cancel
                                         </button>

                    </div>
                </td>
            </tr>
        `;
}else{
   newRow = `
            <tr class="table-warning" data-execution-id="${executionId}">
                <td class="text-nowrap">
                    <div class="fw-semibold">${dateStr}</div>
                    <small class="text-muted">${timeStr}</small>
                </td>
                <td>
                    <span class="badge bg-warning text-dark">
                        <i class="fas fa-spinner fa-spin me-1"></i>Running
                    </span>
                </td>
                <td><span class="text-muted">—</span></td>
                <td class="text-end">
                    <div class="btn-group" role="group">
                     <button type="button" class="btn btn-danger btn-sm" data-bs-toggle="modal" data-bs-target="#confirmModal" data-execution-id="${executionId}"> 
                                          <i class="fas fa-stop me-1"></i>Cancel
                                         </button>
                        
                    </div>
                </td>
            </tr>
        `;
        }
//new row for scheduled
 
  $tbody.prepend(newRow);
}

function updateExecutionRow(executionId, status, duration, hasResult) {
  var $tbody = $(".executions-table tbody");
  var $row = $tbody.find(`tr[data-execution-id="${executionId}"]`);
  if ($row.length === 0) return;

  var statusBadge = "";
  var rowClass = "";
  if (status === "Succeeded") {
    statusBadge =
      '<span class="badge bg-success"><i class="fas fa-check me-1"></i>Succeeded</span>';
    rowClass = "table-success";
  } else if (status === "Failed") {
    statusBadge =
      '<span class="badge bg-danger"><i class="fas fa-times me-1"></i>Failed</span>';
    rowClass = "table-danger";
  } else if (status === "Cancelled") {
    statusBadge =
      '<span class="badge bg-secondary"><i class="fas fa-ban me-1"></i>Cancelled</span>';
    rowClass = "table-secondary";
  }else if (status === "Running") {
    statusBadge =
      '<span class="badge bg-warning text-dark"><i class="fas fa-spinner fa-spin me-1"></i>Running</span>';
    rowClass = "table-warning";
  }

  $row.removeClass("table-warning").addClass(rowClass);
  $row.find("td:eq(1)").html(statusBadge);

  if (duration > 0) {
    $row
      .find("td:eq(2)")
      .html(
        `<span class="badge bg-light text-dark"><i class="fas fa-clock me-1"></i>${duration}s</span>`,
      );
  }

  if (hasResult) {
    $row.find("td:eq(3)").html(`
                <div class="btn-group" role="group">
                    <a class="btn btn-sm btn-outline-primary" href="/Report/DownloadExecutionResult/${executionId}">
                        <i class="fas fa-download me-1"></i>Download
                    </a>
                    <button class="btn btn-sm btn-outline-danger delete-execution" data-execution-id=${executionId}>
                                            <i class="fas fa-trash me-1"></i>Delete
                                        </button>
                </div>
            `);
  } else {
    $row.find("td:eq(3)").html(`
                <div class="btn-group" role="group">
                   <button class="btn btn-sm btn-outline-danger delete-execution" data-execution-id=${executionId}>
                                            <i class="fas fa-trash me-1"></i>Delete
                                        </button>
                </div>
            `);
  }
}


function showNotification(status, executionId) {
  var message =
    status === "Succeeded"
      ? "Report execution completed successfully!"
      : status === "Cancelled"
        ? "Report execution was cancelled!"
        : "Report execution failed!";
  var alertClass =
    status === "Succeeded"
      ? "alert-success"
      : status === "Cancelled"
        ? "alert-warning"
        : "alert-danger";
  var icon =
    status === "Succeeded"
      ? "fa-check-circle"
      : status === "Cancelled"
        ? "fa-ban"
        : "fa-exclamation-circle";

  var notification = `
            <div class="alert ${alertClass} alert-dismissible fade show position-fixed" style="top: 20px; right: 20px; z-index: 9999; min-width: 300px;" role="alert">
                <i class="fas ${icon} me-2"></i>${message}
                <button type="button" class="btn-close" onclick="this.parentElement.remove()"></button>
            </div>
        `;

  $("body").append(notification);

  setTimeout(function () {
    $(".alert").fadeOut(function () {
      $(this).remove();
    });
  }, 5000);
}
