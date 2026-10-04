"use strict";

const requestId = document.getElementById("requestId")?.value;

if (requestId) {

    const connection = new signalR.HubConnectionBuilder()
        .withUrl("/ambulanceHub")
        .withAutomaticReconnect()
        .build();

    connection.on(
        "RequestStatusUpdated",
        function (updatedRequestId, newStatus) {

            console.log(
                "SignalR event received:",
                updatedRequestId,
                newStatus
            );

            if (updatedRequestId.toString() !== requestId) {
                return;
            }

            const statusElement =
                document.getElementById("requestStatus");

            if (statusElement) {
                statusElement.textContent = newStatus;
            }
        }
    );

    connection.start()
        .then(function () {
            console.log("SignalR connected successfully.");
        })
        .catch(function (error) {
            console.error(
                "SignalR connection error:",
                error
            );
        });
}
