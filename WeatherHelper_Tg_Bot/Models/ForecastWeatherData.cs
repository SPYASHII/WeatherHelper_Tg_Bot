namespace WeatherHelperTGBOT.Models
{
    public class ForecastWeatherData
    {
        public Location location { get; set; }
        public Current current { get; set; }
        public Forecast forecast { get; set; }
    }
    public class Day
    {
        public double maxtemp_c { get; set; }
        public double mintemp_c { get; set; }
        public double avgtemp_c { get; set; }
        public double maxwind_kph { get; set; }
        public double totalprecip_mm { get; set; }
        public double totalsnow_cm { get; set; }
        public double avgvis_km { get; set; }
        public double avghumidity { get; set; }
        public int daily_will_it_rain { get; set; }
        public int daily_chance_of_rain { get; set; }
        public int daily_will_it_snow { get; set; }
        public int daily_chance_of_snow { get; set; }
        public Condition condition { get; set; }
    }
    public class Forecast
    {
        public List<Forecastday> forecastday { get; set; }
    }
    public class Forecastday
    {
        public string date { get; set; }
        public int date_epoch { get; set; }
        public Day day { get; set; }
        public List<Hour> hour { get; set; }
    }
    public class Hour
    {
        public int time_epoch { get; set; }
        public string time { get; set; }
        public double temp_c { get; set; }
        public Condition condition { get; set; }
        public double wind_kph { get; set; }
        public int wind_degree { get; set; }
        public string wind_dir { get; set; }
        public double pressure_mb { get; set; }
        public double precip_mm { get; set; }
        public int humidity { get; set; }
        public int cloud { get; set; }
        public double feelslike_c { get; set; }
        public double windchill_c { get; set; }
        public double heatindex_c { get; set; }
        public double dewpoint_c { get; set; }
        public int will_it_rain { get; set; }
        public int chance_of_rain { get; set; }
        public int will_it_snow { get; set; }
        public int chance_of_snow { get; set; }
        public double vis_km { get; set; }
        public double gust_kph { get; set; }
    }
}
