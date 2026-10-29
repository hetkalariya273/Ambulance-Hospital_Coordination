
"use strict";

const connection = new signalR.HubConnectionBuilder()
    .withUrl("/ambulanceHub")
    .withAutomaticReconnect()
    .build();

let isRefreshingRequests = false;
let refreshQueued = false;

async function refreshNearbyRequests() {
    if (isRefreshingRequests) {
        refreshQueued = true;
        return;
    }

    isRefreshingRequests = true;

    try {
        do {
            refreshQueued = false;

            const container = document.getElementById(
                "nearbyRequestsContainer"
            );

            if (!container) {
                console.error(
                    "nearbyRequestsContainer not found."
                );
                return;
            }

            const response = await fetch(
                "/Driver/NearbyRequests",
                {
                    method: "GET",
                    cache: "no-store",
                    headers: {
                        "X-Requested-With": "XMLHttpRequest"
                    }
                }
            );

            if (!response.ok) {
                throw new Error(
                    `Request refresh failed: ${response.status}`
                );
            }

            const html = await response.text();

            // Render the updated requests or empty-state message.
            container.innerHTML = html;

            // Read the exact count supplied by the server.
            const updatedContent = container.querySelector(
                "#nearbyRequestsContent"
            );

            const countElement = document.getElementById(
                "requestCount"
            );

            if (updatedContent && countElement) {
                countElement.textContent =
                    updatedContent.dataset.requestCount;
            } else {
                console.error(
                    "Request count element or updated content is missing."
                );
            }

            console.log(
                "Nearby requests updated. Count:",
                updatedContent?.dataset.requestCount
            );

        } while (refreshQueued);

    } catch (error) {
        console.error(
            "Error refreshing nearby requests:",
            error
        );
    } finally {
        isRefreshingRequests = false;
    }
}

// Receive new request notifications.
connection.on(
    "NewAmbulanceRequest",
    async function (requestId) {
        console.log(
            "New ambulance request received:",
            requestId
        );

        await refreshNearbyRequests();
    }
);

// Rejoin the driver's group after reconnection.
connection.onreconnected(async function () {
    try {
        await connection.invoke("JoinDriverGroup");
        await refreshNearbyRequests();
    } catch (error) {
        console.error("Reconnect error:", error);
    }
});

// Connect and load requests when the dashboard opens.
connection.start()
    .then(async function () {
        console.log("Driver SignalR connected.");

        await connection.invoke("JoinDriverGroup");

        console.log("Driver joined group.");

        await refreshNearbyRequests();
    })
    .catch(function (error) {
        console.error("Driver SignalR error:", error);
    });

// Fallback: refresh every 5 seconds if a notification is missed.
setInterval(function () {
    if (connection.state === signalR.HubConnectionState.Connected) {
        refreshNearbyRequests();
    }
}, 5000);
