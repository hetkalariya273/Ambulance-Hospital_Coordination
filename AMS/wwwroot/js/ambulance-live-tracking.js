
"use strict";

let ambulanceMap;
let ambulanceMarker;
let patientMarker;
let hospitalMarker;
let directionsService;
let directionsRenderer;
let gpsWatchId = null;

let lastRouteOrigin = null;
let lastRouteRequestTime = 0;
let routeRequestInProgress = false;
let pendingRouteOrigin = null;

const ROUTE_REFRESH_INTERVAL = 15000;
const ROUTE_REFRESH_DISTANCE = 50;

function readCoordinate(element, name) {
    const value = element.dataset[name];

    if (value === undefined || value.trim() === "") {
        return null;
    }

    const number = Number(value);
    return Number.isFinite(number) ? number : null;
}

function validPosition(lat, lng) {
    return lat !== null && lng !== null &&
        lat >= -90 && lat <= 90 &&
        lng >= -180 && lng <= 180;
}

function initAmbulanceLiveMap() {
    const trackingElement =
        document.getElementById("ambulanceTracking");

    const mapElement =
        document.getElementById("ambulanceLiveMap");

    const statusElement =
        document.getElementById("trackingStatus");

    if (!trackingElement || !mapElement) return;

    const ambulanceLat =
        readCoordinate(trackingElement, "latitude");

    const ambulanceLng =
        readCoordinate(trackingElement, "longitude");

    const patientLat =
        readCoordinate(trackingElement, "pickupLatitude");

    const patientLng =
        readCoordinate(trackingElement, "pickupLongitude");

    const hospitalLat =
        readCoordinate(trackingElement, "hospitalLatitude");

    const hospitalLng =
        readCoordinate(trackingElement, "hospitalLongitude");

    const hasAmbulance =
        validPosition(ambulanceLat, ambulanceLng);

    const hasPatient =
        validPosition(patientLat, patientLng);

    const hasHospital =
        validPosition(hospitalLat, hospitalLng);

    const ambulancePosition = hasAmbulance
        ? { lat: ambulanceLat, lng: ambulanceLng }
        : null;

    const patientPosition = hasPatient
        ? { lat: patientLat, lng: patientLng }
        : null;

    const hospitalPosition = hasHospital
        ? { lat: hospitalLat, lng: hospitalLng }
        : null;

    const initialPosition =
        ambulancePosition ||
        hospitalPosition ||
        patientPosition ||
        { lat: 20, lng: 0 };

    ambulanceMap = new google.maps.Map(mapElement, {
        center: initialPosition,
        zoom: hasAmbulance || hasPatient || hasHospital ? 15 : 2
    });

    directionsService = new google.maps.DirectionsService();

    directionsRenderer = new google.maps.DirectionsRenderer({
        map: ambulanceMap,
        suppressMarkers: true,
        preserveViewport: true,
        polylineOptions: {
            strokeColor: "#0057FF",
            strokeOpacity: 1,
            strokeWeight: 8,
            zIndex: 10
        }
    });

    // Ambulance marker
    if (ambulancePosition) {
        ambulanceMarker =
            createAmbulanceMarker(ambulancePosition);
    }

    // Patient pickup marker
    if (patientPosition && !hospitalPosition) {
        patientMarker = new google.maps.Marker({
            position: patientPosition,
            map: ambulanceMap,
            title: "Patient Pickup Location",
            zIndex: 20
        });
    }

    // Selected hospital marker
    if (hospitalPosition) {
        hospitalMarker = new google.maps.Marker({
            position: hospitalPosition,
            map: ambulanceMap,
            title: "Selected Hospital",
            label: "H",
            zIndex: 25
        });
    }

    fitMapToMarkers();

    if (statusElement) {
        statusElement.textContent = hasAmbulance
            ? "🚑 Ambulance | H: Hospital | Blue line: Driving route"
            : hospitalMarker
                ? "Hospital loaded. Waiting for ambulance GPS..."
                : hasPatient
                    ? "Patient location loaded. Waiting for ambulance GPS..."
                    : "Waiting for GPS coordinates...";
    }

    // Prefer hospital destination after hospital selection.
    const destinationMarker = hospitalMarker || patientMarker;

    if (ambulancePosition && destinationMarker) {
        requestDrivingRoute(ambulancePosition, true);
    }

    startAmbulanceGpsTracking();
}

function createAmbulanceMarker(position) {
    return new google.maps.Marker({
        position: position,
        map: ambulanceMap,
        title: "Current Ambulance Location",
        zIndex: 30,
        icon: {
            url: "https://maps.google.com/mapfiles/kml/shapes/ambulance.png",
            scaledSize: new google.maps.Size(45, 45),
            anchor: new google.maps.Point(22, 22)
        },
        label: {
            text: "🚑",
            fontSize: "25px"
        }
    });
}

function fitMapToMarkers() {
    if (!ambulanceMap) return;

    const bounds = new google.maps.LatLngBounds();
    let hasMarker = false;

    if (ambulanceMarker) {
        bounds.extend(ambulanceMarker.getPosition());
        hasMarker = true;
    }

    if (hospitalMarker) {
        bounds.extend(hospitalMarker.getPosition());
        hasMarker = true;
    }

    if (patientMarker && !hospitalMarker) {
        bounds.extend(patientMarker.getPosition());
        hasMarker = true;
    }

    

    if (hasMarker) {
        ambulanceMap.fitBounds(bounds);

        google.maps.event.addListenerOnce(
            ambulanceMap,
            "bounds_changed",
            function () {
                if (ambulanceMap.getZoom() > 17) {
                    ambulanceMap.setZoom(17);
                }
            }
        );
    }
}

