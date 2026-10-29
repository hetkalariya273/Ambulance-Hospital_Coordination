let pickupMap;
let pickupMarker;

function initPickupMap() {
    const latitudeInput = document.getElementById("pickupLatitude");
    const longitudeInput = document.getElementById("pickupLongitude");
    const locationStatus = document.getElementById("locationStatus");
    const submitBtn = document.getElementById("submitBtn");

    const defaultLocation = { lat: 22.3072, lng: 73.1812 };

    pickupMap = new google.maps.Map(document.getElementById("pickupMap"), {
        center: defaultLocation,
        zoom: 12
    });

    pickupMarker = new google.maps.Marker({
        position: defaultLocation,
        map: pickupMap,
        draggable: true
    });

    function setPickupLocation(position) {
        const lat = position.lat();
        const lng = position.lng();

        latitudeInput.value = lat;
        longitudeInput.value = lng;

        pickupMarker.setPosition({ lat, lng });
        pickupMap.setCenter({ lat, lng });

        locationStatus.textContent = "Pickup location selected.";
        submitBtn.disabled = false;
    }

    pickupMap.addListener("click", function (event) {
        setPickupLocation(event.latLng);
    });

    pickupMarker.addListener("dragend", function (event) {
        setPickupLocation(event.latLng);
    });

    document.getElementById("getLocationBtn").addEventListener("click", function () {
        if (!navigator.geolocation) {
            locationStatus.textContent = "Geolocation is not supported.";
            return;
        }

        locationStatus.textContent = "Getting your location...";

        navigator.geolocation.getCurrentPosition(
            function (position) {
                const location = new google.maps.LatLng(
                    position.coords.latitude,
                    position.coords.longitude
                );

                setPickupLocation(location);
                pickupMap.setZoom(16);
            },
            function () {
                locationStatus.textContent =
                    "Unable to get location. Allow browser location access.";
            }
        );
    });

    submitBtn.disabled = true;
}