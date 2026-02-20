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

connection.on(
  "ExecutionCompleted",
  function (executionId, status, duration, hasResult) {
    updateExecutionRow(executionId, status, duration, hasResult);
    showNotification(status, executionId);
  },
);
connection.on("ExecutionStarted",
  function (executionId,reportName) {
    console.log(`Execution started: ${executionId}`);
    updateExecutionRow(executionId, "Running", 0, false);
    showNotification("Started", reportName);
    // You can add logic here to update the UI when an execution starts
  },
)
connection.on("ExecutionProgress", function (executionId, progress) {
  console.log(`Execution ${executionId} progress: ${progress}%`);
  // Update progress bar or any other UI element
  updateExecutionRow(executionId, "Progress", 0, false,progress);
  showNotification("In Progress", executionId);
});




}

function IntializePartial(reportId) {
  
var executionId= $("#executionId").val();

  $(document).on("click", "#btnConfirmCancel", function () {
    //var confirmcancelid = $(this).data('execution-id');
    console.log(confirmcancelid);
    $.ajax({
      url: "/Home/CancelExecution",
      method: "POST",
      data: { executionId: confirmcancelid },
      success: function () {
        showNotification("Cancelled", confirmcancelid);
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

function addExecutionToTable(executionId) {
  var $tbody = $(".executions-table tbody");
  $tbody.find('tr:has(td[colspan="4"])').remove();

  var now = new Date();
  var dateStr = now.toLocaleDateString("en-US", {
    month: "short",
    day: "2-digit",
    year: "numeric",
  });
  var timeStr = now.toLocaleTimeString("en-US", { hour12: false });

  var newRow = `
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

  $tbody.prepend(newRow);
}

function updateExecutionRow(executionId, status, duration, hasResult,ProgressNum) {
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
  }else if (status === "Progress") {
    statusBadge =
    //add progress number
    `<span class="badge bg-info text-dark"><i class="fas fa-spinner fa-spin me-1"></i>Progress ${ProgressNum}%</span>`;
 
    rowClass = "table-info";
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
/*

//this is code of razor view we will split in its own js file

if (typeof signalR === "undefined") {
  console.error("SignalR not loaded");

}else{


var connection = new signalR.HubConnectionBuilder()
  .withUrl("/executionHub")
  .configureLogging(signalR.LogLevel.Information)
  //configur with automatic reconnect
  .withAutomaticReconnect()
  .build();

connection
  .start()
  .then(function () {
    console.log("SignalR connected");
  })
  .catch(function (err) {
    console.error("SignalR connection failed: ", err);
  });



connection.on(
  "ExecutionCompleted",
  function (executionId, status, duration, hasResult) {
    //debugger;
    updateExecutionRow(executionId, status, duration, hasResult);
    //if status is not In Progress show notification
    if(status !=="In Progress")
     showNotification(status, executionId);
  },
);
connection.on("ExecutionStarted",
  function (executionId,reportName) {
    console.log(`Execution started: ${executionId}`);
    updateExecutionRow(executionId, "Running", 0, false);
    //showNotification("Started", reportName);
    // You can add logic here to update the UI when an execution starts
  },
)
connection.on("ExecutionProgress", function (executionId, progress) {
  console.log(`Execution ${executionId} progress: ${progress}%`);
  // Update progress bar or any other UI element
  updateExecutionRow(executionId, "Progress", 0, false,progress);
 // showNotification("In Progress", executionId);
});
}
function updateExecutionRow(executionId, status, duration, hasResult, progress) {
  var $row = $(`.executions-table tbody tr[data-execution-id="${executionId}"]`);

  if ($row.length === 0) {
    // If row doesn't exist, add it
    addExecutionToTable(executionId);
    $row = $(`.executions-table tbody tr[data-execution-id="${executionId}"]`);
  }

  var $statusCell = $row.find("td:eq(4)");
  var $durationCell = $row.find("td:eq(2)");
  var $actionsCell = $row.find("td:eq(3)");

  // Update status
  switch (status) {
    case "Success":
      $statusCell.html(
        '<span class="badge bg-success"><i class="fas fa-check-circle me-1"></i>Success</span>',
      );
      break;
    case "Failed":
      $statusCell.html(
        '<span class="badge bg-danger"><i class="fas fa-times-circle me-1"></i>Failed</span>',
      );
      break;
    case "Running":
      $statusCell.html(
        '<span class="badge bg-warning text-dark"><i class="fas fa-spinner fa-spin me-1"></i>Running</span>',
      );
      break;
    case "Progress":
      $statusCell.html(
        '<span class="badge bg-info text-dark"><i class="fas fa-tachometer-alt me-1"></i>In Progress</span>',
      );
      break;
    case "Scheduled":
      $statusCell.html(
        '<span class="badge bg-warning text-dark"><i class="fas fa-calendar-alt me-1"></i>Scheduled</span>',
      );
      break;
    default:
      $statusCell.html(`<span class="badge bg-secondary">${status}</span>`);
  }

  // Update duration
  if (duration > 0) {
    var minutes = Math.floor(duration / 60);
    var seconds = Math.floor(duration % 60);
    var durationStr = `${minutes}m ${seconds}s`;
    $durationCell.text(durationStr);
  }

  // Update actions
  if (status === "Running" || status === "Scheduled") {
    $actionsCell.html(`
            <div class="btn-group" role="group">
                <button type="button" class="btn btn-danger btn-sm" data-bs-toggle = "modal" data-bs-target="#cancelExecutionModal" data-execution-id="${executionId}">
                    <i class="fas fa-stop me-1"></i>Cancel
                </button>
            </div>
        `);
  } else {
    $actionsCell.html(`
            <div class="btn-group" role="group">
                <a class="btn btn-outline-primary btn-sm" href="/Report/DownloadExecutionResult/${executionId}">
                    <i class="fas fa-download me-1"></i>Download
                </a>
                <button class="btn btn-outline-danger btn-sm delete-execution" data-execution-id="${executionId}">
                    <i class="fas fa-trash me-1"></i>Delete
                </button>
            </div>
        `);
  }
}

function addExecutionToTable(executionId) {
  var newRow = `
        <tr data-execution-id="${executionId}">
            <td class="text-center"><i class="fas fa-file-alt text-muted"></i></td>
            <td><span class="badge bg-secondary">Pending</span></td>
            <td>-</td>
            <td>
                <div class="btn-group" role="group">
                    <button type="button" class="btn btn-danger btn-sm" data-bs-toggle="modal" data-bs-target="#cancelExecutionModal" data-execution-id="${executionId}">
                        <i class="fas fa-stop me-1"></i>Cancel
                    </button>
                </div>
            </td>
        </tr>
    `;
  $(".executions-table tbody").prepend(newRow);
}

*/ 