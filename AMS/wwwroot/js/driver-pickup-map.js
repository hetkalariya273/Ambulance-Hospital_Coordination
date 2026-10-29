function initDriverPickupMap() {
    const mapElement = document.getElementById("patientPickupMap");

    if (!mapElement) return;

    const location = {
        lat: Number(mapElement.dataset.lat),
        lng: Number(mapElement.dataset.lng)
    };

    const map = new google.maps.Map(mapElement, {
        center: location,
        zoom: 16
    });

    new google.maps.Marker({
        position: location,
        map: map,
        title: "Patient Pickup Location"
    });
}