function distanceInMeters(pointA, pointB) {
    const earthRadius = 6371000;
    const toRadians = value => value * Math.PI / 180;

    const dLat = toRadians(pointB.lat - pointA.lat);
    const dLng = toRadians(pointB.lng - pointA.lng);

    const lat1 = toRadians(pointA.lat);
    const lat2 = toRadians(pointB.lat);

    const a =
        Math.sin(dLat / 2) ** 2 +
        Math.cos(lat1) * Math.cos(lat2) *
        Math.sin(dLng / 2) ** 2;

    return earthRadius * 2 *
        Math.atan2(Math.sqrt(a), Math.sqrt(1 - a));
}

function requestDrivingRoute(origin, force = false) {
    const destinationMarker = hospitalMarker || patientMarker;

    if (!directionsService || !directionsRenderer ||
        !destinationMarker) {
        return;
    }

    const now = Date.now();

    if (routeRequestInProgress) {
        pendingRouteOrigin = origin;
        return;
    }

    const movedEnough =
        !lastRouteOrigin ||
        distanceInMeters(lastRouteOrigin, origin) >=
        ROUTE_REFRESH_DISTANCE;

    const timeElapsed =
        now - lastRouteRequestTime >= ROUTE_REFRESH_INTERVAL;

    if (!force && !(movedEnough && timeElapsed)) {
        return;
    }

    routeRequestInProgress = true;
    lastRouteRequestTime = now;
    lastRouteOrigin = origin;

    directionsService.route(
        {
            origin: origin,
            destination: destinationMarker.getPosition(),
            travelMode: google.maps.TravelMode.DRIVING,
            provideRouteAlternatives: false
        },
        function (result, status) {
            routeRequestInProgress = false;

            if (status === "OK") {
                directionsRenderer.setDirections(result);

                const route = result.routes[0];

                if (route && route.legs.length > 0) {
                    const leg = route.legs[0];
                    const statusElement =
                        document.getElementById("trackingStatus");

                    if (statusElement) {
                        const destinationName = hospitalMarker
                            ? "Selected Hospital"
                            : "Patient Pickup";

                        statusElement.textContent =
                            `🚑 Ambulance → ${destinationName} | ` +
                            `Route: ${leg.distance.text}, ` +
                            `ETA: ${leg.duration.text}`;
                    }
                }
            } else {
                console.error("Directions error:", status);

                const statusElement =
                    document.getElementById("trackingStatus");

                if (statusElement) {
                    statusElement.textContent =
                        "Unable to calculate driving route: " + status;
                }
            }

            if (pendingRouteOrigin) {
                const nextOrigin = pendingRouteOrigin;
                pendingRouteOrigin = null;

                requestDrivingRoute(nextOrigin);
            }
        }
    );
}

function startAmbulanceGpsTracking() {
    const trackingElement =
        document.getElementById("ambulanceTracking");

    const statusElement =
        document.getElementById("trackingStatus");

    if (!trackingElement || !navigator.geolocation) {
        if (statusElement) {
            statusElement.textContent =
                "GPS is not supported by this browser.";
        }
        return;
    }

    const requestId = trackingElement.dataset.requestId;
    const updateUrl = trackingElement.dataset.updateUrl;

    const token = document.querySelector(
        '#trackingTokenForm input[name="__RequestVerificationToken"]'
    )?.value;

    if (!requestId || !updateUrl || !token) {
        if (statusElement) {
            statusElement.textContent =
                "Tracking configuration is incomplete.";
        }
        return;
    }

    if (gpsWatchId !== null) {
        navigator.geolocation.clearWatch(gpsWatchId);
    }

    gpsWatchId = navigator.geolocation.watchPosition(
        async function (position) {
            const newPosition = {
                lat: position.coords.latitude,
                lng: position.coords.longitude
            };

            try {
                const response = await fetch(updateUrl, {
                    method: "POST",
                    headers: {
                        "Content-Type":
                            "application/x-www-form-urlencoded"
                    },
                    body: new URLSearchParams({
                        requestId: requestId,
                        latitude: newPosition.lat,
                        longitude: newPosition.lng,
                        __RequestVerificationToken: token
                    })
                });

                if (!response.ok) {
                    throw new Error(
                        `Server returned ${response.status}`
                    );
                }

                if (ambulanceMarker) {
                    ambulanceMarker.setPosition(newPosition);
                } else {
                    ambulanceMarker =
                        createAmbulanceMarker(newPosition);

                    fitMapToMarkers();
                }

                // Refresh route toward the selected destination.
                requestDrivingRoute(newPosition);

                if (!hospitalMarker && !patientMarker && statusElement) {
                    statusElement.textContent =
                        "Ambulance GPS received, but destination coordinates are missing.";
                }
            } catch (error) {
                console.error("GPS update failed:", error);

                if (statusElement) {
                    statusElement.textContent =
                        "Unable to send GPS location. Check the browser console.";
                }
            }
        },
        function (error) {
            console.error("GPS error:", error);

            if (statusElement) {
                statusElement.textContent =
                    "Please allow location access on the driver's device.";
            }
        },
        {
            enableHighAccuracy: true,
            maximumAge: 5000,
            timeout: 15000
        }
    );
}
