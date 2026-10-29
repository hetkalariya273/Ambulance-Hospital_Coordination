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



let patientMap;
let patientAmbulanceMarker;
let patientDestinationMarker;
let patientDirectionsService;
let patientDirectionsRenderer;
let patientLastRouteTime = 0;
let patientLastRouteOrigin = null;
let patientRouteBusy = false;
let patientPendingOrigin = null;
let patientDestinationKey = null;

function initPatientTrackingMap() {
    const mapElement = document.getElementById("patientLiveMap");
    if (!mapElement) return;

    patientMap = new google.maps.Map(mapElement, {
        center: { lat: 20, lng: 0 },
        zoom: 2
    });

    patientDirectionsService = new google.maps.DirectionsService();

    patientDirectionsRenderer = new google.maps.DirectionsRenderer({
        map: patientMap,
        suppressMarkers: true,
        preserveViewport: true,
        polylineOptions: {
            strokeColor: "#0057FF",
            strokeOpacity: 1,
            strokeWeight: 8
        }
    });

    refreshPatientTracking();
    setInterval(refreshPatientTracking, 5000);
}

async function refreshPatientTracking() {
    const element = document.getElementById("patientTrackingData");
    if (!element) return;

    try {
        const response = await fetch(element.dataset.trackingUrl, {
            cache: "no-store"
        });

        if (!response.ok) throw new Error("Tracking request failed");

        const data = await response.json();

        const ambulance = {
            lat: Number(data.ambulanceLatitude),
            lng: Number(data.ambulanceLongitude)
        };

        const hasAmbulance =
            data.ambulanceLatitude != null &&
            data.ambulanceLongitude != null &&
            Number.isFinite(ambulance.lat) &&
            Number.isFinite(ambulance.lng);

        const hospital = {
            lat: Number(data.hospitalLatitude),
            lng: Number(data.hospitalLongitude)
        };

        const hasHospital =
            data.hasHospital &&
            data.hospitalLatitude != null &&
            data.hospitalLongitude != null &&
            Number.isFinite(hospital.lat) &&
            Number.isFinite(hospital.lng);

        const pickup = {
            lat: Number(data.pickupLatitude),
            lng: Number(data.pickupLongitude)
        };

        const destination = hasHospital ? hospital : pickup;
        const destinationName = hasHospital
            ? (data.hospitalName || "Selected Hospital")
            : "Patient Pickup Location";

        if (!patientDestinationMarker) {
            patientDestinationMarker = new google.maps.Marker({
                position: destination,
                map: patientMap,
                title: destinationName,
                label: hasHospital ? "H" : undefined
            });

            patientDestinationKey =
                `${destination.lat},${destination.lng}`;
        } else {
            const newKey = `${destination.lat},${destination.lng}`;

            if (newKey !== patientDestinationKey) {
                patientDestinationMarker.setPosition(destination);
                patientDestinationMarker.setTitle(destinationName);
                patientDestinationMarker.setLabel(hasHospital ? "H" : null);
                patientDestinationKey = newKey;

                patientDirectionsRenderer.set("directions", null);
                patientLastRouteOrigin = null;
                patientLastRouteTime = 0;
            }
        }

        if (!hasAmbulance) {
            document.getElementById("patientTrackingStatus").textContent =
                "Waiting for the driver's GPS location...";
            return;
        }

        if (!patientAmbulanceMarker) {
            patientAmbulanceMarker = new google.maps.Marker({
                position: ambulance,
                map: patientMap,
                title: "Live Ambulance",
                icon: {
                    url: "https://maps.google.com/mapfiles/kml/shapes/ambulance.png",
                    scaledSize: new google.maps.Size(45, 45)
                }
            });

            patientMap.setCenter(ambulance);
            patientMap.setZoom(14);
        } else {
            patientAmbulanceMarker.setPosition(ambulance);
        }

        document.getElementById("patientTrackingStatus").textContent =
            "Ambulance location updated • Status: " + data.status;

        drawPatientRoute(ambulance);
    } catch (error) {
        console.error("Patient tracking error:", error);
        document.getElementById("patientTrackingStatus").textContent =
            "Unable to refresh tracking. Retrying...";
    }
}

function patientDistanceMetres(a, b) {
    const rad = value => value * Math.PI / 180;
    const dLat = rad(b.lat - a.lat);
    const dLng = rad(b.lng - a.lng);

    const x =
        Math.sin(dLat / 2) ** 2 +
        Math.cos(rad(a.lat)) * Math.cos(rad(b.lat)) *
        Math.sin(dLng / 2) ** 2;

    return 6371000 * 2 * Math.atan2(Math.sqrt(x), Math.sqrt(1 - x));
}

function drawPatientRoute(origin) {
    if (!patientDestinationMarker || patientRouteBusy) {
        if (patientRouteBusy) patientPendingOrigin = origin;
        return;
    }

    const now = Date.now();
    const moved = !patientLastRouteOrigin ||
        patientDistanceMetres(patientLastRouteOrigin, origin) >= 50;
    const elapsed = now - patientLastRouteTime >= 15000;

    if (!moved || !elapsed) return;

    patientRouteBusy = true;
    patientLastRouteOrigin = origin;
    patientLastRouteTime = now;

    patientDirectionsService.route({
        origin: origin,
        destination: patientDestinationMarker.getPosition(),
        travelMode: google.maps.TravelMode.DRIVING
    }, (result, status) => {
        patientRouteBusy = false;

        if (status === "OK") {
            patientDirectionsRenderer.setDirections(result);

            const leg = result.routes[0]?.legs[0];

            if (leg) {
                document.getElementById("patientRouteDistance").textContent =
                    leg.distance.text;

                document.getElementById("patientRouteEta").textContent =
                    leg.duration.text;
            }
        } else {
            console.error("Patient route error:", status);
            document.getElementById("patientTrackingStatus").textContent =
                "Unable to calculate route: " + status;
        }

        if (patientPendingOrigin) {
            const next = patientPendingOrigin;
            patientPendingOrigin = null;
            drawPatientRoute(next);
        }
    });
}

