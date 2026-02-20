// dashboard.js - for Dashboard/Index.cshtml

if (typeof signalR === "undefined") {
  console.error("SignalR not loaded");
} else {
  var connection = new signalR.HubConnectionBuilder()
    .withUrl("/executionHub")
    .configureLogging(signalR.LogLevel.Information)
    .withAutomaticReconnect()
    .build();

  connection
    .start()
    .then(function () {
      console.log("SignalR connected (dashboard)");
      connection.invoke("JoinGroup", "Admins");
    })
    .catch(function (err) {
      console.error("SignalR connection failed: ", err);
    });

connection.on(
  "ExecutionCompleted",
  function (executionId, status, duration, hasResult) {
    //debugger;
    updateDashboardExecutionRow(executionId, status, duration, hasResult);
    //if status is not In Progress show notification
    // if(status !=="In Progress")
    //  showNotification(status, executionId);
  },
);
connection.on("ExecutionStarted",
  function (executionId,reportName) {
    console.log(`Execution started: ${executionId}`);
  //  updateDashboardExecutionRow(executionId, "Running", 0, false);
    AddNewExecutionRow(executionId, reportName, "Running", 0, false);
    //showNotification("Started", reportName);
    // You can add logic here to update the UI when an execution starts
  },
)
connection.on("ExecutionCancelled", function (executionId) {
    console.log(`Execution cancelled: ${executionId}`);
    updateDashboardExecutionRow(executionId, "Cancelled");
  },)
connection.on("ExecutionFailed", function (executionId, error) {
    console.log(`Execution failed: ${executionId}, Error: ${error}`);
    updateDashboardExecutionRow(executionId, "Failed");
  },)
  connection.on("ExecutionProgress",function (executionId, progress) {
  console.log(`Execution ${executionId} progress: ${progress}%`);
  // Update progress bar or any other UI element
  updateExecutionRow(executionId, "Progress", 0, false,progress);
 // showNotification("In Progress", executionId);
  })

// connection.on("ExecutionProgress", function (executionId, progress) {
//   console.log(`Execution ${executionId} progress: ${progress}%`);
//   // Update progress bar or any other UI element
//   updateDashboardExecutionRow(executionId, "Progress", 0, false,progress);
//  // showNotification("In Progress", executionId);
// });

}

function updateDashboardExecutionRow(executionId, status) {
  var $row = $(`#exec-${executionId}`);
  if ($row.length === 0) return;
  var $statusCell = $row.find(".status");

  switch (status) {
    case "Succeeded":
    case "Completed":
      $statusCell.html('<span class="badge bg-success"><i class="fas fa-check me-1"></i>Completed</span>');
      $row.addClass("table-success");
      setTimeout(() => $row.fadeOut(500, () => $row.remove()), 2000);
      break;
    case "Failed":
      $statusCell.html('<span class="badge bg-danger"><i class="fas fa-times me-1"></i>Failed</span>');
      $row.addClass("table-danger");
      setTimeout(() => $row.fadeOut(500, () => $row.remove()), 2000);
      break;
    case "Cancelled":
      $statusCell.html('<span class="badge bg-secondary"><i class="fas fa-ban me-1"></i>Cancelled</span>');
      $row.addClass("table-secondary");
      setTimeout(() => $row.fadeOut(500, () => $row.remove()), 2000);
      break;
    case "Running":
      $statusCell.html('<span class="badge bg-warning text-dark"><i class="fas fa-spinner fa-spin me-1"></i>Running</span>');
      $row.addClass("table-warning");
      break;
    default:
      $statusCell.html(`<span class="badge bg-secondary">${status}</span>`);
  }
}

$(document).on("click", ".btn-cancel", function () {
  var execId = $(this).data("id");
  if (confirm("Are you sure you want to cancel this execution?")) {
    $.ajax({
      url: "/Home/CancelExecution",
      method: "POST",
      contentType: "application/json",
      data: JSON.stringify({ executionId: execId }),
      success: function () {
        alert("Cancellation requested.");
      },
      error: function () {
        alert("Failed to cancel execution.");
      },
    });
  }
});
function AddNewExecutionRow(executionId, reportName, status, duration, hasResult)
{
    //status badge is running
    let statusBadge = `<span class="badge bg-warning text-dark"><i class="fas fa-spinner fa-spin me-1"></i>${status}</span>`;
    // table row shape Execution ID 	User 	Report/Job 	Status 	Started 	Duration 	Action
    var row = `<tr id="exec-${executionId}">
            <td>${executionId}</td>
            <td>Admin</td>
            <td>${reportName}</td>
            <td class="status">${statusBadge}</td>
            <td>Just now</td>
            <td>${duration}s</td>
            <td>
                <button class="btn btn-sm btn-danger btn-cancel" data-id="${executionId}">
                    <i class="fas fa-times"></i> Cancel
                </button>
            </td>
        </tr>`;
    $("#executions-table-body").prepend(row);


}