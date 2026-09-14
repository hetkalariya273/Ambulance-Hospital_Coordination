namespace AMS.Models
{
    public class Driver
    {
        public int DriverId { get; set; }

        public string FullName { get; set; } = string.Empty;

        public string PhoneNumber { get; set; } = string.Empty;

        public string LicenseNumber { get; set; } = string.Empty;

        public bool IsAvailable { get; set; }
    }
}