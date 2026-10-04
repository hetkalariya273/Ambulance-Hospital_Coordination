"use strict";

const connection = new signalR.HubConnectionBuilder()
    .withUrl("/ambulanceHub")
    .withAutomaticReconnect()
    .build();

connection.on(
    "NewAmbulanceRequest",
    function (requestId) {

        console.log(
            "New ambulance request received:",
            requestId
        );

        fetch("/Driver/NearbyRequests")
            .then(function (response) {
                if (!response.ok) {
                    throw new Error(
                        "Failed to load nearby requests."
                    );
                }

                return response.text();
            })
            .then(function (html) {

                const requestList =
                    document.getElementById("requestList");

                if (requestList) {
                    requestList.innerHTML = html;
                }

                const requestCount =
                    document.getElementById("requestCount");

                if (requestCount) {
                    requestCount.textContent =
                        requestList.querySelectorAll(".request-card").length;
                }
            })
    }
);

connection.start()
    .then(function () {

        console.log(
            "Driver SignalR connected successfully."
        );

        return connection.invoke(
            "JoinDriverGroup"
        );
    })
    .then(function () {

        console.log(
            "Driver joined SignalR group successfully."
        );
    })
    .catch(function (error) {

        console.error(
            "Driver SignalR error:",
            error
        );
    });