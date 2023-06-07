namespace WeatherAPI.Models
{
    public class UserData
    {
        public Notification notification { get; set; }
        public long chatId { get; set; }
        public string location { get; set; }
        public bool waitingForTime { get; set; }
        public bool waitingForDays { get; set; }
        public bool waitingForWeather { get; set; }
        public bool waitingForLocation { get; set; }
        public bool foreCast { get; set; }
        public bool weatherByDate { get; set; }
        public bool dayByWeather { get; set; }
        public bool gptCurrent { get; set; }
        public bool gptByDate { get; set; }
    }
    public class Notification
    {
        public DateTime notificationTime { get; set; }
        public string weather { get; set; }
        public int days { get; set; }
        public bool notify { get; set; }
    }
}
