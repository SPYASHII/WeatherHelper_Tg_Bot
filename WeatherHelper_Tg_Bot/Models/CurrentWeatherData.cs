namespace WeatherHelperTGBOT.Models
{
    public class CurrentWeatherData
    {
        public Location location { get; set; }
        public Current current { get; set; }
    }
    public class Location
    {
        public string name { get; set; }
        public string region { get; set; }
        public string country { get; set; }

        //public float lat { get; set; }
        //public float lon { get; set; }
        //public string tz_id { get; set; }
        //public long localtime_epoch { get; set; }
        public string localtime { get; set; }
    }

    public class Condition
    {
        public string text { get; set; }
    }

    public class Current
    {
        public float temp_c { get; set; }
        public float feelslike_c { get; set; }

        public Condition condition { get; set; }
        public float wind_kph { get; set; }
        public float precip_mm { get; set; }
        public int humidity { get; set; }

        //public float vis_km { get; set; }
    }
}
