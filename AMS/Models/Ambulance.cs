namespace AMS.Models
{
    public class Ambulance
    {
        public int AmbulanceId { get; set; }

        public string VehicleNumber { get; set; } = string.Empty;

        public string Status { get; set; } = "Available";

        public double? Latitude { get; set; }

        public double? Longitude { get; set; }

        //foreign key to Driver
        public int ? DriverId {  get; set; }

        //Navigation property to Driver
        public Driver? Driver{ get; set; }
    }
}